using System;
using WIICWrapper;

namespace WIICTest {

  class Program {

    static void Main(string[] args) {

      Wiimotes.ButtonClicked += Button;
      int numConnnected = Wiimotes.Connect(5);

      if (numConnnected > 0) {
        Console.WriteLine("{0} wiimote(s) found", numConnnected);
        Console.WriteLine("Press ENTER to quit");
        Console.ReadLine();
        Wiimotes.DisconnectAll();
      }
      else {
        Console.WriteLine("No Wiimotes found");
      }
    }

    static void Button(object sender, WiiButtonEventArgs args) {
      Console.WriteLine("{0}: {1}", args.Id, args.Button);
    }
  }
}

