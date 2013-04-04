using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

using OpenTK.Graphics;
using NPlot;
using Nexus.Shared;

namespace GroupSum
{
	public class GroupSumPlotSurface
	{
		#region Fields
		
		protected PlotSurface2D plotSurface = new PlotSurface2D();

        protected LinePlot linePlot = new LinePlot();
        protected GroupSumPointPlot pointPlot = new GroupSumPointPlot();
        protected LinearAxis plotYAxis = new LinearAxis();
        protected LinearAxis plotXAxis = new LinearAxis();
        protected Marker lowMarker = new Marker();
        protected Marker highMarker = new Marker();
        protected Marker correctMarker = new Marker();

        protected Bitmap plotBitmap = null;
        
		#endregion

		public int TextureId { get; protected set; }
		public int TargetNumber { get; set; }
        
		public GroupSumPlotSurface(int targetNumber)
		{
			TargetNumber = targetNumber;
			
			// Set up plot surface
			linePlot.Pen = new Pen(Color.FromArgb(0xCC, 0xCC, 0xFF), 3);

            lowMarker.Size = 10;
            lowMarker.Type = Marker.MarkerType.FilledCircle;
            lowMarker.Pen = new Pen(Color.FromArgb(0x00, 0x00, 0x66), 3);
            lowMarker.FillBrush = new SolidBrush(Color.FromArgb(0x66, 0x66, 0xCC));

            highMarker.Size = 10;
            highMarker.Type = Marker.MarkerType.FilledCircle;
            highMarker.Pen = new Pen(Color.FromArgb(0x66, 0x00, 0x00), 3);
            highMarker.FillBrush = new SolidBrush(Color.FromArgb(0xCC, 0x66, 0x66));

            correctMarker.Size = 10;
            correctMarker.Type = Marker.MarkerType.FilledCircle;
            correctMarker.Pen = new Pen(Color.FromArgb(0x00, 0x66, 0x00), 3);
            correctMarker.FillBrush = new SolidBrush(Color.FromArgb(0x66, 0xCC, 0x66));

            pointPlot.Marker = correctMarker;
            pointPlot.MarkerCallback = new MarkerCallback(pointPlot_MarkerCallback);
            pointPlot.LabelOffset = 15.0f;
            pointPlot.LabelPadding = 2.0f;
            pointPlot.LabelFont = new Font("Arial", 11.0f, FontStyle.Bold);

            plotSurface.Add(new Grid()
            {
                VerticalGridType = Grid.GridType.Coarse,
                HorizontalGridType = Grid.GridType.Coarse
            });

            plotYAxis.SmallTickSize = 1;
            plotYAxis.WorldMin = 0;
            plotYAxis.WorldMax = 100;
            plotYAxis.TickTextFont = new Font("Arial", 12);

            plotXAxis.LargeTickStep = 1;
            plotXAxis.SmallTickSize = 1;
            plotXAxis.WorldMin = 1;
            plotXAxis.WorldMax = 10;
            plotXAxis.TickTextFont = new Font("Arial", 12);
            plotXAxis.TickTextNextToAxis = false;

            plotSurface.SmoothingMode = SmoothingMode.HighQuality;
            plotSurface.YAxis1 = plotYAxis;
            plotSurface.XAxis1 = plotXAxis;
            plotSurface.Add(linePlot);
            plotSurface.Add(pointPlot);

			linePlot.AbscissaData = null;
            linePlot.OrdinateData = null;
            pointPlot.AbscissaData = null;
            pointPlot.OrdinateData = null;
		}

		public void Resize(int width, int height)
		{
			plotBitmap = new Bitmap(width, height);

			if (TextureId >= 0)
			{
				GL.DeleteTexture(TextureId);
			}

			TextureId = Textures.Load(plotBitmap);
		}

		public void RedrawPlotSurface()
		{
            using (var graphics = Graphics.FromImage(plotBitmap))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.Clear(Color.White);
                
                plotSurface.Draw(graphics, new Rectangle(0, 0,
                    plotBitmap.Width, plotBitmap.Height));
            }

			// Update OpenGL texture
			Textures.Update(TextureId, plotBitmap);
		}

		public void DrawWithOpenGL(RectangleF bounds)
		{
			GL.Enable(EnableCap.Texture2D);
			GL.Color3(Color.White);
			GL.BindTexture(TextureTarget.Texture2D, TextureId);
			GL.Begin(BeginMode.Quads);

			GL.TexCoord2(0, 0);
			GL.Vertex2(bounds.Left, bounds.Top);

			GL.TexCoord2(0, 1);
			GL.Vertex2(bounds.Left, bounds.Bottom);

			GL.TexCoord2(1, 1);
			GL.Vertex2(bounds.Right, bounds.Bottom);

			GL.TexCoord2(1, 0);
			GL.Vertex2(bounds.Right, bounds.Top);
			
			GL.End();
			GL.Disable(EnableCap.Texture2D);
		}

		public void SetPlotAxes(double xMin, double xMax, double yMin, double yMax)
		{
			plotXAxis.WorldMin = xMin;
            plotXAxis.WorldMax = xMax;
            
            plotYAxis.WorldMin = yMin;
            plotYAxis.WorldMax = yMax;
		}

		public void UpdatePlotData(IEnumerable<double> sumData)
		{
			pointPlot.OrdinateData = linePlot.OrdinateData = sumData;
			pointPlot.AbscissaData = linePlot.AbscissaData = Enumerable.Range(1, sumData.Count()).ToArray();
		}

		#region Marker Callback

		private Marker pointPlot_MarkerCallback(double ordinate, double abscissa)
        {
        	var groupSum = (int)abscissa;

			if (groupSum < TargetNumber)
			{
				return (lowMarker);
			}
			else if (groupSum > TargetNumber)
			{
				return (highMarker);
			}

			return (correctMarker);
        }

		#endregion
	}
}
