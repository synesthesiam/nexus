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
using OpenTK.Math;

using Nexus.Shared;

namespace GroupSum
{
	public class GroupSumWindow : GameWindow
	{
		#region Constants

		private const int playersPerSectionRow = 12;

		#endregion
	
		#region Fields

		protected log4net.ILog logger = log4net.LogManager.GetLogger("GroupSum.GroupSumWindow");
		protected readonly string basePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);		
		protected Random rand = new Random();
		protected DoubleAnimator animator = new DoubleAnimator();

		protected int[] playerTextureIds = null;
		
		protected GroupSumPlotSurface plotSurface = null;
		protected RectangleF plotBounds = RectangleF.Empty;
		protected double playerSlotSize = 0, playerSectionTop = 0, playerSectionHeight = 0;
		protected double playerSize = 0;

		protected string statusText = "", baseStatusText = "Waiting for guesses";
		protected Font statusFont = null;
		protected PointF statusTextPoint = PointF.Empty;
		protected TextPrinter printer = new TextPrinter(TextQuality.Medium);

		protected Player[] players = null;
		protected IList<double> previousGroupSums = new List<double>();
		protected int maximumPossibleSum = 0;
				
		protected TcpClient inputClient = new TcpClient();
		protected byte[] inputBuffer = new byte[2];

		protected System.Timers.Timer roundTimer = null;
		protected bool isPlotUpdated = false, isStatusTextChanged = false;
		protected int secondsLeftInRound = 0;
		
		protected YAMLOutputStream dataWriter = null;
		
		#endregion
		
		#region Properties
		
		public GroupSumSettings Settings { get; protected set; }
		
		#endregion
		
		#region Constructor
		
		public GroupSumWindow(GroupSumSettings settings) : base(settings.ScreenWidth,
			settings.ScreenHeight, GraphicsMode.Default, "Group Sum",
			settings.Fullscreen ? GameWindowFlags.Fullscreen : 0)
		{
			logger.Info("Initializing Group Sum game window");
			this.Settings = settings;

			Keyboard.KeyDown += Keyboard_KeyDown;

			plotSurface = new GroupSumPlotSurface(Settings.TargetNumber);
			
			maximumPossibleSum = Settings.Players * (GroupSumSettings.PlayerButtonCount - 1);
			plotSurface.SetPlotAxes(0, 1, 0, maximumPossibleSum + 1);

			CalculateSizesAndLoadFonts();
			SetStatusText(string.Format("Waiting for guesses ({0})", Settings.FirstRoundSeconds));
						
			// Open data file
			if (!string.IsNullOrEmpty(Settings.DataFilePath))
			{
				logger.DebugFormat("Opening data file at {0}", Settings.DataFilePath);
				dataWriter = new YAMLOutputStream(Settings.DataFilePath);
			}

			// Validate number of players
			var textureBasePath = Path.Combine(basePath, Path.Combine("etc", "player_images"));
			var playerImageCount = Directory.GetFiles(textureBasePath, "*.png").Length;

			if (Settings.Players > playerImageCount)
			{
				logger.WarnFormat("Too few player images are present, only allowing {0} players", playerImageCount);
				Settings.Players = playerImageCount;
			}

			players = Enumerable.Range(0, Settings.Players).Select(i => new Player(i)).ToArray();

			// Load textures
			logger.DebugFormat("Loading textures from {0}", textureBasePath);
			
			playerTextureIds = Textures.LoadPlayers(textureBasePath, Settings.Players).ToArray();
			
			// Output settings and initial player positions
			if (dataWriter != null)
			{
				dataWriter.WriteStartDocument();
				dataWriter.WriteHashSingle("Version", GroupSumSettings.FileVersion);

				dataWriter.WriteLine();
				dataWriter.WriteStartList("Settings");
				dataWriter.WriteHashSingle("Description", Settings.GameDescription);
				dataWriter.WriteHashSingle("Players", Settings.Players);
				dataWriter.WriteHashSingle("First Round Duration", Settings.FirstRoundSeconds);
				dataWriter.WriteHashSingle("Round Duration", Settings.RoundSeconds);
				dataWriter.WriteHashSingle("Target Number", Settings.TargetNumber);
				dataWriter.WriteHashSingle("Range", string.Format("{0} - {1}",
					settings.MinNumber, settings.MaxNumber));
					
				dataWriter.WriteHashSingle("Show Numeric Feedback", Settings.ShowNumericFeedback);
				dataWriter.WriteHashSingle("Use Previous Input", Settings.UsePreviousRoundInput);

				dataWriter.WriteEndList();

				dataWriter.WriteLine();
				dataWriter.WriteStartList("Rounds");
				dataWriter.WriteComment("round, player, guess");
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

			secondsLeftInRound = Settings.FirstRoundSeconds;
			roundTimer = new System.Timers.Timer(1000);
			roundTimer.Elapsed += roundTimer_Elapsed;
			roundTimer.Start();
		}

		#endregion
		
		#region Window Event Handlers
		
		protected override void OnLoad (System.EventArgs e)
		{
			GL.ClearColor(Color.White);
			GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.LineSmooth);
		}
		
