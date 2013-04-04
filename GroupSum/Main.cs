using System;
using System.Linq;
using System.Collections.Generic;

using NDesk.Options;

using OpenTK;
using OpenTK.Graphics;

using Nexus.Shared;

namespace GroupSum
{
	public class Program
	{
		public static void Main(string[] args)
		{
			if (!log4net.LogManager.GetRepository().Configured)
			{
				log4net.Config.BasicConfigurator.Configure();
			}
			
			log4net.ILog logger = log4net.LogManager.GetLogger("GroupSum.Program");
			
			logger.Info("Starting up Group Sum");
			
			var settings = new GroupSumSettings()
			{
				IsDebugMode = false,
				ScreenWidth = DisplayDevice.Default.Width,
				ScreenHeight = DisplayDevice.Default.Height,
				Fullscreen = false,
				Players = 0, Port = 0,
				FirstRoundSeconds = 0, RoundSeconds = 10,
				ShowNumericFeedback = false, UsePreviousRoundInput = false
			};
			
			logger.Debug("Parsing command-line options");
			
			// Parse command-line options
			bool showHelp = false;
			int rangeStart = 0, rangeEnd = 9;
			
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
					
				{ "players=", "Number of players (required)",
					v => settings.Players = Convert.ToInt32(v) },
					
				{ "port=", "Network port of input server",
					v => settings.Port = Convert.ToInt32(v) },
					
				{ "data-file=", "Path to the output data file",
					v => settings.DataFilePath = v },
					
				{ "description=", "Text to display at the top of the screen during gameplay",
					v => settings.GameDescription = v },

				{ "target-number=", "The number the group is attempting to guess (default: random)",
					v => settings.TargetNumber = Convert.ToInt32(v) },

				{ "first-round-seconds=", "Length of the first round in seconds (default: round-seconds)",
					v => settings.FirstRoundSeconds = Convert.ToInt32(v) },

				{ "round-seconds=", "Length of each round in seconds (default: 10)",
					v => settings.RoundSeconds = Convert.ToInt32(v) },

				{ "range-start=", "Start of number range, multiplied by player count (default: 0)",
					v => rangeStart = Convert.ToInt32(v) },

				{ "range-end=", "End of number range, multiplied by player count (default: 9)",
					v => rangeEnd = Convert.ToInt32(v) },

				{ "numeric-feedback", "If set, players are show how far they were from the answer (default: false)",
					v => settings.ShowNumericFeedback = !string.IsNullOrEmpty(v) },

				{ "previous-input", "If not set, player answers are reset each round (default: false)",
					v => settings.UsePreviousRoundInput = !string.IsNullOrEmpty(v) }
			};

			options.Parse(args);
			
			if (showHelp)
			{
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			if (settings.Players < 1)
			{
				logger.Fatal("players option is required");
				return;
			}

			// Flip range if necessary
			if (rangeStart > rangeEnd)
			{
				int rangeTemp = rangeStart;
				rangeStart = rangeEnd;
				rangeEnd = rangeTemp;
			}

			settings.MinNumber = settings.Players * rangeStart;
			settings.MaxNumber = settings.Players * rangeEnd;

			if (settings.TargetNumber < 1)
			{
				settings.TargetNumber = new Random().Next(settings.MinNumber - 1, settings.MaxNumber) + 1;
			}

			if (settings.FirstRoundSeconds <= 0)
			{
				settings.FirstRoundSeconds = settings.RoundSeconds;
			}

			if (settings.IsDebugMode)
			{
				logger.Info("Debug mode is enabled");
			}

			using (var game = new GroupSumWindow(settings))
			{
				// Start game
				logger.Debug("Running game loop");
				game.Run(0, 0);
			}
		}
	}
}