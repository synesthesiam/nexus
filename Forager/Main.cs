using System;
using System.Linq;
using System.Collections.Generic;

using NDesk.Options;

using OpenTK;
using OpenTK.Graphics;

using Nexus.Shared;

namespace Forager
{
	public class Program
	{
		public static void Main(string[] args)
		{
			if (!log4net.LogManager.GetRepository().Configured)
			{
				log4net.Config.BasicConfigurator.Configure();
			}
			
			log4net.ILog logger = log4net.LogManager.GetLogger("Forager.Program");
			
			logger.Info("Starting up Forager");
			
			var settings = new ForagerSettings()
			{
				IsDebugMode = false,
				ScreenWidth = DisplayDevice.Default.Width,
				ScreenHeight = DisplayDevice.Default.Height,
				Fullscreen = false,
				Players = 0, Port = 0, Plots = 0,
				TravelTime = 3, FoodRate = 3, GameSeconds = 120
			};
			
			logger.Debug("Parsing command-line options");
			
			// Parse command-line options
			bool showHelp = false;
			
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

				{ "plots=", "Number of plots for players to forage in [2-8] (default: 8)",
					v => settings.Plots = Convert.ToInt32(v) },

				{ "travel-time=", "Delay in seconds when traveling between plots (default: 3)",
					v => settings.TravelTime = Convert.ToDouble(v) },

				{ "food-rate=", "Number of food items given out per second (default: 3)",
					v => settings.FoodRate = Convert.ToInt32(v) },

				{ "plot-probabilities=", "Relative probabilities of plots (default: same)",
          v => settings.PlotProbabilities.AddRange(CLIObject.FromMultiArrayString<double>(v).Select(x => x.ToList())) },

				{ "game-duration=", "Number of seconds that the entire game lasts (default: 120)",
          v => settings.GameSeconds = Convert.ToInt32(v) },

        { "probability-shift-times=", "Number of seconds before the next plot probability set is used (default: never)",
          v => settings.ProbabilityShiftTimes = CLIObject.FromArrayString<int>(v).ToList() }
			
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

			if (settings.Plots < 2)
			{
				settings.Plots = 8;
			}

			if (settings.PlotProbabilities.Count < 1)
			{
				logger.Debug("Assuming all plots have equal probability");
				
				// Equal probabilities if none are given by user
				settings.PlotProbabilities.Add(Enumerable.Range(0, settings.Plots).Select(i => 1.0).ToList());
			}
			else
			{
        foreach (var probabilities in settings.PlotProbabilities)
        {
          if (probabilities.Count >= settings.Plots)
          {
            continue;
          }

          var forgottenPlots = probabilities.Count - settings.Plots;
				
          logger.WarnFormat("Last {0} plot(s) are assumed to have zero probability",
                            forgottenPlots);
				
          // Assume non-specified plots have zero probability
          probabilities.AddRange(Enumerable.Range(0, forgottenPlots).Select(i => 0.0).ToList());
        }
			}

      if (settings.ProbabilityShiftTimes.Count < (settings.PlotProbabilities.Count - 1))
      {
        logger.Warn("Some plot probabilities will not be used (see probability-shift-times)");
      }

			if (settings.IsDebugMode)
			{
				logger.Info("Debug mode is enabled");
			}

			using (var game = new ForagerWindow(settings))
			{
				// Start game
				logger.Debug("Running game loop");
				game.Run(0, 0);
			}
		}
	}
}