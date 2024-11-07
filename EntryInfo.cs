using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace AbakConfigurator.Secure.Entry
{
    public class EntryInfo : INotifyPropertyChanged
    {
        EntryType m_Type = EntryType.None;
        EntryState m_State = EntryState.None;
        bool m_Changed = false;
        string m_Title = "";
        string m_Description = "";
        string m_Value = "";
        bool m_Developer = false;
        
        public EntryType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public EntryState State
        {
            get => m_State;
            set
            {
                m_State = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public bool Changed
        {
            get => m_Changed;
            set
            {
                m_Changed = value;
                OnPropertyChanged(nameof(Changed));
            }
        }

        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Description
        {
            get => m_Description;
            set
            {
                m_Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public bool Developer
        {
            get => m_Developer;
            set
            {
                m_Developer = value;
                OnPropertyChanged(nameof(Developer));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            if (name != nameof(Changed))
            {
                Changed = true;
            }
        }
    }
}
