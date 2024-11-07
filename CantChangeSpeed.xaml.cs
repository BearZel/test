using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace AbakConfigurator
{

    public partial class CantChangeSpeed : Window
    {
        public ObservableCollection<ModuleParams> ModulesParams { get; set; } = new ObservableCollection<ModuleParams>();

        public CantChangeSpeed(List<CModule> modules = null)
        {
            Fill(modules);
            InitializeComponent();
            dataGridspeed.Columns.Add(new DataGridTextColumn());
        }

        private void Fill(List<CModule> modules = null)
        {
            foreach (CModule module in modules)
            {
                ModuleParams moduleparams = new ModuleParams();
                moduleparams.Node_Id = module.NodeID;
                moduleparams.Type = module.TypeString;
                ModulesParams.Add(moduleparams);
            }
        }
    }

    public class ModuleParams : Changed
    {
        //Адрес модуля
        public UInt32 Node_Id { get; set; } = 0;
        //Тип модуля
        public string Type { get; set; } = "";
    }
}
