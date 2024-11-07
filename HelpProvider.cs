using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TopicIds;

namespace AbakConfigurator
{
    static class HelpProvider
    {
        static HelpProvider()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(FrameworkElement), 
                new CommandBinding(ApplicationCommands.Help, 
                new ExecutedRoutedEventHandler(Executed),
                new CanExecuteRoutedEventHandler(CanExecute))
                );
        }

        public static int GetID(string alias)
        {
            HelpTopicIds topicID;
            if (!Enum.TryParse(alias, out topicID))
                return -1;

            return (int)topicID;
        }

        public static string GetHelpAlias(DependencyObject obj)
        {
            return (string)obj.GetValue(HelpAliasProperty);
        }

        public static void SetHelpAlias(DependencyObject obj, string value)
        {
            obj.SetValue(HelpAliasProperty, value);
        }

        static private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            String alias = HelpProvider.GetHelpAlias(senderElement);
            if ((alias != null) && (HelpProvider.GetID(alias) != -1))
                e.CanExecute = true;
        }

        static private void Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int id = HelpProvider.GetID(HelpProvider.GetHelpAlias(sender as FrameworkElement));
            System.Windows.Forms.Help.ShowHelp(null, "AbakConfigurator.chm", System.Windows.Forms.HelpNavigator.TopicId, id.ToString());
        }

        public static readonly DependencyProperty HelpAliasProperty = DependencyProperty.RegisterAttached("HelpAlias", typeof(string), typeof(HelpProvider));
    }
}
