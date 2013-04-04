using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Nexus
{
	public static class WiimoteServer
	{
		#region Fields
		
		private static log4net.ILog logger = log4net.LogManager.GetLogger("Nexus.WiimoteServer");
		private static TcpListener server = new TcpListener(IPAddress.Loopback, 0);
		private static List<TcpClient> clients = new List<TcpClient>();
    private static IDictionary<TcpClient, byte[]> buffers = new Dictionary<TcpClient, byte[]>();
		
		#endregion
		
		#region Properties
		
		public static string Address { get; private set; }
		public static int Port { get; private set; }

    public delegate void ButtonEventHandler(int id, int button);
    public static event ButtonEventHandler ButtonClicked;
		
    public delegate void ConnectedEventHandler(int connected);
    public static event ConnectedEventHandler Connected;
		
		#endregion
		
		#region Public Methods
		
		public static void Start()
		{
			logger.Info("Starting wiimote server");
			
			server.Start();
			
			var endPoint = (IPEndPoint)server.LocalEndpoint;
			Address = endPoint.Address.ToString();
			Port = endPoint.Port;
			
			logger.DebugFormat("Wiimote server started at {0}", endPoint);
			
			server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
		}
		
    public static void DisconnectAll() {
      logger.Debug("Disconnecting wiimotes");
      SendToAll(new byte[] { 1 });
    }

		public static void Stop()
		{
			logger.Info("Stopping wiimote server");

      DisconnectAll();
			server.Stop();
			clients.Clear();
		}
		
		private static void SendToAll(byte[] buffer)
		{
			List<TcpClient> clientsToRemove = new List<TcpClient>();
			
			foreach (var client in clients)
			{
				try
				{
					client.Client.Send(buffer);
				}
				catch(SocketException ex)
				{
					logger.WarnFormat("Removing client ({0}): {1}",
						client.Client.LocalEndPoint, ex.Message);
						
					clientsToRemove.Add(client);
				}
			}
			
			// Remove offending clients
			foreach (var client in clientsToRemove)
			{
				clients.Remove(client);
			}
		}
		
		#endregion
		
		#region Utility Methods
		
		private static void AcceptTcpClient(IAsyncResult result)
		{
			var newClient = server.EndAcceptTcpClient(result);
			logger.DebugFormat("New client accepted at {0}", newClient.Client.LocalEndPoint);
			
			clients.Add(newClient);

      buffers[newClient] = new byte[12];
      newClient.Client.BeginReceive(buffers[newClient], 0, 12, SocketFlags.None, ReceiveData, newClient);
			server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
		}

    private static void ReceiveData(IAsyncResult result)
    {
      var client = (TcpClient)result.AsyncState;
      int numBytes = client.Client.EndReceive(result);

      if (numBytes > 0)
      {
        if (BitConverter.ToInt32(buffers[client], 0) == 0) {
          if (ButtonClicked != null) {
            ButtonClicked(BitConverter.ToInt32(buffers[client], 4), BitConverter.ToInt32(buffers[client], 8));
          }
        }
        else {
          if (Connected != null) {
            Connected(BitConverter.ToInt32(buffers[client], 4));
          }
        }
      }

      client.Client.BeginReceive(buffers[client], 0, 12, SocketFlags.None, ReceiveData, client);
    }
		
		#endregion
	}
}
