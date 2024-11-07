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
    public class ProjectSaver
    {
        private XDocument _xDocument;
        public ProjectSaver(XDocument doc)
        {
            _xDocument = doc;
            _xDocument.Add(new XElement("IEC"));
        }
        public void Save(Settings settings)
        {
            var server = new XElement("Server");
            server.Add(new XAttribute("k", settings.K));
            server.Add(new XAttribute("w", settings.W));
            server.Add(new XAttribute("t0", settings.T0));
            server.Add(new XAttribute("t1", settings.T1));
            server.Add(new XAttribute("t2", settings.T2));
            server.Add(new XAttribute("t3", settings.T3));
            server.Add(new XAttribute("port", settings.Port));
            server.Add(new XAttribute("use_buffer", settings.UseBuffer));
            server.Add(new XAttribute("byte_order", (int)settings.ByteOrder));
            server.Add(new XAttribute("iec_sync", settings.IECSync));
            server.Add(new XAttribute("iec_sync_timeout", (int)settings.IECSyncTimeout));

            _xDocument.XPathSelectElement("//IEC").Add(server);
        }

        public void Save(List<ServerRow> serverRows)
        {
            var objects = new XElement("Objects");
            foreach (ServerRow sr in serverRows)
            {
                var obj = new XElement("Object");
                obj.Add(new XAttribute("ioa", sr.IOA));
                obj.Add(new XAttribute("data_type", (int)sr.DataType));
                obj.Add(new XAttribute("periodic_cycle", sr.PeriodicCycle));
                obj.Add(new XAttribute("group_id", (int)sr.GroupID));

                float deadband = Constants.ToNormalizeValueDeadband(sr.DataType, sr.Deadband);
                obj.Add(new XAttribute("deadband", deadband));
                obj.Add(new XAttribute("modbus_address", sr.ModbusAddress));
                obj.Add(new XAttribute("modbus_size", Constants.IECDataTypeSizeInBytes(sr.DataType)));
                obj.Add(new XAttribute("poll_period", sr.PollPeriod));

                objects.Add(obj);
            }


            _xDocument.XPathSelectElement("//IEC/Server").Add(objects);
        }
    }
}
