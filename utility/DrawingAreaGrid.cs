using System;
using System.Drawing;
using System.Collections.Generic;

using Gtk;

namespace Nexus
{

	public class DrawingAreaGrid : DrawingArea
	{
		protected int tiles = 1;
		protected List<Point> enabledPixels = new List<Point>();
		
		public int Tiles
		{
			get { return(tiles); }
			set
			{
				tiles = value;
				enabledPixels.RemoveAll(p => (p.X >= tiles) || (p.Y >= tiles));
			}
		}
		
		public IEnumerable<Point> EnabledPixels
		{
			get { return (enabledPixels); }
		}

    public bool CenterGridHorizontally { get; set; }

    public DrawingAreaGrid()
    {
      this.Events = Gdk.EventMask.ButtonPressMask;
      CenterGridHorizontally = true;
    }

    public void ClearPixels()
    {
      enabledPixels.Clear();
    }

    public void SetPixels(IEnumerable<Point> newPixels)
    {
      enabledPixels.AddRange(newPixels);
    }
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create(evnt.Window))
			{
				// Transparent background for control
				context.SetSourceRGBA(0, 0, 0, 0);
				context.Paint();

				// Draw grid
				var area = this.Allocation;
				var gridSize = (double)Math.Min(area.Width, area.Height);
				
        double gridLeft = CenterGridHorizontally ?
          ((double)area.Width - gridSize) / 2.0 : 0;

        double gridTop = ((double)area.Height - gridSize) / 2.0;
				double lineSpacing = gridSize / (double)tiles;

				// White background for grid
				context.SetSourceRGB(1, 1, 1);
				context.Rectangle(gridLeft, gridTop, gridSize, gridSize);
				context.Fill();

				// Draw fixed pixels in blue
				context.SetSourceRGB(0, 0, 0.8);
				foreach (var point in enabledPixels)
				{
					if ((point.X < 0) || (point.X >= tiles) ||
						(point.Y < 0) || (point.Y >= tiles))
					{
						continue;
					}
					
					context.Rectangle(gridLeft + (point.X * lineSpacing),
                            gridTop + (point.Y * lineSpacing),
                            lineSpacing, lineSpacing);

					context.Fill();
				}

				// Draw lines in black
				context.LineWidth = 1.0;
				context.SetSourceRGB(0, 0, 0);
				
				for (int lineIndex = 0; lineIndex <= tiles; lineIndex++)
				{
					context.MoveTo(gridLeft + (lineIndex * lineSpacing), gridTop);
					context.LineTo(gridLeft + (lineIndex * lineSpacing), gridTop + gridSize);
					context.Stroke();

					context.MoveTo(gridLeft, gridTop + (lineIndex * lineSpacing));
					context.LineTo(gridLeft + gridSize, gridTop + (lineIndex * lineSpacing));
					context.Stroke();
				}
			}

			return (true);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			// Calculate grid area
			var area = this.Allocation;
			var gridSize = (double)Math.Min(area.Width, area.Height);

      double gridLeft = CenterGridHorizontally ?
        ((double)area.Width - gridSize) / 2.0 : 0;

      double gridTop = ((double)area.Height - gridSize) / 2.0;
			double lineSpacing = gridSize / (double)tiles;

			// Calculate the clicked pixel
			double mouseX = evnt.X, mouseY = evnt.Y;

			if ((mouseX < gridLeft) || (mouseX > (gridLeft + gridSize)) ||
				(mouseY < gridTop) || (mouseY > (gridTop + gridSize)))
			{
				return (true);
			}

			var pixelPoint = new Point((int)((mouseX - gridLeft) / lineSpacing),
				(int)((mouseY - gridTop) / lineSpacing));
			
			if (enabledPixels.Contains(pixelPoint))
			{
				enabledPixels.Remove(pixelPoint);
			}
			else
			{
				enabledPixels.Add(pixelPoint);
			}
			
			this.QueueDraw();
			return (true);
		}
	}
}
