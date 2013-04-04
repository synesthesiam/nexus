using System;

namespace Nexus
{  
  public partial class DialogEditFixedTiles : Gtk.Dialog
  {    
    public DrawingAreaGrid Grid { get; protected set; }
    
    public DialogEditFixedTiles()
    {
      this.Build();
    }

    public DialogEditFixedTiles(int tiles) : this()
    {
      Grid = new DrawingAreaGrid() { Tiles = tiles };
      VBox.PackEnd(Grid, true, true, 6);
      Grid.Show();
    }
  }
}
