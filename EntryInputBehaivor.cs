using AbakConfigurator.Secure;
using Microsoft.Xaml.Behaviors;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using AbakConfigurator.Secure.Entry;

namespace AbakConfigurator
{
    public class EntryInputBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty InputTypeProperty = DependencyProperty.Register(nameof(Type), typeof(EntryType), typeof(EntryInputBehavior), (PropertyMetadata)new FrameworkPropertyMetadata((object)EntryType.Text));

        public EntryType Type
        {
            get => (EntryType)this.GetValue(EntryInputBehavior.InputTypeProperty);
            set => this.SetValue(EntryInputBehavior.InputTypeProperty, (object)value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.PreviewTextInput += new TextCompositionEventHandler(this.PreviewTextInput);
            this.AssociatedObject.PreviewKeyDown += new KeyEventHandler(this.PreviewKeyDown);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.PreviewTextInput -= new TextCompositionEventHandler(this.PreviewTextInput);
            this.AssociatedObject.PreviewKeyDown -= new KeyEventHandler(this.PreviewKeyDown);
        }

        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string input = textBox.Text.Insert(textBox.CaretIndex, e.Text);
            if (this.Type == EntryType.Integer)
            {
                Regex regex = new Regex("^-?[0-9]*$");
                e.Handled = !regex.IsMatch(input);
            }
            else if (this.Type == EntryType.Double)
            {
                Regex regex = new Regex("^-?[0-9]*\\.?[0-9]*$");
                e.Handled = !regex.IsMatch(input);
            }
            else
            {
                if (this.Type != EntryType.Phone)
                    return;
                Regex regex = new Regex("^\\+?[0-9]*$");
                e.Handled = !regex.IsMatch(input);
            }
        }

        private void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (this.Type != EntryType.Integer && this.Type != EntryType.Double && this.Type != EntryType.Phone || e.Key != Key.Space)
                return;
            e.Handled = true;
        }
    }
}