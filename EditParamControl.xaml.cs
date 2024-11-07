using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Threading;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for EditParamControl.xaml
    /// </summary>
    public abstract partial class EditParamControl : UserControl
    {
        public CBaseParam param;
        private Boolean isMouseWithinScope = false;
        private Boolean isEditing = false;
        protected Boolean enableEdit = false;
        public bool IsUpdateBlocked = false;
        public EditParamControl()
        {
            InitializeComponent();
        }

        public abstract void showEdit(Boolean enable);

        private void textBlockMouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            this.showEdit(true);
        }

        private void showViewControls()
        {
            if (!toggleButton.IsVisible)
            {
                if (GetType().Name == "DescriptionParamControl")
                {
                    descTextBlock.Visibility = Visibility.Visible;
                    textBlock.Visibility = Visibility.Hidden;
                }
                else
                {
                    descTextBlock.Visibility = Visibility.Hidden;
                    textBlock.Visibility = Visibility.Visible;
                }
            }
            this.textBox.Visibility = Visibility.Hidden;
            this.comboBox.Visibility = Visibility.Hidden;
            this.OKbutton.Visibility = Visibility.Hidden;
            this.Cancelbutton.Visibility = Visibility.Hidden;
            this.isEditing = false;
        }

        protected void showEditControls()
        {
            if (this.param == null)
                return;

            bool isTextBox = param.Options == null || param.Options.Options.Count == 0
                || descTextBlock.IsVisible;

            if (isTextBox)
            {
                //Привязка текстового элемента к параметру
                BindingOperations.ClearBinding(this.textBox, TextBox.TextProperty);
                Binding bind = new Binding();
                bind.Source = this.param;
                bind.Mode = BindingMode.OneTime;
                if (descTextBlock.IsVisible)
                    bind.Path = new PropertyPath("Description");
                else
                    bind.Path = new PropertyPath("ScaleValue");
                this.textBox.SetBinding(TextBox.TextProperty, bind);

                this.textBox.Visibility = Visibility.Visible;
                this.textBox.Focus();
                this.textBox.SelectAll();
                textBlock.Visibility = descTextBlock.Visibility = this.comboBox.Visibility = Visibility.Hidden;
            }
            else
            {
                //Привязка ComboBox  параметру
                List<COption> list = this.param.Options.Options.Select(kvp => kvp.Value).ToList();
                this.comboBox.ItemsSource = list;
                this.comboBox.DisplayMemberPath = "GuiValue";
                this.comboBox.SelectedValuePath = "Value";
                this.comboBox.SelectedValue = this.param.Value.ToString().ToLower();
                this.comboBox.Visibility = Visibility.Visible;
                this.comboBox.Focus();
                comboBox.IsDropDownOpen = true;
                this.textBox.Visibility = Visibility.Hidden;
            }
            this.OKbutton.Visibility = Visibility.Visible;
            this.Cancelbutton.Visibility = Visibility.Visible;
            this.isEditing = true;
        }
        private bool CheckLimits()
        {
            CModuleParam moduleParam = this.param as CModuleParam;
            if (moduleParam == null)
                return true;

            double[] limits = moduleParam.GetScaleLimits();
            if (limits.Count() != 2)
                return true;

            double numb = Convert.ToDouble(CAuxil.AdaptFloat(textBox.Text));

            if (limits[0] <= numb && numb <= limits[1])
                return true;

            this.showViewControls();
            string msg = $"Значение параметра \"{this.param.Name}'\" " +
                $"должно находится в диапазоне от {limits[0]}" +
                $" до {limits[1]}";
            string titlle = "Выход за границы диапазона!";
            MessageBox.Show(msg, titlle, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        //Тут запись параметра
        private void applyValue()
        {
            try
            {
                if (this.textBox.IsVisible)
                    SetValueFromTextBox();
                else
                    SetValueWithOptions();

                this.param.ManualChanged = true;
                this.showViewControls();

            }
            catch
            {

            }
        }
        private void SetValueFromTextBox()
        {
            switch (GetType().Name)
            {
                case "DescriptionParamControl":
                    {
                        CModuleParam moduleParam = this.param as CModuleParam;
                        moduleParam.Description = textBox.Text;
                        moduleParam.DescriptionChanged = true;
                        break;
                    }
                case "ListItemParamControl":
                    {
                        if (!CheckLimits())
                            break;

                        if (textBox.Text.Length != 0)
                        {
                            int index = textBox.Text.Length - 1;
                            if (textBox.Text[index] == ' ')
                                textBox.Text = textBox.Text.Remove(index);
                        }
                        this.param.ScaleValue = this.textBox.Text;
                        this.param.WriteValue = this.param.Value;
                        break;
                    }
            }
        }
        private void SetValueWithOptions()
        {
            if (toggleButton.IsVisible)
            {
                this.param.WriteValue = toggleButton.IsChecked.ToString();
                this.param.Value = this.param.WriteValue;
            }
            else
            {
                this.param.WriteValue = this.comboBox.SelectedValue.ToString();
                this.param.Value = this.param.WriteValue;
            }
        }
        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.applyValue();
            IsUpdateBlocked = false;
        }
        private void CancelButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            if (toggleButton.IsVisible)
                IsUpdateBlocked = false;
            this.showViewControls();
        }

        private void textBoxLostKeyboardFocus_Handler(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!this.isMouseWithinScope)
                this.showViewControls();
        }

        private void comboBoxLostKeyboardFocus_Handler(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!this.isMouseWithinScope)
                this.showViewControls();
        }

        private void GridMouseEnter_Handler(object sender, MouseEventArgs e)
        {
            this.isMouseWithinScope = true;
        }

        private void GridMouseLeave_Handler(object sender, MouseEventArgs e)
        {
            if (!this.isEditing)
                this.enableEdit = false;
            this.isMouseWithinScope = false;
        }

        private void GridKeyUp_Handler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    this.applyValue();
                    break;

                case Key.Escape:
                    this.showViewControls();
                    break;
            }
            e.Handled = true;
        }

        private void toggleButton_Click(object sender, RoutedEventArgs e)
        {
            string state = (toggleButton.IsChecked == true).ToString();
            IsUpdateBlocked = true;
            this.OKbutton.Visibility = Visibility.Visible;
            this.Cancelbutton.Visibility = Visibility.Visible;
        }
    }

    public partial class ListItemParamControl : EditParamControl
    {
        private ListViewItem listItem;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.listItem = CAuxil.GetDependencyObjectFromVisualTree(this, typeof(ListViewItem)) as ListViewItem;
            this.listItem.Tag = this;
            this.param = listItem.DataContext as CBaseParam;
            descTextBlock.Visibility = Visibility.Hidden;
            textBlock.Visibility = Visibility.Visible;
            if (param == null)
                return;

            if (param.Options == null || param.ReadOnly || param.CpuTag || param.Type != ParamType.BOOLEAN)
                return;

            //Привязка ComboBox  параметру
            List<COption> list = this.param.Options.Options.Select(kvp => kvp.Value).ToList();
            if (list.Count != 2)
                return;

            List<string> words = new List<string>()
            {
                { "вкл" }, { "выкл" }, { "да" }, { "нет" }
            };
            bool isVisible = false;
            foreach (string str in words)
            {
                isVisible = list.Any(x => x.GuiValue.ToLower().Contains(str));
                if (!isVisible)
                    continue;

                toggleButton.Visibility = Visibility.Visible;
                localeTextBlock.Visibility = Visibility.Visible;
                string state = param.Value.ToString().ToLower();
                if (state == "false" || state == "true")
                {
                    toggleButton.IsChecked = Convert.ToBoolean(state);
                    this.textBlock.Visibility = Visibility.Hidden;
                    localeTextBlock.Text = param.GuiValue;
                }
                break;
            }
        }

        public override void showEdit(Boolean enable)
        {
            if (this.param.ReadOnly)
                return;

            if (this.listItem.IsSelected && (this.enableEdit || enable))
                this.showEditControls();
            this.enableEdit = this.listItem.IsSelected;
        }
    }
    public partial class DescriptionParamControl : EditParamControl
    {
        private ListViewItem listItem;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            textBlock.Visibility = Visibility.Hidden;
            descTextBlock.Visibility = Visibility.Visible;
            this.listItem = CAuxil.GetDependencyObjectFromVisualTree(this, typeof(ListViewItem)) as ListViewItem;
            this.listItem.Tag = this;
            this.param = listItem.DataContext as CBaseParam;
            CModuleParam moduleParam = param as CModuleParam;
            descTextBlock.Text = moduleParam.Description;
        }

        public override void showEdit(Boolean enable)
        {
            if (CGlobal.CurrState.AssemblyInt < 540)
                return;

            if (this.listItem.IsSelected && (this.enableEdit || enable))
                this.showEditControls();
            this.enableEdit = this.listItem.IsSelected;
        }
    }
}
