using System;
using System.Net;
using System.Net.Sockets;
using WIICWrapper;

namespace WIICServer {

  class Program {

    private static TcpClient client = null;
    private static byte[] termBuffer = new byte[1];

    static void Main(string[] args) {

      var port = Convert.ToInt32(args[0]);
      var timeout = Convert.ToInt32(args[1]);
      var buffer = new byte[12];

      client = new TcpClient();
      client.Connect(IPAddress.Loopback, port);
      client.Client.BeginReceive(termBuffer, 0, 1, SocketFlags.None,
                                 inputClient_DataReceived, null);

      Console.WriteLine("Connected to Nexus at 127.0.0.1:{0}", port);

      Wiimotes.ButtonClicked += Button;
      int numConnnected = Wiimotes.Connect(timeout);

      if (numConnnected > 0) {
        Console.WriteLine("{0} wiimote(s) found", numConnnected);

        CopyInt(ref buffer, 1, 0);
        CopyInt(ref buffer, numConnnected, 4);
        CopyInt(ref buffer, 0, 8);

        client.Client.Send(buffer);
        Wiimotes.Poll();
      }
      else {
        CopyInt(ref buffer, 1, 0);
        CopyInt(ref buffer, 0, 4);
        CopyInt(ref buffer, 0, 8);

        client.Client.Send(buffer);
        Console.WriteLine("No Wiimotes found");
      }
    }

    private static void CopyInt(ref byte[] buffer, int val, int offset) {
      var valBytes = BitConverter.GetBytes(val);

      for (int i = 0; i < 4; ++i) {
        buffer[offset + i] = valBytes[i];
      }
    }

    private static void Button(object sender, WiiButtonEventArgs args) {
      var buffer = new byte[12];
      CopyInt(ref buffer, 0, 0);
      CopyInt(ref buffer, args.Id, 4);
      CopyInt(ref buffer, args.Button, 8);

      //Console.WriteLine("{0}: {1}", args.Id, args.Button);
      client.Client.Send(buffer);
    }

    private static void inputClient_DataReceived(IAsyncResult result)
    {
      var bytesReceived = client.Client.EndReceive(result);

      if (bytesReceived > 0)
      {
        Console.WriteLine("Disconnecting");
        Wiimotes.DisconnectAll();
      }
    }

  }
}

