using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using HITTSDK;

namespace SerialTest
{
    public class Program
    {
        private static bool isConnected = false;

        public static void Main(string[] args) {
            
            if (args.Length < 1) {
                Console.WriteLine("Usage SerialTest.exe <port name> [default]");
                return;
            }

            bool forceDefault = args.Length < 2;
            string portName = args[0];
            IHITTAPI api = Platform.IsOSX ? ((IHITTAPI)new MacAPI()) : ((IHITTAPI)new DefaultAPI());

            ISerialPort port = null;

            try
            {
                // Open port
                port = new DefaultSerialPort();
                Console.WriteLine("Using Mono serial port");

                port.Open(portName);
				port.ReadTimeout = 100;

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

                    Console.WriteLine("Received bytes from serial port: {0}",
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
                }

                port.Close();
            }
            catch (ThreadAbortException)
            {
                port.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serial port thread exception: {0}", ex);
            }
        }

		static void ReadBytes(ISerialPort port, int count, ref byte[] buffer)
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

    }  // class Program

}  // namespace SerialTest
