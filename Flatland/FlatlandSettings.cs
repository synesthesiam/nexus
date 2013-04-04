using System;
using System.Collections.Generic;

using Nexus.Shared;

namespace Flatland
{
  public class FlatlandSettings : DefaultGameSettings
  {
    public const int FileVersion = 1;

    public int Tiles { get; set; }
    public int Players { get; set; }
    public List<FlatlandTeamInfo> Teams { get; protected set; }
    
    public PlayerSort PlayerSort { get; set; }
    public CollisionBehavior CollisionBehavior { get; set; }
    public HashSet<FixedTile> FixedTiles { get; set; }
    
    public bool ShowTeamNames { get; set; }
    public bool WrapWorld { get; set; }

    public FlatlandGameType GameType { get; set; }

    public FlatlandSettings()
    {
      Teams = new List<FlatlandTeamInfo>();
      FixedTiles = new HashSet<FixedTile>();
    }
  }
  
  public enum PlayerSort
  {
    Random, SpreadOut, Clustered
  }
  
  public enum CollisionBehavior
  {
    Block, Allow
  }

  public class FlatlandTeamInfo
  {
    public int TeamIndex { get; set; }
    public string TeamName { get; set; }

    public string ThemeName { get; set; }
    public Theme Theme { get; set; }

    public int MoveSeconds { get; set; }
    public FlatlandTeamKind Kind { get; set; }

    public double SecondsLeftForMove { get; set; }
    public double SecondsLeftForMoving { get; set; }
    public bool IsMoving
    {
      get { return (SecondsLeftForMoving > 0); }
    }

    public bool WrappedMovement { get; set; }
    public FlatlandScoringSystem ScoringSystem { get; set; }
    public int Score { get; set; }

    public FlatlandTeamInfo()
    {
      Kind = FlatlandTeamKind.Normal;
      WrappedMovement = false;
      ScoringSystem = FlatlandScoringSystem.None;
      Score = 0;
    }
  }

  public enum FlatlandTeamKind
  {
    Normal, Predator, Prey
  }

  public enum FlatlandGameType
  {
    Normal, NQueens, OnePrey
  }

  public enum FlatlandScoringSystem
  {
    None, Selfish, Communal
  }

  public class FixedTile
  {
    public int X { get; set; }
    public int Y { get; set; }

    public FixedTile()
    {
    }

    public FixedTile(int x, int y)
    {
      this.X = x;
      this.Y = y;
    }

    public override bool Equals(object obj)
    {
      if ((obj == null) || (obj.GetType() != typeof(FixedTile)))
      {
        return (false);
      }

      var otherTile = (FixedTile)obj;

      return (this.X.Equals(otherTile.X) && this.Y.Equals(otherTile.Y));
    }

    public override int GetHashCode()
    {
      return (this.X ^ this.Y);
    }
  }
}
