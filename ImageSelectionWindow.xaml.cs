﻿using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Окно выводит список доступных для модуля обновлений в контроллере
    /// </summary>
    public partial class ImageSelectionWindow : Window
    {
        public CModule Module = null;
        private string ModuleType = "";
        public ObservableCollection<string> Images { get; set; }
        private Dictionary<string, CImage[]> CompatibleImages = new Dictionary<string, CImage[]>();
        private static Dictionary<string, Delegate> CompatiblePlcImages = new Dictionary<string, Delegate>();
        public CImage PLC2Image = null;
        public CImage ModuleComImage = null;
        public CImage ModuleChannelImage = null;
        public bool IsImages = false;
        public bool IsClosed { get; private set; }
        public bool Error = false;
        public ImageSelectionWindow(CModule module)
        {
            Module = module;
            ModuleType = module.Module_Type;
            InitializeComponent();
        }

        public ImageSelectionWindow(CModule module, string module_plc_version)
        {
            CompatiblePlcImages["K3"] = new Func<Dictionary<string, CImage[]>>(CompatiblePlc3Images);
            CompatiblePlcImages["K2"] = new Func<Dictionary<string, CImage[]>>(CompatiblePlc2Images);
            Module = module;
            ModuleType = module.Module_Type;
            try
            {
                IsImages = Compatible(module_plc_version);
            }
            catch (Exception ex)
            {
                string exMessage = ex.InnerException.Message == "" ? ex.Message : ex.InnerException.Message;
                MessageBox.Show($"Не удалось определить совместимость версий для модуля {module.Module_Name} (номер: {module.NodeID})" +
                    $"\nПричина: {exMessage}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                Error = true;
                return;
            }
            InitializeComponent();
            this.ModuleParams.Content = $"Тип модуля: <{Module.Module_Name}> " + "\n" + $"Версия железа: <{Module.HardVersion}>";
        }

        private Dictionary<string, CImage[]> CompatiblePlc2Images()
        {
            PLC2ImagesList pLC2ImagesList = new PLC2ImagesList();
            pLC2ImagesList.Start(Module, ModuleType);
            return pLC2ImagesList.ImagesList;
        }

        private Dictionary<string, CImage[]> CompatiblePlc3Images()
        {
            ModuleCommList CommList = new ModuleCommList();
            CommList.Start(Module, ModuleType);

            ModuleChanList ChannelList = new ModuleChanList(CommList.COMMList);
            if (!ChannelList.Start(Module, ModuleType))
                return null;

            return ChannelList.CompatibleImages;
        }

        public bool Compatible(string module_plc_version)
        {
            var result = CompatiblePlcImages[module_plc_version].DynamicInvoke();
            CompatibleImages = (Dictionary<string, CImage[]>)result;
            Images = new ObservableCollection<string>(CompatibleImages.Keys);

            return Images.Count() != 0 && CompatibleImages.Count() != 0;
        }

        public void TheNewestPlc2()
        {
            TheNewsetPLC2Image PLC2image = new TheNewsetPLC2Image(ModuleType);

            if (PLC2image.Start(Module, ModuleType))
                PLC2Image = PLC2image.GetImage;
        }

        public void TheNewestPlc3()
        {
            string version = Module.SoftVersion;
            //Ищется прошивка COM-платы
            CommImage FindComImage = new CommImage();
            if (FindComImage.Start(Module, ModuleType))
            {
                ModuleComImage = FindComImage.GetImage;
                version = ModuleComImage.Version;
            }
            //Ищется прошивка канальной платы
            ChannelImage FindChannelImage = new ChannelImage(version);
            if (FindChannelImage.Start(Module, ModuleType))
                ModuleChannelImage = FindChannelImage.GetImage;
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CImage[] image = CompatibleImages[imagesComboBox.Text];
                ModuleComImage = image[0];

                if (image[1] != null)
                    ModuleChannelImage = image[1];

                this.DialogResult = true;
            }
            catch
            {
                MessageBox.Show("Выберите образ!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

    public class ImageCheck : INotifyPropertyChanged
    {
        public CImage GetImage => Image;
        //Тип модуля (0 - ком-плата)
        protected string module_type = "";
        //Для удаления знаков (нужно для метода Remove)
        protected int for_remove = 0;
        //Тут будет хранится версия прошивки, с которой надо будет работать
        protected CImage Image = null;
        //Тут будет хранится информация о модуле, с которой надо будет работать
        protected CModule Module = null;
        //Количество прошивок (нужно для контейнеров)
        private int amount = 0;
        //Тип платы для функции StopThisUpdate()
        protected string board_part = "";
        //Сортируем список прошивок   
        private SortedList<string, CImage> SortedList = new SortedList<string, CImage>();
        //Список прошивок 
        protected List<CImage> ImageList => SortedList.Values.ToList();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// Функция возвращает список прошивок, не поддерживающих артикул
        /// </summary>
        /// <returns></returns>
        private List<CImage> GetImagesWithoutArticules()
        {
            //К3 сняли с производства и там не будет новых версий железа
            if (Module.ModuleVesrsion == ProductVersion.K3)
            {
                return CGlobal.Config.CImagesList.GetCompatibleProductByAdc(Module.ADC_Code);
            }
            return CGlobal.Config.CImagesList.GetImagesByAdcAndHard(Module.ADC_Code, Module.HardVersion, Module.ModuleVesrsion);
        }
        /// <summary>
        /// Функция возвращает список прошивок, поддерживающих артикул
        /// </summary>
        /// <returns></returns>
        private List<CImage> GetImagesWithArticules()
        {
            //Если мы под root, то можно выдать все прошивки для модуля. Ноль  - Значит COMM-плата.
            if (CGlobal.Handler.Auth.Account != "root" || module_type == "0")
            {
                return CGlobal.Config.CImagesList.GetImagesByCode(Module.ProductCode);
            }
            else
            {
                return CGlobal.Config.CImagesList.GetImagesByAdcAndHard(Module.ADC_Code, Module.HardVersion, Module.ModuleVesrsion);
            }
        }
        private List<CImage> GetImages()
        {
            if (!Module.IsModuleFlashed())
            {
                if (CGlobal.Handler.Auth.Account != "root")
                {
                    throw new Exception("В модуле отстуствует прошивка!");
                }
            }

            if (module_type == "0")
            {
                return CGlobal.Config.CImagesList.GetImagesByCode(module_type);
            }

            if (Module.IsSupportArticule())
            {
                return GetImagesWithArticules();
            }
            else
            {
                return GetImagesWithoutArticules();
            }
        }

        //Функция для выгрузки прошивок из базы
        protected void InitModuleImagesList(CModule module)
        {
            Module = module;
            SortedList.Clear();
            ImageList.Clear();
            amount = -1;

            List<CImage> images = GetImages();
            images = images?.Where(i => (i.Product ?? "").ToLower().Trim().Equals(Convert.ToString(module.ModuleVesrsion).ToLower())).ToList();
            if (images == null || images.Count == 0)
            {
                //Пусть пишет штатную ошибку
                throw new Exception("В коллекции отсутствуют образы");
            }

            foreach (CImage image in images)
            {
                if (image.OrderCode != "")
                {
                    this.SortedList.Add(image.OrderCode, image);
                }
                else
                {
                    this.SortedList.Add(image.Version, image);
                }
                amount += 1;
            }
        }

        protected virtual void SQlInsert(CModule module, string module_type)
        {
            try
            {
                string sql_insert = "insert into update_info(id, state, type, old_softver, com_part, channel_part) ";
                string sql_values = "values({0}, {1}, '{2}', '{3}', '{4}', '{5}')";
                string sql = String.Format(sql_insert + sql_values, module.NodeID, 0, module.Module_Name, module.SoftVersion, "", "");
                NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_insert.ExecuteNonQuery();
            }
            catch
            {
                string sql_insert = "update update_info set state = 0, type = '{1}', old_softver = '{2}', com_part = '{3}', channel_part = '{4}' where id = '{0}'";
                string sql = String.Format(sql_insert, module.NodeID, module.Module_Name, module.SoftVersion, "", "");
                NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_insert.ExecuteNonQuery();
            }
        }


        public virtual bool Start(CModule module, string module_type = "")
        {
            //Из базы нужно выгрузить версии прошивок для определенного типа модуля 
            InitModuleImagesList(module);
            //Если ничего не нашли, то таблица не заполняется 
            if (!IsTheLatest())
                return false;
            //Ну мало ли
            if (ImageList[amount] == null)
                return false;

            //FillTheQueue(ImageList[amount].ID);

            return true;
        }

        //Ищется последняя и совместимая сборка
        protected virtual bool IsTheLatest()
        {
            if (amount == -1)
            {
                StopThisUpdate(2);
                return false;
            }

            for (int i = amount; amount >= 0; i--)
            {
                if (!FindVers())
                    continue;

                if (IsVersCompatible())
                    return true;
                amount -= 1;
            }

            return false;
        }

        //Проверка совместимости
        protected virtual bool IsVersCompatible()
        {
            if (amount == -1)
                StopThisUpdate(1);

            return false;
        }
        protected virtual void StopThisUpdate(int status)
        {
            Dictionary<int, string> sql_update = new Dictionary<int, string>();
            sql_update.Add(0, "update update_info set state = 3, {0} = 'Обновление не требуется' where id ='{1}'");
            sql_update.Add(1, "update update_info set state = 0, {0} = 'Несовместимые версии железа и ПО' where id ='{1}'");
            sql_update.Add(2, "update update_info set state = 0, {0} = 'В контроллере отсутствует сборка для модуля' where id ='{1}'");
            string sql = String.Format(sql_update[status], board_part, Module.NodeID);
            NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd_insert.ExecuteNonQuery();
        }

        //Функция сравнивает версию в базе и версию модуля
        private bool FindVers()
        {
            try
            {
                //Версия в базе
                string image_com = ImageList[amount].Version;
                string new_com = image_com.Remove(for_remove, 6);
                double d_icom = Convert.ToDouble(CAuxil.AdaptFloat(image_com.Remove(for_remove, 6)));
                //Версия модуля
                double d_mcom = Convert.ToDouble(CAuxil.AdaptFloat(Module.SoftVersion.Remove(for_remove, 6)));

                //Если в базе есть версяя новее, то будем проверять совместимость
                if (d_icom > d_mcom)
                {
                    Image = ImageList[amount];
                    return true;
                }
            }
            catch
            {
                StopThisUpdate(1);
                amount = -1;
                return false;
            }

            if (amount == 0)
                StopThisUpdate(0);

            amount -= 1;
            return false;
        }
    }

    //Сборка для COM-платы
    public class CommImage : ImageCheck
    {
        //Список версий плат, совместимых с ПО ком-платы в базе 
        public List<double> GethardList = new List<double>();
        public CommImage()
        {
            this.module_type = "0";
            for_remove = 5;
        }

        public override bool Start(CModule module, string module_type)
        {
            SQlInsert(module, module_type);
            bool state = base.Start(module);
            return state;
        }

        //Если ничего не подходит
        protected override void StopThisUpdate(int status)
        {
            board_part = "com_part";
            base.StopThisUpdate(status);
        }

        //Проверка совместимости
        protected override bool IsVersCompatible()
        {
            uint sum = Convert.ToUInt32(Module.SubType >= 10) + Convert.ToUInt32(Image.SubType >= 10);
            if (sum == 1)
                return false;
            GetComHardware(Image.Version);
            string[] str = Module.HardVersion.Split('.');
            if (GethardList.Contains(Convert.ToDouble(CAuxil.AdaptFloat(str[0]))))
                return true;

            return base.IsVersCompatible();
        }

        protected virtual void GetComHardware(string version)
        {
            GethardList.Clear();
            String sql = $"select hard_adc from module_update_table where version = '{version}' " +
                $"and type = '{module_type}'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            string[] hard_array = reader.GetString(0).Split(',');
            foreach (string vers in hard_array)
            {
                //Только у K3.1 может быть так
                if (version.Split('.').Length == 4)
                {
                    string[] str = vers.Split('.');
                    GethardList.Add(Convert.ToDouble(CAuxil.AdaptFloat(str[0])));
                    continue;
                }

                GethardList.Add(Convert.ToDouble(CAuxil.AdaptFloat(vers)));
            }
            reader.Close();
        }
    }

    //Сборка для канальной платы
    public class ChannelImage : ImageCheck
    {
        protected List<double> ChanHardList = new List<double>();
        public string FutureSoftVersion = "";
        public ChannelImage(string softversion)
        {
            FutureSoftVersion = softversion;
        }
        public override bool Start(CModule module, string module_type)
        {
            for_remove = 0;
            base.module_type = module_type;
            return base.Start(module);
        }
        protected void GetChanHardList(string version)
        {
            ChanHardList.Clear();
            String sql = $"select hardware from module_update_table where version = '{version}' " +
                $"and hard_adc = '{module_type}'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            string[] hard_array = reader.GetString(0).Split(',');
            foreach (string vers in hard_array)
            {
                string[] numbs = vers.Split('.');
                ChanHardList.Add(Convert.ToDouble(CAuxil.AdaptFloat(numbs[0])));
            }
            reader.Close();
        }
        //Проверяем совместимость прошивки канальной платы с версией ПО COM-платы модуля
        protected override bool IsVersCompatible()
        {
            uint sum = Convert.ToUInt32(Module.SubType >= 10) + Convert.ToUInt32(Image.SubType >= 10);
            if (sum == 1)
                return false;

            string image_com = Image.Version;
            double d_icom = Convert.ToDouble(CAuxil.AdaptFloat(image_com.Remove(5, 6)));
            //Версия модуля
            double d_mcom = Convert.ToDouble(CAuxil.AdaptFloat(FutureSoftVersion.Remove(5, 6)));
            if (d_icom <= d_mcom)
                return true;

            return base.IsVersCompatible();
        }

        protected override void StopThisUpdate(int status)
        {
            board_part = "channel_part";
            base.StopThisUpdate(status);
        }
    }

    sealed class ModuleCommList : CommImage
    {
        public List<CImage> COMMList = new List<CImage>();
        //Список версий плат, совместимых с ПО ком-платы в базе 

        public override bool Start(CModule module, string module_type = "")
        {
            for_remove = 5;
            this.module_type = "0";
            Module = module;
            base.InitModuleImagesList(module);
            return IsVersCompatible();
        }

        protected override bool IsVersCompatible()
        {
            double hardVer;
            //Считываем версии железа для всех прошивок, чтобы "вычеркнуть" не совместимые
            foreach (var image in ImageList)
            {
                //0 - 9 старый формат K3
                //И модуль и прошивка ком-платы должны быть одного типа
                uint sum = Convert.ToUInt32(Module.SubType >= 10) + Convert.ToUInt32(image.SubType >= 10);
                if (sum == 1)
                    continue;

                base.GetComHardware(image.Version);
                string[] str = Module.HardVersion.Split('.');
                hardVer = Convert.ToDouble(CAuxil.AdaptFloat(str[0]));
                if (GethardList.Contains(hardVer))
                    COMMList.Add(image);
            }

            ImageList.Clear();

            return COMMList.Count != 0;
        }
    }

    public sealed class ModuleChanList : ChannelImage
    {
        private List<CImage> COMMlist = null;

        public Dictionary<string, CImage[]> CompatibleImages = new Dictionary<string, CImage[]>();
        public ModuleChanList(List<CImage> COMMlist, string softversion = null) : base(softversion)
        {
            this.COMMlist = COMMlist;
        }

        public override bool Start(CModule module, string module_type)
        {
            for_remove = 0;
            this.module_type = module_type;
            base.InitModuleImagesList(module);
            SQlInsert(module, module_type);
            return IsVersCompatible();
        }
        protected override bool IsVersCompatible()
        {
            foreach (CImage chanimages in ImageList)
            {
                string[] str = Module.HardVersion.Split('.');
                //4 цифры только у модуля K3.1
                if (str.Count() == 4)
                {
                    uint sum = Convert.ToUInt32(Module.SubType >= 10) + Convert.ToUInt32(chanimages.SubType >= 10);
                    if (sum == 1)
                        continue;

                    GetChanHardList(chanimages.Version);
                    //Совместимость с канальной платой опеределяется по 3 цифре
                    if (!ChanHardList.Contains(Convert.ToDouble(CAuxil.AdaptFloat(str[2]))))
                        continue;
                }
                foreach (CImage comimages in COMMlist)
                {
                    //для проверки железа
                    string str_hard = chanimages.Version.Remove(5, 6);
                    double d_hard = Convert.ToDouble(CAuxil.AdaptFloat(str_hard));
                    //Версия ком-платы
                    string str_com = comimages.Version.Remove(5, 6);
                    double d_com = Convert.ToDouble(CAuxil.AdaptFloat(str_com));

                    if (d_hard > d_com)
                        continue;

                    string str_channel = chanimages.Version.Remove(0, 6);
                    //Массив с прошивками для обеих плат
                    CImage[] ar_image = { comimages, chanimages };

                    //Имя прошивки
                    string final_image = "";
                    if (ar_image[1].OrderCode != "")
                    {
                        final_image = $"[{ar_image[1].OrderCode}] ";
                    }
                    final_image += str_com + "." + str_channel;
                    CompatibleImages.Add(final_image, ar_image);
                }
            }
            return CompatibleImages.Count != 0;
        }

        protected override void SQlInsert(CModule module, string module_type)
        {
            try
            {
                base.SQlInsert(module, module_type);

            }
            catch
            {
                string sql_insert = "update update_info set state = {1}, type = '{2}', old_softver = '{3}', com_part = '{4}', channel_part = '{5}' where id = {0} ";
                string sql = String.Format(sql_insert, Module.NodeID, 0, module_type, Module.SoftVersion, "", "");
                NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_insert.ExecuteNonQuery();
            }
        }

        public void GetImages(out CImage channel, out CImage comm, string version = null)
        {
            if (version == null)
            {
                channel = null;
                comm = null;
            }
            else
            {
                CImage[] image = CompatibleImages[version];
                comm = image[0];
                channel = image[1];
            }
        }

    }

    public class PLC2ImagesList : CommImage
    {
        public Dictionary<string, CImage[]> ImagesList = new Dictionary<string, CImage[]>();
        //public Dictionary<string, CImage[]> CompatibleImages = new Dictionary<string, CImage[]>();
        //Список версий плат, совместимых с ПО ком-платы в базе 
        public override bool Start(CModule module, string module_type = "")
        {
            for_remove = 5;
            this.module_type = module_type;
            Module = module;
            base.InitModuleImagesList(module);
            SQlInsert(module, module_type);
            return IsVersCompatible();
        }

        protected override void SQlInsert(CModule module, string module_type)
        {
            try
            {
                base.SQlInsert(module, module_type);

            }
            catch
            {
                string sql_insert = "update update_info set state = {1}, type = '{2}', old_softver = '{3}', com_part = '{4}', channel_part = '{5}' where id = {0} ";
                string sql = String.Format(sql_insert, Module.NodeID, 0, module_type, Module.SoftVersion, "", "");
                NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_insert.ExecuteNonQuery();
            }
        }

        protected override bool IsVersCompatible()
        {
            double version = Convert.ToDouble(CAuxil.AdaptFloat(Module.HardVersion));
            //Считываем версии железа для всех прошивок, чтобы "вычеркнуть" не совместимые
            foreach (var image in ImageList)
            {
                if (!image.HardwareList.Contains(version))
                {
                    continue;
                }
                CImage[] ar_image = { image, null };
                string key = "";
                if (image.OrderCode != "")
                {
                    key = $"[{image.OrderCode}] ";
                }
                key += image.Version;
                this.ImagesList.Add(key, ar_image);
            }

            ImageList.Clear();

            return ImagesList.Count != 0;
        }
    }

    public class TheNewsetPLC2Image : ImageCheck
    {
        //Список версий плат
        public List<double> GethardList = new List<double>();
        public TheNewsetPLC2Image(string module_type)
        {
            base.for_remove = 5;
            base.module_type = module_type;
        }

        public override bool Start(CModule module, string module_type)
        {
            SQlInsert(module, module_type);
            bool state = base.Start(module);
            return state;
        }
        protected override void SQlInsert(CModule module, string module_type)
        {
            try
            {
                string sql_insert = "insert into update_info(id, state, type, old_softver, com_part, channel_part) ";
                string sql_values = "values({0}, {1}, '{2}', '{3}', '{4}', '{5}')";
                string sql = String.Format(sql_insert + sql_values, module.NodeID, 0, module.Module_Name, module.SoftVersion, "", "No");
                NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_insert.ExecuteNonQuery();
            }
            catch
            {
                string sql_insert = "update update_info set state = 0, type = '{1}', old_softver = '{2}', com_part = '{3}', channel_part = '{4}' where id = '{0}'";
                string sql = String.Format(sql_insert, module.NodeID, module.Module_Name, module.SoftVersion, "", "No");
                NpgsqlCommand cmd_insert = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_insert.ExecuteNonQuery();
            }
        }

        //Если ничего не подходит
        protected override void StopThisUpdate(int status)
        {
            board_part = "com_part";
            base.StopThisUpdate(status);
        }

        //Проверка совместимости
        protected override bool IsVersCompatible()
        {
            return true;
        }
    }
}