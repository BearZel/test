using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace AbakConfigurator.network
{
    public class InterfaceSettings
    {
        public struct Route
        {
            public Route(string from, string to)
            {
                this.from = from;
                this.to = to;
            }

            [JsonProperty("from")]
            public string from;
            [JsonProperty("to")]
            public string to;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Ethernets
        {
            Eth0, Eth1, Eth2, Eth3, Eth4, Eth5
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpeedEnum
        {
            S10, S100
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DuplexEnum
        {
            Full, Half
        }

        [JsonProperty("eth")]
        public Ethernets Eth { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("netmask")]
        public string Netmask { get; set; }

        [JsonProperty("gateway")]
        public string Gateway { get; set; }

        [JsonProperty("speed")]
        public SpeedEnum Speed { get; set; }

        [JsonProperty("duplex")]
        public DuplexEnum Duplex { get; set; }

        [JsonProperty("global_ip")]
        public string GlobalIp { get; set; }

        [JsonProperty("dns")]
        public List<string> Dns { get; set; } = new List<string>();

        [JsonProperty("static_routes")]
        public List<Route> StaticRoutes { get; set; } = new List<Route>();
    }
}
