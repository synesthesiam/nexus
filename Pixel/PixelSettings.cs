using System;
using System.Drawing;
using System.Collections.Generic;

using Nexus.Shared;

namespace Pixel
{
	public class PixelSettings : DefaultGameSettings
	{
		public const int FileVersion = 1;

		public int MaxSize { get; set; }
		public int Players { get; set; }
		public List<FixedPixel> FixedPixels { get; protected set; }
		
		public PlayerSort PlayerSort { get; set; }
		public InitialState InitialState { get; set; }

		public PixelSettings()
		{
			FixedPixels = new List<FixedPixel>();
		}
	}
	
	public enum PlayerSort
	{
		Random, Clustered, Diffuse
	}
	
	public enum InitialState
	{
		Random, On, Off
	}

	public class FixedPixel
	{
		public int X { get; set; }
		public int Y { get; set; }
	}
}
