using System;
using System.IO.Ports;

namespace HITTSDK
{
    public interface ISerialPort
    {
        void Open(string portName);
        byte ReadByte();
        void Close();
        int ReadTimeout { get; set; }
    }

    public class ConnectionFailedException : Exception
    {
    }
}
