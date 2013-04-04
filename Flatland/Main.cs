using System;
using System.Linq;
using System.Collections.Generic;

using NDesk.Options;

using OpenTK;
using OpenTK.Graphics;

using Nexus.Shared;

namespace Flatland
{
  public class Program
  {
    public static void Main(string[] args)
    {
      if (!log4net.LogManager.GetRepository().Configured)
      {
        log4net.Config.BasicConfigurator.Configure();
      }
      
      log4net.ILog logger = log4net.LogManager.GetLogger("Flatland.Program");
      
      logger.Info("Starting up Flatland");
      
      var settings = new FlatlandSettings()
      {
        IsDebugMode = false,
        ScreenWidth = DisplayDevice.Default.Width,
        ScreenHeight = DisplayDevice.Default.Height,
        Fullscreen = false,
        Tiles = 0, Players = 0, Port = 0, PlayerSort = PlayerSort.Random,
        CollisionBehavior = CollisionBehavior.Allow,
        GameType = FlatlandGameType.Normal
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
          
        { "tiles=", "Number of tiles in a row or column (default: maximum)",
          v => settings.Tiles = Convert.ToInt32(v) },
          
        { "players=", "Number of players (required)",
          v => settings.Players = Convert.ToInt32(v) },
          
        { "port=", "Network port of input server",
          v => settings.Port = Convert.ToInt32(v) },
          
        { "data-file=", "Path to the output data file",
          v => settings.DataFilePath = v },
          
        { "player-sort=", "Initial arrangement of players (Random, SpreadOut, Clustered)",
          v => settings.PlayerSort = (PlayerSort)Enum.Parse(typeof(PlayerSort), v) },
          
        { "collision-behavior=", "Behavior for when players collide (Block, Allow)",
          v => settings.CollisionBehavior = (CollisionBehavior)Enum.Parse(typeof(CollisionBehavior), v) },
          
        { "team-info=", "Information about a team in the format [Index,Theme,MoveSeconds,Kind,Wrap] (Kind = Normal, Predator, Prey)",
          v => settings.Teams.AddRange(CLIObject.FromString<FlatlandTeamInfo>(v, "TeamIndex", "ThemeName", "MoveSeconds", "Kind", "WrappedMovement", "ScoringSystem")) },

        { "fixed-tiles=", "List of fixed tiles in the format [X1,Y1][X2,Y2]...",
          v => settings.FixedTiles.UnionWith(CLIObject.FromString<FixedTile>(v, "X", "Y")) },

        { "description=", "Text to display at the top of the screen during gameplay",
          v => settings.GameDescription = v },

        { "game-type=", "Type of Flatland game to play (Normal, NQueens)",
          v => settings.GameType = (FlatlandGameType)Enum.Parse(typeof(FlatlandGameType), v) }
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

      if (settings.Teams.Count < 1)
      {
        logger.Fatal("at least team one is required");
        return;
      }
      else
      {
        // Set team names based on index
        foreach (var team in settings.Teams)
        {
          team.TeamName = ((char)(((int)'A') + team.TeamIndex)).ToString();
          team.Theme = Themes.GetTheme((ThemeColor)Enum.Parse(typeof(ThemeColor), team.ThemeName));
        }
      }

      // Calculate number of pixels
      var requestedPixels = settings.Tiles * settings.Tiles;
      
      if (requestedPixels < settings.Players)
      {
        settings.Tiles = (int)Math.Ceiling(Math.Sqrt(settings.Players));
        logger.WarnFormat("Setting number of tiles to {0}x{0} to accomodate players", settings.Tiles);
      }
      
      // Make sure there are enough tiles for all players
      if ((settings.CollisionBehavior == CollisionBehavior.Block) &&
          (((settings.Tiles * settings.Tiles) - settings.FixedTiles.Count) < settings.Players))
      {
        logger.Fatal("Not enough tiles for players!");
        return;
      }

      if (settings.IsDebugMode)
      {
        logger.Info("Debug mode is enabled");
      }

      using (var game = new FlatlandWindow(settings))
      {
        // Start game
        logger.Debug("Running game loop");
        game.Run(0, 0);
      }
    }
  }
}

