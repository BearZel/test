using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AbakConfigurator.IEC
{
    public class ProjectLoader
    {
        private XDocument _xDocument;
        public ProjectLoader(XDocument xDoc)
        {
            _xDocument = xDoc;
        }

        public void Load(out Settings settings)
        {
            settings = new Settings();
            var server = _xDocument.XPathSelectElement("//IEC/Server");
            settings.K = int.Parse(server.Attribute("k").Value);
            settings.W = int.Parse(server.Attribute("w").Value);
            settings.T0 = int.Parse(server.Attribute("t0").Value);
            settings.T1 = int.Parse(server.Attribute("t1").Value);
            settings.T2 = int.Parse(server.Attribute("t2").Value);
            settings.T3 = int.Parse(server.Attribute("t3").Value);
            settings.Port = int.Parse(server.Attribute("port").Value);
            settings.UseBuffer = XmlConvert.ToBoolean(server.Attribute("use_buffer").Value);
            settings.ByteOrder = (ByteOrder)int.Parse(server.Attribute("byte_order").Value);
            settings.IECSync = XmlConvert.ToBoolean(server.Attribute("iec_sync").Value);
            settings.IECSyncTimeout = int.Parse(server.Attribute("iec_sync_timeout").Value);
        }

        public void Load(out List<ServerRow> rows)
        {
            rows = new List<ServerRow>();
            foreach (XElement obj in _xDocument.XPathSelectElements("//IEC/Server/Objects/Object"))
            {
                ServerRow row = new ServerRow();
                row.IOA = (int)obj.Attribute("ioa");
                row.DataType = (DataType)int.Parse(obj.Attribute("data_type").Value);
                row.PeriodicCycle = (int)obj.Attribute("periodic_cycle");
                row.GroupID = (GroupID)int.Parse(obj.Attribute("group_id").Value);
                
                float deadband = (float)obj.Attribute("deadband");
                row.Deadband = Constants.FromNormalizeValueDeadband(row.DataType, deadband);
                row.ModbusAddress = (int)obj.Attribute("modbus_address");
                row.PollPeriod = (int)obj.Attribute("poll_period");

                rows.Add(row);
            }
        }
    }
}
