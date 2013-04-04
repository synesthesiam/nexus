using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

using Nexus.Shared;

namespace Pixel
{
	public class PixelWindow : GameWindow
	{
		#region Fields
		
		protected log4net.ILog logger = log4net.LogManager.GetLogger("Pixel.PixelWindow");
		protected readonly string basePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);		
		protected Random rand = new Random();
		protected DoubleAnimator animator = new DoubleAnimator();
		
		protected int pixelCount = 0, tiles = 0;
		protected double pixelSize = 0, textureOffset = 0, buttonSize = 0;
		protected double boardLeft = 0, boardTop = 0, boardSize = 0;
		
		protected List<PlayerPixel> pixels = new List<PlayerPixel>();
		protected int[] playerTextureIds = null;
		protected Theme buttonTheme = Themes.Blue;
		
		protected Font pixelFont = null, descriptionFont = null;
		protected TextPrinter printer = new TextPrinter(TextQuality.Medium);
		protected PointF descriptionPoint = PointF.Empty;
		
		protected TcpClient inputClient = new TcpClient();
		protected byte[] inputBuffer = new byte[2];
		protected System.Diagnostics.Stopwatch inputTimer = new System.Diagnostics.Stopwatch();
		
		protected YAMLOutputStream dataWriter = null;
		
		#endregion
		
		#region Properties
		
		public PixelSettings Settings { get; protected set; }
		
		#endregion
		
		#region Constructor
		
		public PixelWindow(PixelSettings settings) : base(settings.ScreenWidth,
			settings.ScreenHeight, GraphicsMode.Default, "Pixel",
			settings.Fullscreen ? GameWindowFlags.Fullscreen : 0)
		{
			logger.Info("Initializing Pixel game window");
			this.Settings = settings;
			
			Keyboard.KeyDown += Keyboard_KeyDown;
			
			// Open data file
			if (!string.IsNullOrEmpty(Settings.DataFilePath))
			{
				logger.DebugFormat("Opening data file at {0}", Settings.DataFilePath);
				dataWriter = new YAMLOutputStream(Settings.DataFilePath);
			}
			
			// Calculate number of pixels
			var possiblePixels = settings.Players * PixelSettings.PlayerButtonCount;
			var maxPixels = Settings.MaxSize * Settings.MaxSize;
			
			if ((maxPixels > 0) && (possiblePixels > maxPixels))
			{
				tiles = Settings.MaxSize;
			}
			else
			{
				tiles = (int)Math.Floor(Math.Sqrt(possiblePixels));
			}
			
			pixelCount = tiles * tiles;
			
			// Calculate pixel and board sizes
			CalculateSizesAndLoadFonts();
									
			// Load textures
			var textureBasePath = Path.Combine(basePath, Path.Combine("etc", "player_images"));
			logger.DebugFormat("Loading textures from {0}", textureBasePath);
			
			playerTextureIds = Textures.LoadPlayers(textureBasePath, Settings.Players).ToArray();
			
			// Create pixels
			pixels.AddRange(Enumerable.Range(0, pixelCount)
				.Select(i => new PlayerPixel(i % tiles, i / tiles,
					GetInitialPixelState()))
			);
			
			SetupPlayerSort();

			// Output settings and player pixels
			if (dataWriter != null)
			{
				dataWriter.WriteStartDocument();
				dataWriter.WriteHashSingle("Version", PixelSettings.FileVersion);

				dataWriter.WriteLine();
				dataWriter.WriteStartList("Settings");
        dataWriter.WriteHashSingle("Description", Settings.GameDescription);
				dataWriter.WriteHashSingle("Players", Settings.Players);
				dataWriter.WriteHashSingle("Size", string.Format("{0}x{0}", tiles));
				dataWriter.WriteHashSingle("Initial State", Settings.InitialState);
				dataWriter.WriteHashSingle("Player Sort", Settings.PlayerSort);				
				dataWriter.WriteEndList();

				dataWriter.WriteLine();
				dataWriter.WriteStartList("Player Pixels");
				dataWriter.WriteComment("x, y, state, player");

				foreach (var pixel in pixels)
				{
					if (pixel.State == PixelState.Fixed)
					{
						dataWriter.WriteText(string.Format("{0}, {1}, Fixed", pixel.TileLeft, pixel.TileTop));
					}
					else
					{
						dataWriter.WriteText(string.Format("{0}, {1}, {2}, {3}",
							pixel.TileLeft, pixel.TileTop, pixel.State, pixel.PlayerId));
					}
				}

				dataWriter.WriteEndList();

				dataWriter.WriteLine();
				dataWriter.WriteStartList("Moves");
				dataWriter.WriteComment("time in milliseconds, player, x, y, On/Off");
			}
			
			// Connect to input server
			if (Settings.Port > 0)
			{
				try
				{
					inputClient.Connect(IPAddress.Loopback, Settings.Port);
					inputClient.Client.BeginReceive(inputBuffer, 0, 2, SocketFlags.None,
						inputClient_DataReceived, null);
				}
				catch (Exception ex)
				{
					logger.Error("Failed to connect to input server", ex);
				}
			}
			
			inputTimer.Start();
		}
		
