using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Nexus.Shared
{
  public class TextPrinter
  {
    private IDictionary<string, TextBuffer> buffers = new Dictionary<string, TextBuffer>();
    private Brush brush;
    private Font font;


#region Constructors
    public TextPrinter(Brush brush, Font font)
    {
      this.brush = brush;
      this.font = font;
    }

#endregion

#region Public Members
    public void Free()
    {
      foreach (var buffer in buffers.Values)
      {
        buffer.Free();
      }
    }

    public SizeF Measure(string str)
    {
      if (!buffers.ContainsKey(str))
      {
        buffers[str] = new TextBuffer(str, brush, font);
      }

      var buffer = buffers[str];
      return new SizeF(buffer.ActualWidth, buffer.ActualHeight);
    }

    public void Render(string str)
    {
      Render(str, Color.White);
    }

    public void Render(string str, Color color)
    {
      if (!buffers.ContainsKey(str))
      {
        buffers[str] = new TextBuffer(str, brush, font);
      }

      var buffer = buffers[str];

      GL.Color3(color);
      GL.Enable(EnableCap.Texture2D);
      GL.BindTexture(TextureTarget.Texture2D, buffer.TextureId);
      GL.Begin(BeginMode.Quads);

      GL.TexCoord2(0, 0);
      GL.Vertex2(0, 0);

      GL.TexCoord2(0, buffer.HeightScale);
      GL.Vertex2(0, buffer.ActualHeight);

      GL.TexCoord2(buffer.WidthScale, buffer.HeightScale);
      GL.Vertex2(buffer.ActualWidth, buffer.ActualHeight);

      GL.TexCoord2(buffer.WidthScale, 0);
      GL.Vertex2(buffer.ActualWidth, 0);

      GL.End();
      GL.Disable(EnableCap.Texture2D);
    }

  } // class TextPrinter

  public class TextBuffer
  {
    public int TextureId { get; set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int ActualWidth { get; private set; }
    public int ActualHeight { get; private set; }
    public double WidthScale { get; private set; }
    public double HeightScale { get; private set; }

    public TextBuffer(string str, Brush brush, Font font)
    {
      // Bootstrap
      var bmp = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      var gfx = Graphics.FromImage(bmp);
      gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

      var strSize = gfx.MeasureString(str, font);      
      ActualWidth = (int)Math.Ceiling(strSize.Width);
      ActualHeight = (int)Math.Ceiling(strSize.Height);

      Width = (int)Math.Pow(2, Math.Ceiling(Math.Log(ActualWidth, 2)));
      Height = (int)Math.Pow(2, Math.Ceiling(Math.Log(ActualHeight, 2)));

      WidthScale = (double)ActualWidth / (double)Width;
      HeightScale = (double)ActualHeight / (double)Height;

      bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      gfx = Graphics.FromImage(bmp);
      gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

      TextureId = GL.GenTexture();
      GL.BindTexture(TextureTarget.Texture2D, TextureId);
      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
      GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

      gfx.DrawString(str, font, brush, new PointF(0, 0));

      // Create OpenGL texture
      System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, Width, Height),
                                                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

      GL.BindTexture(TextureTarget.Texture2D, TextureId);
      GL.TexSubImage2D(TextureTarget.Texture2D, 0,
                       0, 0, Width, Height,
                       PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

      bmp.UnlockBits(data);
    }

    public void Free()
    {
      GL.DeleteTexture(TextureId);
    }
  }

#endregion

} // namespace Nexus.Shared

