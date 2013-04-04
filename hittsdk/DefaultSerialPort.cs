using System;
using System.IO.Ports;

namespace HITTSDK
{
    public class DefaultSerialPort : ISerialPort, IDisposable
    {
        protected SerialPort port = null;

        public int ReadTimeout
        {
            get { return (port.ReadTimeout); }
			set { port.ReadTimeout = value; }
        }

        public void Open(string portName)
        {
            port = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One);
            port.Open();
        }

        public byte ReadByte()
        {
            return (Convert.ToByte(port.ReadByte()));
        }

        public void Close()
        {
            if ((port == null) || !port.IsOpen)
            {
                return;
            }

            port.Close();
            port = null;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
