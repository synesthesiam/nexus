using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WIICWrapper {

  public class Wiimotes {

    private static bool callbacksSet = false;

    private delegate void ButtonCallback(int id, int button);

    [DllImport("wiic_wrapper.dll")]
    private static extern int wrapper_connect(int timeout);

    [DllImport("wiic_wrapper.dll")]
    private static extern void wrapper_disconnect();

    [DllImport("wiic_wrapper.dll")]
    private static extern void wrapper_poll();

    [DllImport("wiic_wrapper.dll")]
    private static extern void set_callbacks(ButtonCallback bc);

    public static event WiiButtonEventHander ButtonClicked;

    public static int Connect(int timeout) {
      if (!callbacksSet) {
        set_callbacks(OnButton);
      }

      return (wrapper_connect(timeout));
    }

    public static void DisconnectAll() {
      wrapper_disconnect();
    }

    public static void Poll() {
      wrapper_poll();
    }

    private static void OnButton(int id, int button) {
      if (ButtonClicked != null) {
        ButtonClicked(null, new WiiButtonEventArgs() { Id = id, Button = button });
      }
    }
  }

  public delegate void WiiButtonEventHander(object sender, WiiButtonEventArgs args);

  public class WiiButtonEventArgs : EventArgs {
    public int Id { get; set; }
    public int Button { get; set; }
  }
}

