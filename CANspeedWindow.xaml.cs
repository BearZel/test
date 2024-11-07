using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text;


namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for CANspeedWindow.xaml
    /// </summary>
    public partial class CANspeedWindow : Window
    {
        private int savedSpeed;
        private int defaultSpeed;
        private JObject settings;
        private ObservableCollection<int> speed;
        public ObservableCollection<int> Speed { get => speed; }
        public int CurrentSpeed { get; set; }
        public CANspeedWindow(Stream stream)
        {
            LoadSpeedFromCPU(stream);
            InitializeComponent();
        }
        
        private void LoadSpeedFromCPU(Stream stream)
        {
            StreamReader sr = new StreamReader(stream);
            settings = JObject.Parse(sr.ReadToEnd());
            JArray speedVariants = (JArray)settings["speed_variants"];
            speed = new ObservableCollection<int>(speedVariants.Select(token => (int)token));
            defaultSpeed = (int)settings["default"];
            CurrentSpeed = savedSpeed = (int)settings["current"];
        }
        private void Refresh(object sender, RoutedEventArgs e)
        {
            ComboBoxCAN.SelectedItem = savedSpeed;
        }
        private void ResetToDefault(object sender, RoutedEventArgs e)
        {
            ComboBoxCAN.SelectedItem = defaultSpeed;
        }
        private void Accept(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Службы контроллера и модули ввода/вывода будут перезагружены. " +
                "Вы уверены, что хотитие продолжить", "Перезагрузка контроллера", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Change CAN speed"));
            settings["current"] = CurrentSpeed;
            
            CGlobal.Session.SSHClient.WriteFile("/opt/abak/A:/assembly/configs/can_speed.json", 
                CAuxil.StringToStream($"{settings}", Encoding.GetEncoding(1251)));
            this.DialogResult = true;
        }
    }
}