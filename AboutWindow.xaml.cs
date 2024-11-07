using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Navigation;
using System.Drawing;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private Dictionary<string, string> LinksForClicks = new Dictionary<string, string>
        {
            { "Telegram", "https://t.me/incomsystem"},
            {"Site", "https://incomsystem.ru/abak-controllers/" },
            {"VK", "https://vk.com/club212260150"}
        };

        private Dictionary<string, string> LinksForCopy = new Dictionary<string, string>
        {
            {"Support", "support.abak@incomsystem.ru" },
            {"Sales", "sales.abak@incomsystem.ru" },
            {"Site", "incomsystem.ru/abak-controllers/" },
            { "Telegram", "https://t.me/incomsystem"},
            {"VK", "https://vk.com/club212260150"},
        };

        public AboutWindow()
        {
            InitializeComponent();
            this.versionLabel.Content = CGlobal.GetResourceValue("l_versionLabel") + ": " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
           // ReleaseLabel.Content = CGlobal.GetResourceValue("l_releasedateLable") + ": 25.07.2022";
        }

        public static void ShowAboutWindow(Window parent)
        {
            AboutWindow window = new AboutWindow();
            window.Owner = parent;
            window.ShowDialog();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            var Button = (Button)sender;
            Process.Start(new ProcessStartInfo(LinksForClicks[Button.Name]));
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void MenuItem_MouseLeftButtonUp(object sender, RoutedEventArgs  e)
        {
            MenuItem GetMenuItem = (MenuItem)sender;
            Clipboard.SetText(LinksForCopy[GetMenuItem.Name]);
        }

        private void ContextMenu_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}

