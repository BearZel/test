using System.Windows;
using System.Windows.Controls;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for PrefixTextBox.xaml
    /// </summary>
    public partial class PrefixTextBox : UserControl
    {
        /// <summary>
        /// Gets or sets the Prefix
        /// </summary>
        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        /// <summary>
        /// Prefix dependency property
        /// </summary>
        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register("Prefix", typeof(string),
              typeof(PrefixTextBox), new PropertyMetadata(""));

        /// <summary>
        /// Gets or sets the Text
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Text dependency property
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string),
              typeof(PrefixTextBox), new PropertyMetadata(""));

        public PrefixTextBox()
        {
            InitializeComponent();
        }
    }
}
