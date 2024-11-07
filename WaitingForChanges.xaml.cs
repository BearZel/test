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
    public partial class WaitingForChanges : Window, INotifyPropertyChanged
    {
        //Таймер обновления данных
        private DispatcherTimer timer = new DispatcherTimer();
        //Контекст потока для работы с пользовательским интерфейсом приложения из других потоков
        private SynchronizationContext uiContext;
        //Закрываем принудительно
        bool HardClose = false;
        bool firstloop = true;
        public WaitingForChanges()
        {
            InitializeComponent();
            this.uiContext = SynchronizationContext.Current;
            //Инициализация таймера
            this.timer.IsEnabled = true;
            this.timer.Interval = new TimeSpan(0, 0, 0, 10, 0);
            this.timer.Tick += new EventHandler(this.timer_Tick);
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            if (firstloop)
            {
                await Task.Run(() => updateTimer());
                firstloop = false;
                return;
            }
            this.timer.IsEnabled = false;
            HardClose = true;
            this.Close();

        }

        //Функция для отмены закрытия конфигуратора во время обновления
        private void ClosingOff(object sender, CancelEventArgs e)
        {
            if(!HardClose)
                e.Cancel = true; //Отменяется закрытие
        }

        private void enableTimer(object state)
        {
            this.timer.IsEnabled = Convert.ToBoolean(state);
        }

        /// <summary>
        /// Функция обновляет статистику о интерфейсе
        /// </summary>
        private void updateTimer()
        {
            //double count_perc = Math.Round((Convert.ToDouble(CGlobal.CurrState.ModulesAmount) / AmountOfModules) * 100);
            //this.Percentage = "Идёт подготовка модулей " + count_perc.ToString() + "%";
            this.uiContext.Post(this.enableTimer, true);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => updateTimer());
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
