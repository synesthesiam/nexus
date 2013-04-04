using System;
using System.Linq;

using Gtk;

using Nexus.Shared;

namespace Nexus
{
	public class DialogEditGame : DialogNewGame
	{
		public DialogEditGame(IGameInfo gameInfo) : base()
		{
			this.Title = "Edit Game";

			if (gameInfo is PixelGameInfo)
			{
				SetGameInfo((PixelGameInfo)gameInfo);
			}
			else if (gameInfo is FlatlandGameInfo)
			{
				SetGameInfo((FlatlandGameInfo)gameInfo);
			}
			else if (gameInfo is GroupSumGameInfo)
			{
				SetGameInfo((GroupSumGameInfo)gameInfo);
			}
			else if (gameInfo is ForagerGameInfo)
			{
				SetGameInfo((ForagerGameInfo)gameInfo);
			}
		}
	}
}
