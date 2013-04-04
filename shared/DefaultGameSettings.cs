using System;

namespace Nexus.Shared
{
	public abstract class DefaultGameSettings
	{
		public const int PlayerButtonCount = 10;

		public bool IsDebugMode { get; set; }
		
		public int ScreenWidth { get; set; }
		public int ScreenHeight { get; set; }
		public bool Fullscreen { get; set; }
		
		public int Port { get; set; }
		public string DataFilePath { get; set; }
		public string GameDescription { get; set; }
	}
}
