using System;
using System.IO;
using System.Collections.Generic;

using Gdk;

namespace Nexus
{
	public static class Images
	{
		private static IDictionary<int, Pixbuf> playerImageCache = new Dictionary<int, Pixbuf>();
		
		public static readonly string PlayerImagePath = Path.Combine(
			Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]),
			Path.Combine("etc", "player_images")
		);
		
		public static int GetPlayerImageCount()
		{
			return (Directory.GetFiles(PlayerImagePath, "*.png").Length);
		}
		
		public static Pixbuf GetPlayerImage(int index)
		{
			if (!playerImageCache.ContainsKey(index))
			{
				playerImageCache[index] = new Pixbuf(Path.Combine(PlayerImagePath,
					string.Format("player{0}.png", index)), 72, 72
				);
			}
			
			return (playerImageCache[index]);
		}
	}
}
