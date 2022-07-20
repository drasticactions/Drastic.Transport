using System;
using System.Collections.Generic;
using System.Text;

namespace Drastic.Transport
{
    [Serializable]
    public class TcpInitializationData
    {
        public TcpInitializationData(string ipAddress = "127.0.0.1", int port = 8888)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new ArgumentException(nameof(ipAddress));
            }

            if (port <= 0)
            {
                throw new ArgumentException(nameof(port));
            }

            this.IpAddress = ipAddress;
            this.Port = port;
        }

        public int Port { get; }

        public string IpAddress { get; }
    }
}
