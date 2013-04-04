using System;
using System.Collections.Generic;

namespace Nexus {

  public interface IWiimotes {

    event WiiButtonEventHander ButtonClicked;

    int Connect(int );
    void DisconnectAll();
    void SetLED(int id, int led);
  }

  public delegate void WiiButtonEventHander(object sender, WiiButtonEventArgs args);

  public class WiiButtonEventArgs : EventArgs {
    public int Id { get; set; }
    public int Button { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
  }
}
