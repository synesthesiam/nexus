using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Drawing;

using Nexus.Shared;

namespace Nexus
{
  public class FlatlandGameInfo : IGameInfo 
  {
    public int? Tiles { get; set; }
    public string CollisionBehavior { get; set; }
    public IList<FlatlandTeamInfo> Teams { get; protected set; }
    public List<Point> FixedTiles { get; protected set; }
    public string GameType { get; set; }
    
    public FlatlandGameInfo()
    {
      Teams = new List<FlatlandTeamInfo>();
      FixedTiles = new List<Point>();
    }
    
    public FlatlandGameInfo(XElement element) : this()
    {
      Load(element);
    }
  
    #region IGameInfo Members
    
    public string GameName
    {
      get { return ("Flatland"); }
    }

    public string GameDescription { get; set; }
    
    public IDictionary<string, string> GetArguments()
    {
      var arguments = new Dictionary<string, string>();
      
      if (Tiles.HasValue)
      {
        arguments["tiles"] = Tiles.Value.ToString();
      }

      arguments["team-info"] = string.Join
        (string.Empty, Teams.Select(t => CLIObject.ToString(t, "TeamIndex", "ThemeName", "MoveSeconds", "Kind", "WrappedMovement", "ScoringSystem")).ToArray());
      
      arguments["collision-behavior"] = CollisionBehavior;
      arguments["game-type"] = GameType;
      
      if (FixedTiles.Count > 0)
      {
        arguments["fixed-tiles"] = string.Join(string.Empty,
         FixedTiles.Select(t => CLIObject.ToString(t, "X", "Y")).ToArray());
      }
      
      return (arguments);
    }
    
    public void Load(XElement element)
    {
      var tilesElement = element.Element("tiles");
      Tiles = (tilesElement != null) ? Convert.ToInt32(tilesElement.Value) : (int?)null;      
      CollisionBehavior = element.Element("collisionBehavior").Value;

      var gameTypeElement = element.Element("gameType");
      GameType = (gameTypeElement != null) ? gameTypeElement.Value : "Normal";

      // Load teams
      var teamsElement = element.Element("teams");
      Teams.Clear();

      if (teamsElement != null)
      {
        foreach (var teamElement in teamsElement.Elements("team"))
        {
          var kindElement = teamElement.Element("kind");
          var wrapMoveElement = teamElement.Element("wrappedMovement");
          var scoringElement = teamElement.Element("scoringSystem");

          Teams.Add(new FlatlandTeamInfo()
          {
            TeamIndex = Convert.ToInt32(teamElement.Element("teamIndex").Value),
            ThemeName = teamElement.Element("themeName").Value,
            MoveSeconds = Convert.ToInt32(teamElement.Element("moveSeconds").Value),
            Kind = (kindElement != null) ? kindElement.Value : "Normal",
            WrappedMovement = (wrapMoveElement != null) ? Convert.ToBoolean(wrapMoveElement.Value) : false,
            ScoringSystem = (scoringElement != null) ? scoringElement.Value : "None"
          });
        }
      }

      var fixedTilesElement = element.Element("fixedTiles");
      FixedTiles.Clear();

      if (fixedTilesElement != null)
      {
        foreach (var tileElement in fixedTilesElement.Elements("tile"))
        {
          FixedTiles.Add(new Point(Convert.ToInt32(tileElement.Element("x").Value),
            Convert.ToInt32(tileElement.Element("y").Value))
          );
        }
      }
    }
    
    public void Save(XmlTextWriter writer)
    {
      if (Tiles.HasValue)
      {
        writer.WriteElementString("tiles", Tiles.ToString());
      }
      
      writer.WriteElementString("collisionBehavior", CollisionBehavior);
      writer.WriteElementString("gameType", GameType);

      if (Teams.Count > 0)
      {
        writer.WriteStartElement("teams");
        
        foreach (var team in Teams)
        {
          writer.WriteStartElement("team");
          writer.WriteElementString("teamIndex", team.TeamIndex.ToString());
          writer.WriteElementString("themeName", team.ThemeName);
          writer.WriteElementString("moveSeconds", team.MoveSeconds.ToString());
          writer.WriteElementString("kind", team.Kind);
          writer.WriteElementString("wrappedMovement", team.WrappedMovement.ToString());
          writer.WriteElementString("scoringSystem", team.ScoringSystem.ToString());
          writer.WriteEndElement();
        }

        // teams
        writer.WriteEndElement();

        if (FixedTiles.Count > 0)
        {
          writer.WriteStartElement("fixedTiles");
          
          foreach (var point in FixedTiles)
          {
            writer.WriteStartElement("tile");
            writer.WriteElementString("x", point.X.ToString());
            writer.WriteElementString("y", point.Y.ToString());
            writer.WriteEndElement();
          }
        
          // fixedTiles
          writer.WriteEndElement();
        }
      }
    }
    
    #endregion
  }

  public class FlatlandTeamInfo
  {
    public int TeamIndex { get; set; }
    public string ThemeName { get; set; }
    public int MoveSeconds { get; set; }
    public string Kind { get; set; }
    public bool WrappedMovement { get; set; }
    public string ScoringSystem { get; set; }
  }
}

