using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Npgsql;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для EthernetStatisticWindow.xaml
    /// </summary>
    public partial class WaitingForUpdateWindow : Window
    {
        ////Таймер обновления данных
        //private DispatcherTimer timer = new DispatcherTimer();
        //DispatcherTimer timer_2 = new DispatcherTimer();
        ////Контекст потока для работы с пользовательским интерфейсом приложения из других потоков
        //private SynchronizationContext uiContext;
        ////Проценты
        //private String percentage = "";
        //private int AmountOfModules = 0;
        private bool EnableClosing { get; set; } = false;
        ////Закрываем принудительно
        //bool HardClose = false;
        public WaitingForUpdateWindow()
        {
            InitializeComponent();
            //AmountOfModules = amount_of_modules;
            //this.uiContext = SynchronizationContext.Current;
            ////Инициализация таймера
            //this.timer.IsEnabled = true;
            //this.timer.Interval = new TimeSpan(0, 0, 0, 5, 0);
            //this.timer.Tick += new EventHandler(this.timer_Tick);
            //int num = 0;


            //timer_2.Interval = new TimeSpan(0, 0, 0, 15, 0);
            //timer_2.IsEnabled = true;
            //timer_2.Tick += timer_Tick_2;
            
        }


        //private async void timer_Tick(object sender, EventArgs e)
        //{
        //    this.timer.IsEnabled = true;
            
        //    bool should_be_closed = CGlobal.CurrState.ModulesAmount == AmountOfModules || HardClose;
        //    if (should_be_closed)
        //    {

        //        if (HardClose)
        //        {
        //            this.uiContext.Post(this.enableTimer, false);
        //            enableclosing = true;
        //            this.Close();
        //        }
        //    }
        //    await Task.Run(() => updateStatistic());
        //}

        //private async void timer_Tick_2(object sender, EventArgs e)
        //{
        //    MessageBox.Show("close");
        //    timer_2.Tick -= timer_Tick_2;
        //    HardClose = true;
        //}
        public void HardClose()
        {
            EnableClosing = true;
            this.Close();
        }
        //Функция для отмены закрытия конфигуратора во время обновления
        private void ClosingOff(object sender, CancelEventArgs e)
        {
            if(!EnableClosing)
                e.Cancel = true; //Отменяется закрытие
        }


    }
}
