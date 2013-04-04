using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using NDesk.Options;

using OpenTK;
using OpenTK.Graphics;

using Nexus.Shared;

namespace Pixel
{
	public class Program
	{
		public static void Main(string[] args)
		{
			if (!log4net.LogManager.GetRepository().Configured)
			{
				log4net.Config.BasicConfigurator.Configure();
			}
			
			log4net.ILog logger = log4net.LogManager.GetLogger("Pixel.Program");
			
			logger.Info("Starting up Pixel");
			
			var settings = new PixelSettings()
			{
				IsDebugMode = false,
				ScreenWidth = DisplayDevice.Default.Width,
				ScreenHeight = DisplayDevice.Default.Height,
				Fullscreen = false,
				MaxSize = 0, Players = 0, Port = 0, PlayerSort = PlayerSort.Random
			};
			
			logger.Debug("Parsing command-line options");
			
			// Parse command-line options
			bool showHelp = false, doClusterTest = false;
			
			var options = new OptionSet()
			{
				{ "h|?|help", "Show this help message",
					v => showHelp = !string.IsNullOrEmpty(v) },

				{ "debug", "Enable debug mode (random commands can be issued with 'R')",
					v => settings.IsDebugMode = !string.IsNullOrEmpty(v) },
					
				{ "screen-width=", "Screen width in pixels (default: current)",
					v => settings.ScreenWidth = Convert.ToInt32(v) },
					
				{ "screen-height=", "Screen heigh in pixels (default: current)",
					v => settings.ScreenHeight = Convert.ToInt32(v) },
					
				{ "full-screen", "Enables full-screen mode",
					v => settings.Fullscreen = !string.IsNullOrEmpty(v) },
					
				{ "max-size=", "Maximum number of tiles in a row or column",
					v => settings.MaxSize = Convert.ToInt32(v) },
					
				{ "players=", "Number of players (required)",
					v => settings.Players = Convert.ToInt32(v) },
					
				{ "port=", "Network port of input server",
					v => settings.Port = Convert.ToInt32(v) },
					
				{ "data-file=", "Path to the output data file",
					v => settings.DataFilePath = v },
					
				{ "player-sort=", "Initial arrangement of players (Random, Clustered, Diffuse)",
					v => settings.PlayerSort = (PlayerSort)Enum.Parse(typeof(PlayerSort), v) },
					
				{ "initial-state=", "Initial state of pixels (Random, On, Off)",
					v => settings.InitialState = (InitialState)Enum.Parse(typeof(InitialState), v) },

				{ "cluster-test", "Tests the effectiveness of the clustering algorithm",
					v => doClusterTest = !string.IsNullOrEmpty(v) },

				{ "fixed-pixels=", "List of fixed pixels in the format [X1,Y1][X2,Y2]...",
					v => settings.FixedPixels.AddRange(CLIObject.FromString<FixedPixel>(v, "X", "Y")) },

				{ "description=", "Text to display at the top of the screen during gameplay",
					v => settings.GameDescription = v }
			};
			
			options.Parse(args);

			if (showHelp)
			{
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			if (doClusterTest)
			{
				for (int players = 2; players <= settings.Players; players++)
				{
					var tiles = (int)Math.Floor(Math.Sqrt(players * PixelSettings.PlayerButtonCount));					
					var testBoard = ClusteredBoard.Create(players, tiles);
					
					logger.InfoFormat("{0} player(s): {1} stragglers", players,
					                  ClusteredBoard.GetStragglerCount(ref testBoard));
				}
				
				return;
			}				
			
			if (settings.Players < 1)
			{
				logger.Fatal("players option is required");
				return;
			}

			if (settings.IsDebugMode)
			{
				logger.Info("Debug mode is enabled");
			}
				
			using (var game = new PixelWindow(settings))
			{
				// Start game
				logger.Debug("Running game loop");
				game.Run(0, 0);
			}
		}
	}
}