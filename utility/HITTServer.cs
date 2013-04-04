using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Nexus
{
	public static class HITTServer
	{
		#region Fields
		
		private static log4net.ILog logger = log4net.LogManager.GetLogger("Nexus.HITTServer");
		private static TcpListener server = new TcpListener(IPAddress.Loopback, 0);
		private static List<TcpClient> clients = new List<TcpClient>();
		
		#endregion
		
		#region Properties
		
		public static string Address { get; private set; }
		public static int Port { get; private set; }
		
		#endregion
		
		#region Public Methods
		
		public static void Start()
		{
			logger.Info("Starting input server");
			
			server.Start();
			
			var endPoint = (IPEndPoint)server.LocalEndpoint;
			Address = endPoint.Address.ToString();
			Port = endPoint.Port;
			
			logger.DebugFormat("Input server started at {0}", endPoint);
			
			server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
		}
		
		public static void Stop()
		{
			logger.Info("Stopping input server");
			
			server.Stop();
			clients.Clear();
		}
		
		public static void SendToAll(byte[] buffer)
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
			server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
		}
		
		#endregion
	}
}