		protected override void OnUnload (System.EventArgs e)
		{
			logger.Debug("Shutting down Group Sum");
			
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

			GL.DeleteTexture(plotSurface.TextureId);
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

			// Cancel any pending animations
			animator.FinishAndClear();

			CalculateSizesAndLoadFonts();
			
			plotSurface.Resize((int)plotBounds.Width, (int)plotBounds.Height);
			plotSurface.RedrawPlotSurface();
		}
		
		protected override void OnUpdateFrame (OpenTK.FrameEventArgs e)
		{
			animator.Update(e.Time);

			if (isPlotUpdated)
			{
				isPlotUpdated = false;
				plotSurface.RedrawPlotSurface();
			}

			if (isStatusTextChanged)
			{
				isStatusTextChanged = false;
				PositionStatusText();
			}
		}
		
		protected override void OnRenderFrame (OpenTK.FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.LoadIdentity();

			// Draw status text box
			GL.Color3(Themes.Blue.Background);
			GL.Begin(BeginMode.Quads);
			GL.Vertex2(0, 0);
			GL.Vertex2(0, plotBounds.Top);
			GL.Vertex2(this.Width, plotBounds.Top);
			GL.Vertex2(this.Width, 0);
			GL.End();

			GL.LineWidth(2);
			GL.Color3(Themes.Blue.Border);
			GL.Begin(BeginMode.Lines);
			GL.Vertex2(0, plotBounds.Top - 2);
			GL.Vertex2(this.Width, plotBounds.Top - 2);
			GL.End();

			// Draw status text
			GL.PushMatrix();
			GL.Translate(statusTextPoint.X, statusTextPoint.Y, 0);
			printer.Print(statusText, statusFont, Themes.Blue.Foreground,
				RectangleF.Empty, TextPrinterOptions.NoCache);
				
			GL.PopMatrix();

			// Draw plot surface
			plotSurface.DrawWithOpenGL(plotBounds);

			// Draw players
			GL.Color3(Themes.Black.Background);
			GL.Begin(BeginMode.Quads);
			GL.Vertex2(0, playerSectionTop);
			GL.Vertex2(0, this.Height);
			GL.Vertex2(this.Width, this.Height);
			GL.Vertex2(this.Width, playerSectionTop);
			GL.End();

			GL.LineWidth(2);
			GL.Color3(Themes.Black.Border);
			GL.Begin(BeginMode.Lines);
			GL.Vertex2(0, playerSectionTop);
			GL.Vertex2(this.Width, playerSectionTop);
			GL.End();

			for (int playerIndex = 0; playerIndex < Settings.Players; playerIndex++)
			{
				// Layout players left to right, top to bottom
				var slotLeft = (playerSlotSize * playerIndex) % this.Width;
				
				var playerSlotRow = (double)(playerIndex / playersPerSectionRow);
				var slotTop = playerSectionTop + (playerSlotRow * playerSlotSize);

				// Draw highlighted background if player has answered
				var opacity = (int)(players[playerIndex].HighlightOpacity * 255.0);
				
				GL.Color4(Color.FromArgb(opacity, Themes.Yellow.Background));							
				GL.Begin(BeginMode.Quads);
				GL.Vertex2(slotLeft, slotTop);
				GL.Vertex2(slotLeft, slotTop + playerSlotSize);
				GL.Vertex2(slotLeft + playerSlotSize, slotTop + playerSlotSize);
				GL.Vertex2(slotLeft + playerSlotSize, slotTop);
				GL.End();

				// Center players in their slots
				var playerLeft = slotLeft + ((playerSlotSize - playerSize) / 2.0);				
				var playerTop = slotTop + ((playerSlotSize - playerSize) / 2.0);

				GL.Enable(EnableCap.Texture2D);
				GL.Color3(Color.White);
				GL.BindTexture(TextureTarget.Texture2D, playerTextureIds[playerIndex]);
				GL.Begin(BeginMode.Quads);
				GL.TexCoord2(0, 0);
				GL.Vertex2(playerLeft, playerTop);

				GL.TexCoord2(0, 1);
				GL.Vertex2(playerLeft, playerTop + playerSize);

				GL.TexCoord2(1, 1);
				GL.Vertex2(playerLeft + playerSize, playerTop + playerSize);

				GL.TexCoord2(1, 0);
				GL.Vertex2(playerLeft + playerSize, playerTop);
				GL.End();
				GL.Disable(EnableCap.Texture2D);
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
						for (int playerIndex = 0; playerIndex < Settings.Players; playerIndex++)
						{
							ProcessPlayerButton(playerIndex, rand.Next(GroupSumSettings.PlayerButtonCount) + 1);
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
				
				if ((playerId >= 0) && (playerId < Settings.Players))
				{	
					ProcessPlayerButton(playerId, button);
				}
				
				// Continue listening for input
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

		protected void ProcessPlayerButton(int playerId, int button)
		{
			var player = players[playerId];

			// Button 0 comes in as 10, so this will force it to be
			// interpreted as a 0 input.
			player.Answer = button % 10;

			animator.PropertyTo(player, "HighlightOpacity", 0, 0.2, 0, 0, AfterDarkAnimation);
		}

		private void AfterDarkAnimation(DoubleAnimationInfo animation)
		{
			animator.PropertyTo(animation.Object, "HighlightOpacity", 1, 0.5);
		}
		
		#endregion

		#region Timer Event Handlers

		private void roundTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			secondsLeftInRound--;

			if (secondsLeftInRound <= 0)
			{
				secondsLeftInRound = Settings.RoundSeconds;

				// Calculate group sum
				double groupSum = players.Sum(p => p.Answer);

				previousGroupSums.Add(groupSum);

				// Write to data file
				if (dataWriter != null)
				{
          foreach (var player in players)
          {
            dataWriter.WriteText
              (string.Format("{0}, {1}, {2}",
                             previousGroupSums.Count, player.Id, player.Answer));
          }
				}

				// Display results
				if (groupSum < Settings.TargetNumber)
				{
					if (Settings.ShowNumericFeedback)
					{
						baseStatusText = string.Format("Low by {0}!", Settings.TargetNumber - (int)groupSum);
					}
					else
					{
						baseStatusText = "Too low!";
					}
				}
				else if (groupSum > Settings.TargetNumber)
				{
					if (Settings.ShowNumericFeedback)
					{
						baseStatusText = string.Format("High by {0}!", (int)groupSum - Settings.TargetNumber);
					}
					else
					{
						baseStatusText = "Too high!";
					}
				}
				else
				{
					roundTimer.Stop();
					baseStatusText = string.Format("Correct! The answer was {0}", Settings.TargetNumber);
				}

				// Reset player answers and highlights
				if (!Settings.UsePreviousRoundInput)
				{
					foreach (var player in players)
					{
						player.Answer = 0;
						animator.PropertyTo(player, "HighlightOpacity", 0, 0.5);
					}
				}
					
				// Update plot surface
				plotSurface.SetPlotAxes(0, previousGroupSums.Count + 1, 0, maximumPossibleSum + 1);
				plotSurface.UpdatePlotData(previousGroupSums);
				isPlotUpdated = true;
			}

			SetStatusText(string.Format("{0} ({1})", baseStatusText, secondsLeftInRound));
		}

		#endregion
		
		#region Utility Methods

		protected void CalculateSizesAndLoadFonts()
		{
			float windowWidth = this.Width, windowHeight = this.Height;

			// Reserve part of the top screen space for the description text
			windowHeight *= 0.93f;

			// Fit 12 players per section line
			playerSlotSize = this.Width / (double)playersPerSectionRow;
			playerSize = playerSlotSize * 0.85;
			playerSectionHeight = playerSlotSize * Math.Ceiling((double)Settings.Players / (double)playersPerSectionRow);
			playerSectionTop = this.Height - playerSectionHeight;

			plotBounds.Width = windowWidth * 0.98f;
			plotBounds.Height = windowHeight  - (float)playerSectionHeight;
			plotBounds.X = (windowWidth - plotBounds.Width) / 2.0f;
			plotBounds.Y = this.Height - windowHeight;

			// Load fonts
			statusFont = new Font(FontFamily.GenericMonospace, (float)plotBounds.Top * 0.55f, FontStyle.Bold);
			PositionStatusText();
		}

		protected void SetStatusText(string text)
		{
			statusText = text;
			isStatusTextChanged = true;
		}

		protected void PositionStatusText()
		{
			var statusExtents = printer.Measure(statusText, statusFont);
			statusTextPoint.X = ((float)this.Width - statusExtents.BoundingBox.Width) / 2.0f;
			statusTextPoint.Y = (float)Math.Truncate((plotBounds.Top - statusExtents.BoundingBox.Height) / 2.0f);
		}

		#endregion
	}

	public class Player
	{
    public int Id { get; set; }
		public int Answer { get; set; }
		public double HighlightOpacity { get; set; }

    public Player(int id)
    {
      Id = id;
    }
	}
}
