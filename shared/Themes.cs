using System;
using System.Drawing;
using System.Collections.Generic;

namespace Nexus.Shared
{
	public static class Themes
	{
		#region Fields
		
		private static Theme[] themes =
		{
			new Theme(0xFF, 0xCC, 0xCC, 0xCC, 0x66, 0x66, 0x66, 0x00, 0x00),
			new Theme(0xCC, 0xFF, 0xCC, 0x66, 0xCC, 0x66, 0x00, 0x66, 0x00),
			new Theme(0xCC, 0xCC, 0xFF, 0x66, 0x66, 0xCC, 0x00, 0x00, 0x66),
			new Theme(0xFF, 0xFF, 0xCC, 0x66, 0x66, 0x00, 0x33, 0x33, 0x00),
			new Theme(0x33, 0x33, 0x33, 0x66, 0x66, 0x66, 0xFF, 0xFF, 0xFF),
			new Theme(0xFF, 0x66, 0x00, 0x66, 0x33, 0x00, 0x55, 0x22, 0x00),
			new Theme(0xCC, 0x00, 0xFF, 0x33, 0x00, 0x66, 0xFF, 0xD5, 0xF6)
		};
		
		#endregion
		
		#region Properties
		
		public static int ThemeCount
		{
			get { return (themes.Length); }
		}
		
		public static Theme Red
		{
			get { return (themes[(int)ThemeColor.Red]); }
		}
		
		public static Theme Green
		{
			get { return (themes[(int)ThemeColor.Green]); }
		}
		
		public static Theme Blue
		{
			get { return (themes[(int)ThemeColor.Blue]); }
		}
		
		public static Theme Yellow
		{
			get { return (themes[(int)ThemeColor.Yellow]); }
		}

		public static Theme Black
		{
			get { return (themes[(int)ThemeColor.Black]); }
		}

		public static Theme Orange
		{
			get { return (themes[(int)ThemeColor.Orange]); }
		}

		public static Theme Purple
		{
			get { return (themes[(int)ThemeColor.Purple]); }
		}
		
		#endregion
		
		#region Methods
		
		public static Theme GetTheme(ThemeColor color)
		{
			return (themes[(int)color]);
		}
		
		public static Theme GetTheme(int themeIndex)
		{
			return (themes[themeIndex]);
		}
		
		#endregion
	}
	
	public class Theme
	{
		public Color Background { get; set; }
		public Color Border { get; set; }
		public Color Foreground { get; set; }
		
		public Theme(Color background, Color border, Color foreground)
		{
			this.Background = background;
			this.Border = border;
			this.Foreground = foreground;
		}
		
		public Theme(byte bgR, byte bgG, byte bgB,
			byte bdR, byte bdG, byte bdB,
			byte fgR, byte fgG, byte fgB)
		{
			this.Background = Color.FromArgb(bgR, bgG, bgB);
			this.Border = Color.FromArgb(bdR, bdG, bdB);
			this.Foreground = Color.FromArgb(fgR, fgG, fgB);
		}
	}
	
	public enum ThemeColor
	{
		Red = 0, Green = 1, Blue = 2, Yellow = 3, Black = 4, Orange = 5, Purple = 6
	}
}
