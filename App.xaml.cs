using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Threading;
namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static List<CultureInfo> languages = new List<CultureInfo>();

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public static void Main()
        {
            try

            {
                AbakConfigurator.App app = new AbakConfigurator.App();
                //app.Run();
                app.InitializeComponent();

#if DEBUG
                try
                {
                    app.Run();
                }
                catch (Exception ex)
                {
                    CAuxil.WriteStackTrace(ex);
                }
#else
                app.Run();
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Во время запуска конфигуратора произошла ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static CultureInfo EnglishLang
        {
            get;
            set;
        }

        public static CultureInfo RussianLang
        {
            get;
            set;
        }

        public static List<CultureInfo> Languages
        {
            get
            {
                return languages;
            }
        }

        public App()
        {
            languages.Clear();
            EnglishLang = new CultureInfo(CGlobal.EU);
            languages.Add(EnglishLang);

            RussianLang = new CultureInfo(CGlobal.RU);
            languages.Add(RussianLang);
        }

        public static CultureInfo Language
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) 
                    return;

                //1. Меняем язык приложения:
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;

                //2. Создаём ResourceDictionary для новой культуры
                ResourceDictionary dict = new ResourceDictionary();
                switch (value.Name)
                {
                    case CGlobal.RU:
                        dict.Source = new Uri(String.Format("Resources/lang.{0}.xaml", value.Name), UriKind.Relative);
                        break;

                    default:
                        dict.Source = new Uri("Resources/lang.xaml", UriKind.Relative);
                        break;
                }

                //3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
                ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.StartsWith("Resources/lang.")
                                              select d).First();
                if (oldDict != null)
                {
                    int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                //4. Вызываем евент для оповещения всех окон.
                //LanguageChanged(Application.Current, new EventArgs());
                CGlobal.Settings.Language = Language.Name;
                CGlobal.Settings.Save();
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CommandLineClass.ProcessCommandLine(e.Args);
            Language = RussianLang;
        }

        private void Application_NavigationFailed(object sender,
       System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            if (e.Exception is System.Net.WebException)
            {
                MessageBox.Show("Сайт " + e.Uri.ToString() + " не доступен :(");
                e.Handled = true;
            }
        }
    }
}
