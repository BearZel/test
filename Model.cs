using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AbakConfigurator.Soe
{
    public class SourceItem
    {
        public bool IsSelected { get; set; } = true;
        public string SourceFile { get; set; }
    }

    public class ModuleItem
    {
        public bool IsSelected { get; set; } = false;
        public string Module
        {
            get { return string.Format("Module {0}", ModuleNumber); }
        }
        public int ModuleNumber { get; set; } = 0;
    }

    public class Model : INotifyPropertyChanged
    {
        private ObservableCollection<Event> events;
        private int progress = 0;
        private Visibility progressVisibility = Visibility.Collapsed;
        private CanLoggerConfig canLoggerConfig;

        public ObservableCollection<SourceItem> Sources { get; set; }
        public ObservableCollection<ModuleItem> Modules { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now.Date;
        public DateTime EndDate { get; set; } = DateTime.Now.Date.AddDays(1);
        public string EventName { get; set; } = "";

        public string LogPath
        {
            get { return canLoggerConfig.Props.LogName; }
            set { canLoggerConfig.Props.LogName = value; }
        }

        public int LogSizeMb
        {
            get { return canLoggerConfig.Props.LogSizeMb; }
            set { canLoggerConfig.Props.LogSizeMb = value; }
        }

        public int DelayRecovery
        {
            get { return canLoggerConfig.Props.DelayRecoverySec; }
            set { canLoggerConfig.Props.DelayRecoverySec = value; }
        }

        public int UpdateDelay
        {
            get { return canLoggerConfig.Props.EmptyEventsDelaySec; }
            set { canLoggerConfig.Props.EmptyEventsDelaySec = value; }
        }

        public int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private bool Filter(Event e)
        {
            Func<DateTime, bool> dataRange = (date) =>
            {
                return date >= StartDate && date <= EndDate;
            };
            foreach (var item in Sources)
            {
                if (item.IsSelected && item.SourceFile == e.Source)
                {
                    return e.Message.ToLower().Contains(EventName.ToLower()) && dataRange(e.Timestamp);
                }
            }
            return false;
        }

        public ICollectionView FilteredEvents
        {
            get
            {
                var source = CollectionViewSource.GetDefaultView(events);
                source.Filter = p => Filter((Event)p);
                return source;
            }
        }

        public Visibility ProgressVisibility
        {
            get => progressVisibility;
            set
            {
                progressVisibility = value;
                OnPropertyChanged(nameof(ProgressVisibility));
            }
        }

        public Model()
        {
            events = new ObservableCollection<Event>();
            Sources = new ObservableCollection<SourceItem>();
            Modules = new ObservableCollection<ModuleItem>();
            canLoggerConfig = new CanLoggerConfig(CGlobal.Session.SSHClient);

            for (int i = 1; i < 101; i++)
            {
                Modules.Add(new ModuleItem
                {
                    // TODO когда у нас будет несколько vxcan
                    // нужно будет брать не первый а нужный
                    IsSelected = canLoggerConfig.Props.Triggers.First().HasModule(i),
                    ModuleNumber = i
                });
            }

            LoadAsync();
        }

        public async void LoadAsync()
        {
            Sources.Clear();

            EventsLoader loader = new EventsLoader();
            ProgressVisibility = Visibility.Visible;
            events = await Task.Run(() =>
            {
                var progress = new Progress<int>(value => { Progress = value; });
                loader.LoadFiles(progress);

                var ev = new ObservableCollection<Event>();
                foreach (List<Event> le in loader.Events.Values)
                {
                    le.ForEach(e => ev.Add(e));
                }
                return ev;
            });

            ProgressVisibility = Visibility.Collapsed;
            
            loader.Sources.ForEach(s =>
            {
                Sources.Add(new SourceItem
                {
                    IsSelected = true,
                    SourceFile = s
                });
            });

            OnPropertyChanged(nameof(FilteredEvents));
        }

        public void SaveConfiguration()
        {
            var t = canLoggerConfig.Props.Triggers[0];
            t.CanName = "vxcan1";
            t.Modules.Clear();
            foreach (var item in Modules)
            {
                if (item.IsSelected)
                {
                    t.AddModule(item.ModuleNumber);
                }
            }
            canLoggerConfig.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
