using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.Toolbox
{
    internal class Utilities
    {
        public static string GetSHA256(string text)
        {
            var hashed_password = new StringBuilder();

            using (var hash = SHA256Managed.Create())
            {
                byte[] hash_data = hash.ComputeHash(Encoding.UTF8.GetBytes(text));

                foreach (byte element in hash_data)
                {
                    hashed_password.Append(element.ToString("x2"));
                }
            }

            return hashed_password.ToString();
        }

        public static string GetLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system");
        }
    }
}