		#endregion
		
		#region Window Event Handlers
		
		protected override void OnLoad (System.EventArgs e)
		{
			GL.ClearColor(Color.DimGray);
			GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.LineSmooth);
		}
		
		protected override void OnUnload (System.EventArgs e)
		{
			logger.Debug("Shutting down Pixel");
			
			if (dataWriter != null)
			{
				dataWriter.WriteEndList();
				dataWriter.WriteEndDocument();
				
				// Close output file
				dataWriter.Close();
			}
			
			logger.Debug("Freeing textures");			
			foreach (var textureId in playerTextureIds)
			{
				GL.DeleteTexture(textureId);
			}
		}
		
		protected override void OnResize (EventArgs e)
		{
			// Setup 2-D viewing mode with a viewport the size of the window
			GL.Viewport(0, 0, ClientRectangle.Width, ClientRectangle.Height);
      GL.MatrixMode(MatrixMode.Projection);
      GL.LoadIdentity();
      GL.Ortho(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);

      GL.MatrixMode(MatrixMode.Modelview);
      GL.LoadIdentity();

			// Cancel any pending animations and recalculate sizes
			animator.FinishAndClear();
			CalculateSizesAndLoadFonts();
		}
		
		protected override void OnUpdateFrame (OpenTK.FrameEventArgs e)
		{
			if (Keyboard[Key.Escape] || Keyboard[Key.Q])
			{
				Exit();
			}
		}
		
		protected override void OnRenderFrame (OpenTK.FrameEventArgs e)
		{
			lock (animator)
			{
				animator.Update(e.Time);
			}
			
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.LoadIdentity();

			// Draw game description text
			if (!string.IsNullOrEmpty(Settings.GameDescription))
			{
				// Draw box background
				GL.Color3(Themes.Black.Background);
				GL.Begin(BeginMode.Quads);
				GL.Vertex2(0, 0);
				GL.Vertex2(0, boardTop + 1);
				GL.Vertex2(this.Width, boardTop + 1);
				GL.Vertex2(this.Width, 0);
				GL.End();

				// Draw description text
				GL.PushMatrix();
				GL.Translate(descriptionPoint.X, descriptionPoint.Y, 0);
				printer.Print(Settings.GameDescription, descriptionFont, Themes.Black.Foreground);
				GL.PopMatrix();
			}
			
			// Draw entire board
			for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
			{
				var currentPixel = pixels[pixelIndex];
				double pixelLeft = boardLeft + (currentPixel.TileLeft * pixelSize);
				double pixelTop = boardTop + (currentPixel.TileTop * pixelSize);
		
				// Draw pixel background
				GL.Color3(currentPixel.Red, currentPixel.Green, currentPixel.Blue);
				
				GL.Disable(EnableCap.Texture2D);
				GL.Begin(BeginMode.Quads);
				GL.Vertex2(pixelLeft, pixelTop);
				GL.Vertex2(pixelLeft, pixelTop + pixelSize);
				GL.Vertex2(pixelLeft + pixelSize, pixelTop + pixelSize);
				GL.Vertex2(pixelLeft + pixelSize, pixelTop);
				GL.End();
				
				// Don't draw anything else for fixed pixels
				if (currentPixel.State == PixelState.Fixed)
				{
					continue;
				}
				
				// Draw player				
				GL.Color3(Color.White);
				GL.Enable(EnableCap.Texture2D);
				GL.BindTexture(TextureTarget.Texture2D, playerTextureIds[currentPixel.PlayerId]);
				GL.Begin(BeginMode.Quads);
				
				GL.TexCoord2(0, 0);
				GL.Vertex2(pixelLeft + textureOffset, pixelTop + textureOffset);
				
				GL.TexCoord2(0, 1);
				GL.Vertex2(pixelLeft + textureOffset, pixelTop + pixelSize - textureOffset);
				
				GL.TexCoord2(1, 1);
				GL.Vertex2(pixelLeft + pixelSize - textureOffset,
					pixelTop + pixelSize - textureOffset);
				
				GL.TexCoord2(1, 0);
				GL.Vertex2(pixelLeft + pixelSize - textureOffset, pixelTop + textureOffset);
				
				GL.End();
				
				// Draw button box
				double buttonLeft = pixelLeft + pixelSize - buttonSize;
				double buttonTop = pixelTop + pixelSize - buttonSize;
				
				GL.Disable(EnableCap.Texture2D);
				GL.Color3(buttonTheme.Background);
				
				GL.Begin(BeginMode.Quads);
				GL.Vertex2(buttonLeft, buttonTop);
				GL.Vertex2(buttonLeft, buttonTop + buttonSize);
				GL.Vertex2(buttonLeft + buttonSize, buttonTop + buttonSize);
				GL.Vertex2(buttonLeft + buttonSize, buttonTop);
										
				GL.End();
				
				// Draw box outline
				GL.Color3(buttonTheme.Border);
				GL.LineWidth(1);
				
				GL.Begin(BeginMode.LineLoop);
				GL.Vertex2(buttonLeft, buttonTop);
				GL.Vertex2(buttonLeft, buttonTop + buttonSize);
				GL.Vertex2(buttonLeft + buttonSize, buttonTop + buttonSize);
				GL.Vertex2(buttonLeft + buttonSize, buttonTop);
										
				GL.End();
				
				// Draw button number
				var buttonString = currentPixel.GetButtonString();
				
				if (!string.IsNullOrEmpty(buttonString))
				{
					var buttonTextSize = printer.Measure(buttonString, pixelFont)[0];
						
					GL.PushMatrix();			
					GL.Translate(buttonLeft + ((buttonSize - buttonTextSize.Width) / 2.0f) - 1.0f,
						buttonTop + ((buttonSize - buttonTextSize.Height) / 2.0f) - 1.0f, 0);
						
					printer.Print(buttonString, pixelFont, buttonTheme.Foreground);
					GL.PopMatrix();
				}
			}
			
			// Draw board lines
			GL.Disable(EnableCap.Texture2D);
			GL.Color3(Color.DarkBlue);
			GL.LineWidth(2);
			
			for (int tileIndex = 0; tileIndex <= tiles; tileIndex++)
			{
				GL.Begin(BeginMode.Lines);
				
				// Vertical line
				GL.Vertex2(boardLeft + (tileIndex * pixelSize), boardTop);
				GL.Vertex2(boardLeft + (tileIndex * pixelSize), boardTop + boardSize);
				
				// Horizontal line
				GL.Vertex2(boardLeft, boardTop + (tileIndex * pixelSize));
				GL.Vertex2(boardLeft + boardSize, boardTop  + (tileIndex * pixelSize));
				
				GL.End();
			}
			
			SwapBuffers();
		}
		
		#endregion

		#region Keyboard Event Handlers

		protected void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
				case Key.Q:
					Exit();
					break;

				case Key.F:
					this.WindowState = (this.WindowState == WindowState.Fullscreen) ?
						this.WindowState = WindowState.Normal : this.WindowState = WindowState.Fullscreen;
					break;

				case Key.R:
					if (Settings.IsDebugMode)
					{
						for (int playerId = 0; playerId < Settings.Players; playerId++)
						{
							ProcessPlayerButton(playerId, rand.Next(PixelSettings.PlayerButtonCount + 1));
						}
					}
					break;
			}
		}

		#endregion
		
		#region Network Methods
		
		protected void inputClient_DataReceived(IAsyncResult result)
		{
			try			
			{
				var bytesReceived = inputClient.Client.EndReceive(result);
				
				if (bytesReceived <= 0)
				{
					return;
				}
				
				int playerId = inputBuffer[0], button = inputBuffer[1];
				ProcessPlayerButton(playerId, button);
				
				inputClient.Client.BeginReceive(inputBuffer, 0, 2,
					SocketFlags.None, inputClient_DataReceived, null);
			}
			catch (SocketException ex)
			{
				logger.Error("Network error", ex);
			}
			catch (Exception ex)
			{
				logger.Error("inputClient_DataReceived", ex);
			}
		}
		
		#endregion
		
		#region Initial State and Player Sort Methods
		
		protected PixelState GetInitialPixelState()
		{
			if (Settings.InitialState == InitialState.On)
			{
				return (PixelState.On);
			}
			else if (Settings.InitialState == InitialState.Off)
			{
				return (PixelState.Off);
			}
			
			return ((rand.NextDouble() < 0.5) ? PixelState.On : PixelState.Off);
		}
		
		protected void SetupPlayerSort()
		{
			var playerIds = Enumerable.Range(0, Settings.Players);
			var shuffledPlayerIds = playerIds.OrderBy(x => rand.Next()).ToList();

			if ((Settings.FixedPixels.Count > 0) && (Settings.PlayerSort != PlayerSort.Random))
			{
				logger.Warn("Fixed pixels are only used when player sort is random");
			}
			
			if (Settings.PlayerSort == PlayerSort.Random)
			{
				int pixelIndex = 0, playerIndex = 0, currentButton = 1;

				// Go through the list of pixels from left to right, top to bottom
				// and assign players in semi-random order (grouped).
				while (pixelIndex < pixels.Count)
				{
					var currentPixel = pixels[pixelIndex];
					pixelIndex++;

					// Skip fixed pixels
					if (Settings.FixedPixels.Any(p => (p.X == currentPixel.TileLeft) &&
						(p.Y == currentPixel.TileTop)))
					{
						currentPixel.Red = 0;
						currentPixel.Green = 0;
						currentPixel.Blue = 0.8;
						currentPixel.State = PixelState.Fixed;
						continue;
					}
					
					currentPixel.PlayerId = shuffledPlayerIds[playerIndex];
					currentPixel.Button = currentButton;
					
					if (++playerIndex >= shuffledPlayerIds.Count)
					{
						// Reshuffle player order and continue to next button
						playerIndex = 0;
						shuffledPlayerIds = playerIds.OrderBy(x => rand.Next()).ToList();
						currentButton++;
					}
				}
			}
			else if (Settings.PlayerSort == PlayerSort.Diffuse)
			{
				// Make a kinda-latin square
				int startingPlayerId = 0;
				
				for (int tileTop = 0; tileTop < tiles; tileTop++)
				{
					var playerId = startingPlayerId;
					
					// Repeat this row's ordering
					for (int tileLeft = 0; tileLeft < tiles; tileLeft++)
					{
						var pixelIndex = (tileTop * tiles) + tileLeft;
						var currentPixel = pixels[pixelIndex];
						
						currentPixel.PlayerId = playerId;
						playerId = (playerId + 1) % Settings.Players;
					}
					
					// Shift the starting player id forward by two for each row
					startingPlayerId = (startingPlayerId + 2) % Settings.Players;
				}
				
				// Assign buttons from left to right, top to bottom
				var playerButtons = Enumerable.Range(0, Settings.Players)
					.Select(i => 1)
					.ToList();
					
				foreach (var pixel in pixels)
				{
					pixel.Button = playerButtons[pixel.PlayerId]++;
				}
			}
			else if (Settings.PlayerSort == PlayerSort.Clustered)
			{
				var board = ClusteredBoard.Create(Settings.Players, tiles);

				// Assign buttons from left to right, top to bottom
				var playerButtons = Enumerable.Range(0, Settings.Players)
					.Select(i => 1)
					.ToList();
					
				foreach (var pixel in pixels)
				{
					pixel.PlayerId = (int)board[pixel.TileLeft, pixel.TileTop];
					pixel.Button = playerButtons[pixel.PlayerId]++;
				}
			}
		}

		#endregion

		#region Utility Methods

		protected void CalculateSizesAndLoadFonts()
		{
			double windowWidth = this.Width, windowHeight = this.Height;

			if (!string.IsNullOrEmpty(Settings.GameDescription))
			{
				// Reserve part of the top screen space for the description text
				windowHeight *= 0.93;
			}

			double descriptionHeight = (this.Height - windowHeight);

			// Calculate grid and pixel sizes based on the window size
			pixelSize = (double)Math.Min(windowWidth, windowHeight) / (double)tiles;
            textureOffset = (pixelSize - (pixelSize * 0.85f)) / 2.0f;
            buttonSize = pixelSize * 0.35f;
			boardSize = pixelSize * tiles;
			boardLeft = ((double)windowWidth - boardSize) / 2.0f;
			boardTop = descriptionHeight + ((double)windowHeight - boardSize) / 2.0f;
			
			// Load font
			pixelFont = new Font(FontFamily.GenericMonospace, (float)pixelSize * 0.3f, FontStyle.Bold);
			descriptionFont = new Font(FontFamily.GenericSansSerif, (float)descriptionHeight * 0.45f, FontStyle.Bold);

			var descriptionExtents = printer.Measure(Settings.GameDescription, descriptionFont);
			descriptionPoint.X = ((float)windowWidth - descriptionExtents.BoundingBox.Width) / 2.0f;
			descriptionPoint.Y = ((float)descriptionHeight - descriptionExtents.BoundingBox.Height) / 2.0f;
		}

		protected void ProcessPlayerButton(int playerId, int button)
		{
			var timeStamp = inputTimer.ElapsedMilliseconds;
			
			// Find the first pixel with a matching player id and button
			var matchingPixel = pixels.FirstOrDefault(p =>
				(p.PlayerId == playerId) && (p.Button == button));
				
			if ((matchingPixel != null) && (matchingPixel.State != PixelState.Fixed))
			{				
				// Flip the pixel	
				matchingPixel.State = (matchingPixel.State == PixelState.On) ?
					PixelState.Off : PixelState.On;
				
				if (dataWriter != null)
				{
					// Record input
					dataWriter.WriteText(string.Format("{0} {1}, {2}, {3}, {4}",
						timeStamp, playerId, matchingPixel.TileLeft,
						matchingPixel.TileTop, matchingPixel.State));
				}
				
				lock (animator)
				{
					animator.PropertyTo(matchingPixel, "AllColors",
						(matchingPixel.State == PixelState.On) ? 1 : 0, 0.3f);
				}
			}
		}

		#endregion		
	}
}
