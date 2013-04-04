using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Nexus.Shared
{
	public static class Textures
	{
		public static IEnumerable<int> Load(string basePath, string prefix,
			int textureCount, Func<Bitmap, Bitmap> imageFilter)
		{
			var textureIds = new List<int>();
			
			for (int textureIndex = 0; textureIndex < textureCount; textureIndex++)
			{
				var bitmap = new Bitmap(Path.Combine(basePath,
					string.Format("{0}{1}.png", prefix, textureIndex)));
					
				textureIds.Add(Load(imageFilter(bitmap)));
			}
			
			return (textureIds);
		}

    public static int LoadSingle(string basePath, string name)
    {
      return (Load(new Bitmap(Path.Combine(basePath, string.Format("{0}.png", name)))));
    }

		public static IEnumerable<int> LoadPlayers(string basePath, int players,
			Func<Bitmap, Bitmap> imageFilter)
		{
			return (Load(basePath, "player", players, imageFilter));
		}

		public static IEnumerable<int> LoadPlayers(string basePath, int players)
		{
			return (LoadPlayers(basePath, players, (x) => x));
		}

		public static int Load(Bitmap bitmap)
		{
			GL.Enable(EnableCap.Texture2D);
			int textureId = GL.GenTexture();
			
      GL.BindTexture(TextureTarget.Texture2D, textureId);

      // Use linear filtering
      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                      (int)TextureMinFilter.Linear);

      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                      (int)TextureMagFilter.Linear);

      // Don't repeat texture
      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                      (int)TextureWrapMode.Clamp);

      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                      (int)TextureWrapMode.Clamp);
        	
      // Load raw bitmap data into OpenGL
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0,
				bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				bitmapData.Width, bitmapData.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte,
				bitmapData.Scan0);
				
			bitmap.UnlockBits(bitmapData);

			return (textureId);
		}

		public static void Update(int textureId, Bitmap bitmap)
		{
			GL.Enable(EnableCap.Texture2D);		
        	GL.BindTexture(TextureTarget.Texture2D, textureId);
            	
      // Update raw bitmap data in OpenGL
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0,
				bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				
			GL.TexSubImage2D(TextureTarget.Texture2D, 0,
				0, 0, bitmapData.Width, bitmapData.Height,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte,
				bitmapData.Scan0);
				
			bitmap.UnlockBits(bitmapData);
		}

		public static Bitmap MakeGrayScale(Bitmap bitmap)
		{
			var grayScaleMatrix = new ColorMatrix(new float[][]
			{
				new float[] { .3f, .3f, .3f, 0, 0 },
				new float[] { .59f, .59f, .59f, 0, 0 },
				new float[] { .11f, .11f, .11f, 0, 0 },
				new float[] { 0, 0, 0, 1, 0 },
				new float[] { 0, 0, 0, 0, 1 }
		    });

			var attributes = new ImageAttributes();
			attributes.SetColorMatrix(grayScaleMatrix);

			var newBitmap = new Bitmap(bitmap.Width, bitmap.Height, bitmap.PixelFormat);
		      
			using (var graphics = Graphics.FromImage(newBitmap))
			{
				graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
			}

			return (newBitmap);
		}
	}
}
