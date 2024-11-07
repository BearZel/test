using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.IEC
{
    public enum ByteOrder
    {
        ABCD,
        CDAB,
        BADC,
        DCAB
    }

    public class Settings
    {
        public int K { get; set; } = 12;
        public int W { get; set; } = 8;
        public int T0 { get; set; } = 30;
        public int T1 { get; set; } = 15;
        public int T2 { get; set; } = 10;
        public int T3 { get; set; } = 20;
        public int Port { get; set; } = 2404;
        public bool UseBuffer { get; set; } = false;
        public ByteOrder ByteOrder { get; set; } = ByteOrder.CDAB;
        public bool IECSync { get; set; } = true;
        public int IECSyncTimeout { get; set; } = 60;

        static public Array ByteOrders { get { return Enum.GetValues(typeof(ByteOrder)); } }
    }
}
