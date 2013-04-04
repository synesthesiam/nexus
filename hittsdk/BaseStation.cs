using System;
using System.Linq;
using System.Threading;
using System.ComponentModel;

using System.Collections.Generic;

namespace HITTSDK
{
    public class BaseStation
    {
        #region Fields

        protected log4net.ILog log = log4net.LogManager.GetLogger(typeof(BaseStation));

        protected string portName = "";        
        protected bool isConnected = false;

        protected Thread pollThread = null;
        protected AutoResetEvent connectedEvent = new AutoResetEvent(false);
        protected Exception threadException = null;
        protected IHITTAPI api = Platform.IsOSX ? ((IHITTAPI)new MacAPI()) : ((IHITTAPI)new DefaultAPI());

        #endregion

        #region Properties

        public bool IsConnected
        {
            get
            {
                return (isConnected);
            }
        }

        #endregion

        #region Events

        public event KeyReceivedEventHandler KeyReceived;

        #endregion

        #region Methods

        public void Connect(string portName)
        {
            this.portName = portName;
            
            Disconnect();

            log.DebugFormat("Connecting to {0}", portName);

            // Start polling thread
            pollThread = new Thread(new ThreadStart(PollPort));
            pollThread.IsBackground = true;

            threadException = null;
            connectedEvent.Reset();

            pollThread.Start();

            connectedEvent.WaitOne();

            if (threadException != null)
            {
                throw threadException;
            }
        }

        public void Disconnect()
        {
            if (isConnected)
            {
                log.Debug("Disconnecting");

                isConnected = false;
                pollThread.Join(100);
                pollThread.Abort();
            }

            pollThread = null;
        }

        protected void PollPort()
        {
            ISerialPort port = null;

            try
            {
                // Open port
                //if (Platform.IsOSX)
                if (false)
                {
                    port = new MacSerialPort();
                    log.Debug("Using OSX serial port");
                }
                else
                {
                    port = new DefaultSerialPort();
                    log.Debug("Using Mono serial port");
                }

                port.Open(portName);
				port.ReadTimeout = 100;
                connectedEvent.Set();

                byte[] buffer = new byte[10];
                isConnected = true;

                APIStatus status = APIStatus.Error;
                uint remoteId = 0;
                Keys key = Keys.None;

                // Start polling
                while (isConnected)
                {
                    ReadBytes(port, 10, ref buffer);

                    if (!isConnected)
                    {
                        break;
                    }

                    log.DebugFormat("Received bytes from serial port: {0}",
                            BitConverter.ToString(buffer));

                    status = api.inspect(buffer, ref remoteId, ref key);

					if (status == APIStatus.Error)
					{
						var doShift = true;
						
						do
						{
							if (doShift)
							{
								// Shift left by one position
		                        for (int i = 0; i < buffer.Length - 1; i++)
		                        {
		                            buffer[i] = buffer[i + 1];
								}

								doShift = false;
							}

							try
							{
								// Read new byte into the last position of the array
		                        buffer[buffer.Length - 1] = port.ReadByte();								
		
		                        // Inspect again
		                        status = api.inspect(buffer, ref remoteId, ref key);
								doShift = true;
							}
							catch(TimeoutException)
							{
								// Continue trying to read while connected
							}
	
						} while (isConnected && (status == APIStatus.Error));
					}

                    if (KeyReceived != null)
                    {
                        // Fire event
                        KeyReceived(this, new KeyReceivedEventArgs(remoteId, key, buffer));
                    }
                }

                port.Close();
            }
            catch (ThreadAbortException)
            {
                port.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
                connectedEvent.Set();
                log.Error("Serial port thread exception", ex);
            }

        }

		protected void ReadBytes(ISerialPort port, int count, ref byte[] buffer)
		{
			int bytesRead = 0;

			while (isConnected && (bytesRead < count))
			{
				try
				{
					buffer[bytesRead] = port.ReadByte();
					bytesRead++;
				}
				catch(TimeoutException)
				{
					// Continue trying to read while connected
				}
			}
		}

        #endregion
    }

    #region Delegates / Classes / Enumerations

    public delegate void KeyReceivedEventHandler(object sender, KeyReceivedEventArgs e);

    public class KeyReceivedEventArgs : EventArgs
    {
        protected uint remoteId = 0;
        protected Keys key = Keys.None;
        protected byte[] buffer = null;

        public uint RemoteId
        {
            get { return (remoteId); }
        }

        public Keys Key
        {
            get { return (key); }
        }

        public byte[] Buffer
        {
            get { return (buffer); }
        }

        public KeyReceivedEventArgs(uint remoteId, Keys key, byte[] buffer)
        {
            this.remoteId = remoteId;
            this.key = key;
            this.buffer = buffer;
        }
    }

    public enum APIStatus
    {
        OK = 0, Error = 1
    }

    public enum Keys
    {
        A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8, I = 9, J = 10,
        Forward = 0, Reverse = 11, None = 12
    }

    #endregion
}
