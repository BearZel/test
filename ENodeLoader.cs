using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AbakConfigurator.IEC
{
    public class ENodeLoader
    {
        private XDocument _xDocument;
        public ENodeLoader(XDocument xDocument)
        {
            _xDocument = xDocument;
        }

        public void Load(out Settings settings)
        {
            settings = new Settings();

            var server = _xDocument.XPathSelectElement("//ADHConfiguration/Device/Application/ApplicationConfiguration/ENodeInstance/Settings");
            settings.K = int.Parse(server.Attribute("K").Value);
            settings.W = int.Parse(server.Attribute("w").Value);
            settings.T0 = int.Parse(server.Attribute("t0").Value);
            settings.T1 = int.Parse(server.Attribute("t1").Value);
            settings.T2 = int.Parse(server.Attribute("t2").Value);
            settings.T3 = int.Parse(server.Attribute("t3").Value);

            var ipsettings = _xDocument.XPathSelectElement("//ADHConfiguration/Device/Application/ApplicationConfiguration/ENodeInstance/ProtocolInstance/Listen");
            settings.Port = int.Parse(ipsettings.Attribute("IPPort").Value);

            //settings.UseBuffer = XmlConvert.ToBoolean(server.Attribute("use_buffer").Value);
            //settings.ByteOrder = (ByteOrder)int.Parse(server.Attribute("byte_order").Value);
            //settings.IECSync = XmlConvert.ToBoolean(server.Attribute("iec_sync").Value);
            //settings.IECSyncTimeout = int.Parse(server.Attribute("iec_sync_timeout").Value);
        }

        struct Row
        {
            public string Ioa { get; set; }
            public string Datatype { get; set;}
            public string InterrogationMask { get; set;}
            public string CyclicTime { get; set;}
            public string DeadBand { get; set;}
            public string PollPeriod { get; set;}
            public string Address { get; set;}
            public string Time { get; set; }
        };

        public void Load(out List<ServerRow> rows)
        {
            Dictionary<string, Row> rowDict = new Dictionary<string, Row>();

            var elements = _xDocument.XPathSelectElements("//ADHConfiguration/Device/Application/ApplicationConfiguration/ENodeInstance/ProtocolInstance/IED/References");
            foreach (XElement e in elements)
            {
                string id = e.Attribute("objectID").Value;

                var iec = e.XPathSelectElement("./IEC10X");

                string ioa = iec.Attribute("IOA").Value;
                string datatype = iec.Attribute("iecDataType").Value; 
                string interrogationMask = iec.Attribute("interrogationMask")?.Value;
                string cyclicTime = iec.Attribute("cyclicTime")?.Value;
                string deadBand = iec.Attribute("deadband")?.Value;
                string time = iec.Attribute("time")?.Value;

                rowDict.Add(id, new Row
                {
                    Ioa = ioa,
                    Datatype = datatype,
                    InterrogationMask = interrogationMask,
                    CyclicTime = cyclicTime,
                    DeadBand = deadBand,
                    Time = time
                });
            }

            var slave = _xDocument.XPathSelectElement("//Application/ApplicationConfiguration/Server/Slave");
            string pollPeriod = slave.Attribute("pollPeriod")?.Value;

            var generates = slave.XPathSelectElements("./Generates");
            foreach(XElement e in generates)
            {
                string id = e.Attribute("objectID")?.Value;

                var modBus = e.XPathSelectElement("./Modbus");
                string address = modBus.Attribute("address")?.Value;

                if (rowDict.ContainsKey(id))
                {
                    var row = rowDict[id];
                    row.PollPeriod = pollPeriod;
                    row.Address = address;
                    rowDict[id] = row;
                }
            }

            rows = new List<ServerRow>();
            foreach (Row r in rowDict.Values)
            {
                ServerRow row = new ServerRow();
                int result = 0;

                row.IOA = int.Parse(r.Ioa);
                row.DataType = ToDataType(r.Datatype + r.Time);

                int.TryParse(r.CyclicTime, out result);
                row.PeriodicCycle = result;

                row.GroupID = ToGroupID(r.InterrogationMask);

                int.TryParse(r.DeadBand, out result);
                row.Deadband = result;

                int.TryParse(r.Address, out result);
                row.ModbusAddress = result;

                int.TryParse(r.PollPeriod, out result);
                row.PollPeriod = result;

                rows.Add(row);
            }
        }

        DataType ToDataType(string s)
        {
            switch (s)
            {
                case "Bitstring320" : return DataType.M_BO_NA_1;
                case "Bitstring3224" : return DataType.M_BO_TA_1;
                case "Bitstring3256" : return DataType.M_BO_TB_1;
                //------------------------------------------------	
                case "MV-Float0" : return DataType.M_ME_NC_1;
                case "MV-Float24" : return DataType.M_ME_TC_1;
                case "MV-Float56" : return DataType.M_ME_TF_1;
                //------------------------------------------------
                case "MV-Normalized0" : return DataType.M_ME_NA_1;
                case "MV-Normalized24" : return DataType.M_ME_TA_1;
                case "MV-Normalized56" : return DataType.M_ME_TD_1;
                //------------------------------------------------
                case "MV-Normalized-NoQuality0" : return DataType.M_ME_ND_1;
                //------------------------------------------------
                case "MV-Scaled0" : return DataType.M_ME_NB_1;
                case "MV-Scaled24" : return DataType.M_ME_TB_1;
                case "MV-Scaled56" : return DataType.M_ME_TE_1;
                //------------------------------------------------
                case "IT0" : return DataType.M_IT_NA_1;
                case "IT24" : return DataType.M_IT_TA_1;
                case "IT56" : return DataType.M_IT_TB_1;
                //------------------------------------------------
                case "Step0" : return DataType.M_ST_NA_1;
                case "Step24" : return DataType.M_ST_TA_1;
                case "Step56" : return DataType.M_ST_TB_1;
                //------------------------------------------------
                case "SP0" : return DataType.M_SP_NA_1;
                case "SP24" : return DataType.M_SP_TA_1;
                case "SP56" : return DataType.M_SP_TB_1;
                //------------------------------------------------
                case "DP0" : return DataType.M_DP_NA_1; break;
                case "DP24" : return DataType.M_DP_TA_1; break;
                case "DP56" : return DataType.M_DP_NA_1;
                //------------------------------------------------
                case "Param-MV-Normalized" : return DataType.M_ME_NA_1;
                case "Param-MV-Scaled" : return DataType.M_ME_NB_1;
                case "Param-MV-Float" : return DataType.M_ME_NC_1;
                //------------------------------------------------
                case "C_Bitstring32" : return DataType.C_BO_TA_1;
                case "C_MV-Float" : return DataType.C_SE_NC_1;
                case "C_MV-Scaled" : return DataType.C_SE_NB_1;
                //------------------------------------------------
                case "C_SP" : return DataType.C_SC_NA_1;
                case "C_DP" : return DataType.C_DC_NA_1;
                case "C_Step" : return DataType.C_RC_NA_1;
                case "C_MV-Normalized" : return DataType.C_SE_NA_1;

                //case "MV-EventProtect24" : return DataType.M_EP_TA_1;

                case "MV-Double0" : return DataType.M_IT_ND_1;
                case "MV-Double56" : return DataType.M_IT_TD_1;
                case "MV-Int640" : return DataType.M_ME_NO_1;
                case "MV-Int6456" : return DataType.M_ME_TO_1;
                case "MV-UInt640" : return DataType.M_ME_NX_1;
                case "MV-UInt6456" : return DataType.M_ME_TX_1;
                default:
                    break;
            }

            return DataType.M_ME_TF_1;
        }

        GroupID ToGroupID(string s)
        {
            switch (s)
            {
                // VALUE
                case "0x10000" : return GroupID.CS101_INTERROGATED_DEFAULT;
                case "0x1" : return GroupID.CS101_INTERROGATED_1;
                case "0x2" : return GroupID.CS101_INTERROGATED_2;
                case "0x4" : return GroupID.CS101_INTERROGATED_3;
                case "0x8" : return GroupID.CS101_INTERROGATED_4;
                case "0x10" : return GroupID.CS101_INTERROGATED_5;
                case "0x20" : return GroupID.CS101_INTERROGATED_6;
                case "0x40" : return GroupID.CS101_INTERROGATED_7;
                case "0x80" : return GroupID.CS101_INTERROGATED_8;
                case "0x100" : return GroupID.CS101_INTERROGATED_9;
                case "0x200" : return GroupID.CS101_INTERROGATED_10;
                case "0x400" : return GroupID.CS101_INTERROGATED_11;
                case "0x800" : return GroupID.CS101_INTERROGATED_12;
                case "0x1000" : return GroupID.CS101_INTERROGATED_13;
                case "0x2000" : return GroupID.CS101_INTERROGATED_14;
                case "0x4000" : return GroupID.CS101_INTERROGATED_15;
                case "0x8000" : return GroupID.CS101_INTERROGATED_16;
                default:
                    break;
            }
            return GroupID.CS101_INTERROGATED_DEFAULT;
        }
    }
}
