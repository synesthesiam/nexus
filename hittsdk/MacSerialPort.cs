using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace HITTSDK
{	
	public class MacSerialPort : ISerialPort, IDisposable
	{
		protected int fd = -1, readTimeout = 0;
		
		[DllImport("libMacSerial")]
		private static extern int openserial(string device);
	
		[DllImport("libMacSerial")]
		private static extern void closeserial(int fd);
	
		[DllImport("libMacSerial")]
		private static extern int readserial(int fd);
		
		[DllImport("libMacSerial")]
		private static extern void settimeout(int fd, int timeout);
		
		protected bool IsConnected
		{
			get { return (fd >= 0); }
		}
		
		public int ReadTimeout
		{
			get { return (readTimeout); }
			set
			{
				readTimeout = value;
				settimeout(fd, readTimeout);
			}
		}
		
		public void Open(string portName)
		{
			fd = openserial(portName);
			
			if (!IsConnected)
			{
				throw new IOException(string.Format("Connection to {0} failed", portName));
			}
		}
		
		public byte ReadByte()
		{
			if (!IsConnected)
			{
				throw new InvalidOperationException("Not connected");
			}

			var result = readserial(fd);

			if (result < 1)
			{
				throw new TimeoutException();
			}
			
			return (Convert.ToByte(result));
		}
		
		public void Close()
		{
			if (IsConnected)
			{
				closeserial(fd);
				fd = -1;
			}
		}
		
		public void Dispose()
		{
			Close();
		}
	}
}
