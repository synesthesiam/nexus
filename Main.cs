using System;
using Gtk;

using log4net.Config;

namespace Nexus
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			BasicConfigurator.Configure();
			log4net.ILog logger = log4net.LogManager.GetLogger("Nexus.Application");
			
			logger.Info("Starting Nexus");
			
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
			
			logger.Info("Stopping Nexus");
		}
	}
}