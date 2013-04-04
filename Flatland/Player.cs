using System;
using System.Drawing;

using Nexus.Shared;

namespace Flatland
{
  public class Player
  {
    public int Id { get; protected set; }
    public double Left { get; set; }
    public double Top { get; set; }
    
    public int TileLeft { get; set; }
    public int TileTop { get; set; }
    
    public double ArrowAngle { get; set; }
    public double ArrowOpacity { get; set; }
    
    public double BoxHighlightOpacity { get; set; }
    public Color BoxBackColor { get; set; }
    public Color BoxBorderColor { get; set; }
    public Color ArrowColor { get; set; }
    
    public PlayerDirection NextDirection { get; set; }
    public FlatlandTeamInfo Team { get; set; }

    public int Score { get; set; }
    public bool IsPredator { get; set; }
    public bool IsPrey { get; set; }
    public double Opacity { get; set; }
    public double OverlayRed { get; set; }

    public double SpawnTimeout { get; set; }
    public bool IsSpawning { get; set; }
    
    public Player(int id)
    {
      this.Id = id;
      
      Left = 0;
      Top = 0;
      
      TileLeft = -1;
      TileTop = -1;
      
      ArrowAngle = 0;
      ArrowOpacity = 0;

      Opacity  = 1;
      OverlayRed = 0;

      BoxHighlightOpacity = 0;
      
      NextDirection = PlayerDirection.None;
      SetBoxTheme(Themes.Black);
    }
    
    public void SetBoxTheme(Theme theme)
    {
      BoxBackColor = theme.Background;
      BoxBorderColor = theme.Border;
      ArrowColor = theme.Foreground;
    }
  }
  
  public enum PlayerDirection
  {
    None, Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft 
  }
}

