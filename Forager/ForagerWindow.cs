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

namespace Forager
{
  public class ForagerWindow : GameWindow
  {
    #region Constants

    private const int playersPerSectionRow = 15;
    private const double playerTeleportTime = 0.5;

    #endregion
    
    #region Fields

    protected log4net.ILog logger = log4net.LogManager.GetLogger("Forager.ForagerWindow");
    protected readonly string basePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);    
    protected Random rand = new Random();
    protected DoubleAnimator animator = new DoubleAnimator();
    protected GameState gameState = GameState.StartingUp;

    protected int[] playerTextureIds = null, playerGrayTextureIds = null, foodTextureIds = null;
    protected double playerSlotSize = 0, playerSectionTop = 0, playerSectionHeight = 0;
    protected double playerSize = 0;
    protected double plotAreaTop = 0;

    protected RectangleF[] plots = null;
    protected PointF[] plotNumberOffsets = null;
    protected TextExtents plotNumberExtents = null;
    protected int plotProbabilityIndex = 0;

    protected Font plotNumberFont = null, statusFont = null,
      scoreFont = null, foodCountFont = null;

    protected string statusText = "00:00";
    protected PointF statusTextPoint = PointF.Empty;
    protected bool statusTextChanged = true;
    
    protected TextPrinter statusPrinter = new TextPrinter(TextQuality.Medium);
    protected TextPrinter plotPrinter = new TextPrinter(TextQuality.Medium);

    protected Player[] players = null;
    protected PlayerShadow[] playerShadows = null;

    protected bool isFoodReady = false;
    protected double foodSize = 0, foodOffset = 0;

    protected EmptyPlotFood[] emptyPlotFood = null;
        
    protected TcpClient inputClient = new TcpClient();
    protected byte[] inputBuffer = new byte[2];

    protected System.Timers.Timer roundTimer = null;
    protected int gameSecondsLeft = 0, probabilitySecondsLeft = 0;
    
    protected EndGameState endGameState = new EndGameState();
    
    protected YAMLOutputStream dataWriter = null;
    protected System.Diagnostics.Stopwatch inputTimer = new System.Diagnostics.Stopwatch();
    
    #endregion
    
    #region Properties
    
    public ForagerSettings Settings { get; protected set; }
    
    #endregion
    
    #region Constructor
    
    public ForagerWindow(ForagerSettings settings) : base(settings.ScreenWidth,
      settings.ScreenHeight, GraphicsMode.Default, "Forager",
      settings.Fullscreen ? GameWindowFlags.Fullscreen : 0)
    {
      logger.Info("Initializing Forager game window");
      this.Settings = settings;

      Keyboard.KeyDown += Keyboard_KeyDown;

      // Open data file
      if (!string.IsNullOrEmpty(Settings.DataFilePath))
      {
        logger.DebugFormat("Opening data file at {0}", Settings.DataFilePath);
        dataWriter = new YAMLOutputStream(Settings.DataFilePath);
      }

      // Validate number of players
      var playerTextureBasePath = Path.Combine(basePath, Path.Combine("etc", "player_images"));
      var playerImageCount = Directory.GetFiles(playerTextureBasePath, "*.png").Length;

      if (Settings.Players > playerImageCount)
      {
        logger.WarnFormat("Too few player images are present, only allowing {0} players", playerImageCount);
        Settings.Players = playerImageCount;
      }

      players = Enumerable.Range(0, Settings.Players).Select(i => new Player(i)).ToArray();
      playerShadows = players.Select(p => new PlayerShadow(p.Id)).ToArray();

      // Load player textures
      logger.DebugFormat("Loading player textures from {0}", playerTextureBasePath);
      
      playerTextureIds = Textures.LoadPlayers(playerTextureBasePath, Settings.Players).ToArray();
      playerGrayTextureIds = Textures.LoadPlayers(playerTextureBasePath, Settings.Players,
        (image) => Textures.MakeGrayScale(image)).ToArray();

      // Load food textures
      var foodTextureBasePath = Path.Combine(basePath, Path.Combine("etc", "food_images"));
      var foodImageCount = Directory.GetFiles(foodTextureBasePath, "*.png").Length;

      logger.DebugFormat("Loading food textures from {0}", foodTextureBasePath);
      foodTextureIds = Textures.Load(foodTextureBasePath, "food", foodImageCount, x => x).ToArray();

      plots = Enumerable.Range(0, Settings.Plots).Select(i => RectangleF.Empty).ToArray();
      plotNumberOffsets = Enumerable.Range(0, Settings.Plots).Select(i => PointF.Empty).ToArray();
      emptyPlotFood = Enumerable.Range(0, Settings.Plots).Select(i => new EmptyPlotFood()).ToArray();
      
      CalculateSizesAndLoadFonts();
      NormalizeProbabilities();

      AssignPlayerPositions();
      
      // Output settings
      if (dataWriter != null)
      {
        dataWriter.WriteStartDocument();
        dataWriter.WriteHashSingle("Version", ForagerSettings.FileVersion);

        dataWriter.WriteLine();
        dataWriter.WriteStartList("Settings");
        dataWriter.WriteHashSingle("Description", Settings.GameDescription);
        dataWriter.WriteHashSingle("Players", Settings.Players);

        dataWriter.WriteStartList("Player Plots");
        dataWriter.WriteComment("player, plot");

        foreach (var player in players)
        {
          dataWriter.WriteText(string.Format("{0}, {1}", player.Id, player.PlotNumber + 1));
        }

        // Player plots
        dataWriter.WriteEndList();
        
        dataWriter.WriteHashSingle("Game Duration", Settings.GameSeconds);
        dataWriter.WriteHashSingle("Travel Time", Settings.TravelTime);
        dataWriter.WriteHashSingle("Food Rate", Settings.FoodRate);
        dataWriter.WriteHashSingle("Plots", Settings.Plots);

        if (Settings.ProbabilityShiftTimes.Count > 0)
        {
          dataWriter.WriteStartList("Probability Shift Times");
          dataWriter.WriteComment("set, seconds from last shift");

          int setIndex = 1;

          foreach (var shiftTime in settings.ProbabilityShiftTimes)
          {
            dataWriter.WriteText(string.Format("{0}, {1}", setIndex, shiftTime));
            setIndex++;
          }

          // Probability Shift Times
          dataWriter.WriteEndList();
        }
        
        dataWriter.WriteStartList("Plot Probabilities");
        dataWriter.WriteComment("set, plot #, probability");

        int probabilitySet = 1;
        double probabilitySum = 0;

        foreach (var probabilities in Settings.PlotProbabilities)
        {        
          for (int plotIndex = 0; plotIndex < probabilities.Count; plotIndex++)
          {
            var plotProbability = probabilities[plotIndex] - probabilitySum;
            probabilitySum += plotProbability;
          
            dataWriter.WriteText(string.Format("{0}, {1}, {2:N2}", probabilitySet,
                                               plotIndex + 1, plotProbability));
          }

          probabilitySet++;
          probabilitySum = 0;
        }

        // Plot probabilities
        dataWriter.WriteEndList();

        // Settings
        dataWriter.WriteEndList();

        dataWriter.WriteLine();
        dataWriter.WriteStartList("Actions");
        dataWriter.WriteComment("time in milliseconds, action, player, plot number");
      }

      inputTimer.Start();
      
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

      gameSecondsLeft = Settings.GameSeconds;
      probabilitySecondsLeft = Settings.ProbabilityShiftTimes.FirstOrDefault();
      UpdateStatusText();

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
      logger.Debug("Shutting down Forager");
      
      if (dataWriter != null)
      {
        dataWriter.WriteEndList();
        dataWriter.WriteEndDocument();
        
        // Close output file
        dataWriter.Close();
      }
      
      logger.Debug("Freeing textures");     
      foreach (var textureId in Enumerable.Concat(playerTextureIds,
        Enumerable.Concat(playerGrayTextureIds, foodTextureIds)))
      {
        GL.DeleteTexture(textureId);
      }

      if (endGameState.TextureId > 0)
      {
        GL.DeleteTexture(endGameState.TextureId);
      }
    }
    
    protected override void OnResize(EventArgs e)
    {
      // Setup 2-D viewing mode with a viewport the size of the window
      GL.Viewport(0, 0, ClientRectangle.Width, ClientRectangle.Height);
      GL.MatrixMode(MatrixMode.Projection);
      GL.LoadIdentity();
      GL.Ortho(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
      
      GL.MatrixMode(MatrixMode.Modelview);
      GL.LoadIdentity();

      // Cancel any pending animations
      lock (animator)
      {
        animator.FinishAndClear();
      }

      CalculateSizesAndLoadFonts();
    }
    
    protected override void OnUpdateFrame (OpenTK.FrameEventArgs e)
    {
      animator.Update(e.Time);

      if (gameState == GameState.Running)
      {   
        // Move players
        foreach (var player in players)
        {
          if (player.State == PlayerState.ReadyToTravel)
          {
            player.Position = FindOpenPosition(PlayerState.Traveling);
            player.State = PlayerState.Traveling;
            player.TravelTimeLeft = Settings.TravelTime;
            player.Opacity = 0;
            
            animator.PropertyTo(player, "TravelTimeLeft", 0,
              player.TravelTimeLeft, 0, 0, OnPlayerFinishTravel);
  
            // Set up fade
            animator.PropertyTo(player, "Opacity", 1, playerTeleportTime);
  
            var shadow = playerShadows[player.Id];
            shadow.Left = player.Left;
            shadow.Top = player.Top;
            shadow.Opacity = 1;
            shadow.IsGray = false;
  
            animator.PropertyTo(shadow, "Opacity", 0, playerTeleportTime);
  
            // Record player movement
            if (dataWriter != null)
            {
              dataWriter.WriteText(string.Format("{0}, {1}, {2}",
                inputTimer.ElapsedMilliseconds, "Travel", player.Id, player.PlotNumber + 1));
            }
          }
          else if (player.State == PlayerState.FinishedTraveling)
          {
            player.Position = FindOpenPosition(PlayerState.WaitingForInput, player.PlotNumber);
            player.State = PlayerState.WaitingForInput;
            player.Opacity = 0;
  
            // Set up fade
            animator.PropertyTo(player, "Opacity", 1, playerTeleportTime);
  
            var shadow = playerShadows[player.Id];
            shadow.Left = player.Left;
            shadow.Top = player.Top;
            shadow.Opacity = 1;
            shadow.IsGray = true;
  
            animator.PropertyTo(shadow, "Opacity", 0, playerTeleportTime);
  
            // Record player movement
            if (dataWriter != null)
            {
              dataWriter.WriteText(string.Format("{0}, {1}, {2}, {3}",
                inputTimer.ElapsedMilliseconds, "Move", player.Id, player.PlotNumber + 1));
            }
          }
        }
  
        // Give players food
        if (isFoodReady)
        {
          isFoodReady = false;

          foreach (var player in players)         
          {
            player.FoodFound = 0;
          }

          foreach (var emptyFood in emptyPlotFood)
          {
            emptyFood.Count = 0;
          }
  
          for (int foodNumber = 0; foodNumber < Settings.FoodRate; foodNumber++)
          {
            var randomNumber = rand.NextDouble();
            var luckyPlotNumber = 0;
  
            for (int plotIndex = 0;
                 plotIndex < Settings.PlotProbabilities[plotProbabilityIndex].Count;
                 plotIndex++)
            {
              if (randomNumber <= Settings.PlotProbabilities[plotProbabilityIndex][plotIndex])
              {
                luckyPlotNumber = plotIndex;
                break;
              }
            }         
            
            var luckyPlayer = players.Where(p => (p.PlotNumber == luckyPlotNumber) &&
              (p.State == PlayerState.WaitingForInput))
              .OrderBy(p => rand.Next()).FirstOrDefault();
  
            if (luckyPlayer == null)
            {
              var emptyFood = emptyPlotFood[luckyPlotNumber];

              emptyFood.FoodTextureId = foodTextureIds[rand.Next(0, foodTextureIds.Length)];
              emptyFood.Opacity = 1;
              emptyFood.Count++;

              animator.PropertyTo(emptyFood, "Opacity", 0, 0.2, 0, 0.8, null);
              
              emptyFood.IsPresent = true;

              // No one was on the plot. Too bad!
              continue;
            }
  
            luckyPlayer.Score++;
            luckyPlayer.FoodFound++;
            luckyPlayer.FoodOpacity = 1;
            luckyPlayer.FoodTextureId = foodTextureIds[rand.Next(foodTextureIds.Length)];
            
            animator.PropertyTo(luckyPlayer, "TopOffset", playerSize * 0.3, 0.1, 1, 0, null);
            animator.PropertyTo(luckyPlayer, "FoodOpacity", 0, 0.2, 0, 0.8, null);
  
            // Record food disbursment
            if (dataWriter != null)
            {
              dataWriter.WriteText(string.Format("{0}, {1}, {2}, {3}",
                inputTimer.ElapsedMilliseconds, "Food", luckyPlayer.Id,
                  luckyPlayer.PlotNumber + 1));
            }
          }
        } // if (isFoodReady)
      }
      else if (gameState == GameState.Finished)
      {
          endGameState.TextureId = Textures.Load(new Bitmap(this.Width, this.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb));

          GL.BindTexture(TextureTarget.Texture2D, endGameState.TextureId);
          GL.CopyTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            0, 0, this.Width, this.Height, 0);

          gameState = GameState.Blurring;
      }
      else if (gameState == GameState.BlurFinished)
      {
        double scoreBoardWidth = (this.Width * 0.85), scoreBoardHeight = (this.Height * 0.85);
        
        endGameState.ScoreBoardLeft = (this.Width - scoreBoardWidth) / 2.0;
        endGameState.ScoreBoardTop = (this.Height - scoreBoardHeight) / 2.0;

        int playersPerColumn = (int)(scoreBoardHeight / playerSlotSize) - 1;
        double columnOffset = scoreBoardWidth / Math.Ceiling((double)Settings.Players / (double)playersPerColumn);

        int scoreRow = 0, scoreColumn = 0;
        var playerScoreOrder = players.OrderByDescending(p => p.Score).Select(p => p.Id).ToList();
        
        // Position players
        foreach (var playerIndex in playerScoreOrder)
        {
          var player = players[playerIndex];
          
          var slotLeft = endGameState.ScoreBoardLeft + (scoreColumn * columnOffset);            
          var slotTop = endGameState.ScoreBoardTop + (scoreRow * playerSlotSize);
          
          player.Left = slotLeft + ((playerSlotSize - playerSize) / 2.0);       
          player.Top = slotTop + ((playerSlotSize - playerSize) / 2.0);

          scoreRow++;

          if (scoreRow > playersPerColumn)
          {
            scoreRow = 0;
            scoreColumn++;
          }
        }
          
        animator.PropertyTo(endGameState, "ScoreBoardOpacity", 0.9, 0.5);
        animator.PropertyTo(endGameState, "PlayerScoreBoardOpacity", 1, 1.0);
        
        gameState = GameState.ShowingScoreBoard;
        
      } // BlurFinished
    }
    
    protected override void OnRenderFrame (OpenTK.FrameEventArgs e)
    {
      GL.Clear(ClearBufferMask.ColorBufferBit);
      GL.LoadIdentity();

      if ((gameState == GameState.StartingUp) ||
          (gameState == GameState.Running))
      {
        // Draw status text box
        GL.Color3(Themes.Black.Background);
        GL.Begin(BeginMode.Quads);
        GL.Vertex2(0, 0);
        GL.Vertex2(0, plotAreaTop);
        GL.Vertex2(this.Width, plotAreaTop);
        GL.Vertex2(this.Width, 0);
        GL.End();
  
        // Draw plots
        for (int plotIndex = 0; plotIndex < plots.Length; plotIndex++)
        {
          var plotRectangle = plots[plotIndex];
  
          GL.PushMatrix();
          GL.Translate(plotRectangle.Left, plotRectangle.Top, 0);
          
          GL.Color3(Themes.Green.Background);
          GL.Begin(BeginMode.Quads);
          GL.Vertex2(0, 0);
          GL.Vertex2(0, plotRectangle.Height);
          GL.Vertex2(plotRectangle.Width, plotRectangle.Height);
          GL.Vertex2(plotRectangle.Width, 0);
          GL.End();
  
          GL.Color3(Themes.Green.Border);
          GL.LineWidth(2);
          GL.Begin(BeginMode.LineLoop);
          GL.Vertex2(0, 0);
          GL.Vertex2(0, plotRectangle.Height);
          GL.Vertex2(plotRectangle.Width, plotRectangle.Height);
          GL.Vertex2(plotRectangle.Width,0);
          GL.End();
  
          var textOffset = plotNumberOffsets[plotIndex];
          
          GL.Translate(textOffset.X, textOffset.Y, 0);
          plotPrinter.Print((plotIndex + 1).ToString(),
                            plotNumberFont, Themes.Green.Border);
  
          GL.PopMatrix();

          var emptyFood = emptyPlotFood[plotIndex];

          if (emptyFood.IsPresent)
          {
            RenderFood(emptyFood.FoodTextureId, emptyFood.Count,
                       plotRectangle.Left + (plotRectangle.Width / 2.0f),
                       (plotRectangle.Top + (plotRectangle.Height / 2.0f)) + (playerSize / 2.0),
                       emptyFood.Opacity);
          }
        }

        // Draw players
        GL.Color3(Themes.Blue.Background);
        GL.Begin(BeginMode.Quads);
        GL.Vertex2(0, playerSectionTop);
        GL.Vertex2(0, this.Height);
        GL.Vertex2(this.Width, this.Height);
        GL.Vertex2(this.Width, playerSectionTop);
        GL.End();
  
        GL.LineWidth(2);
        GL.Color3(Themes.Blue.Border);
        GL.Begin(BeginMode.Lines);
        GL.Vertex2(0, playerSectionTop);
        GL.Vertex2(this.Width, playerSectionTop);
        GL.End();
  
        for (int playerIndex = 0; playerIndex < Settings.Players; playerIndex++)
        {
          var player = players[playerIndex];
          double slotLeft = 0, slotTop = 0;
          int textureId = 0;
  
          if (player.State == PlayerState.Traveling)
          {     
            // Layout players left to right, top to bottom
            slotLeft = (playerSlotSize * player.Position) % this.Width;
          
            var playerSlotRow = (double)(player.Position / playersPerSectionRow);
            slotTop = playerSectionTop + (playerSlotRow * playerSlotSize);
            
            textureId = playerGrayTextureIds[playerIndex];
          }
          else if (player.State == PlayerState.WaitingForInput)
          {
            // Draw in plot
            var plot = plots[player.PlotNumber];
            var playersPerPlotRow = (int)Math.Max(1, Math.Floor(plot.Width / playerSlotSize));
            slotLeft = plot.Left + ((player.Position % playersPerPlotRow) * playerSlotSize);
            slotTop = plot.Top + ((player.Position / playersPerPlotRow) * playerSlotSize);
            
            textureId = playerTextureIds[playerIndex];
          }
          else
          {
            // Don't draw player during in-between states
            continue;
          }
  
          // Center player in the slots
          player.Left = slotLeft + ((playerSlotSize - playerSize) / 2.0);       
          player.Top = slotTop + ((playerSlotSize - playerSize) / 2.0);
  
          var actualPlayerTop = player.Top - player.TopOffset;
  
          // Draw texture of player
          GL.Color4(1, 1, 1, player.Opacity);
          GL.Enable(EnableCap.Texture2D);
          GL.BindTexture(TextureTarget.Texture2D, textureId);
          
          GL.Begin(BeginMode.Quads);
          GL.TexCoord2(0, 0);
          GL.Vertex2(player.Left, actualPlayerTop);
  
          GL.TexCoord2(0, 1);
          GL.Vertex2(player.Left, actualPlayerTop + playerSize);
  
          GL.TexCoord2(1, 1);
          GL.Vertex2(player.Left + playerSize, actualPlayerTop + playerSize);
  
          GL.TexCoord2(1, 0);
          GL.Vertex2(player.Left + playerSize, actualPlayerTop);
          GL.End();
  
          // Draw food if player has received some
          if ((player.FoodOpacity > 0) && (player.FoodFound > 0))
          {
            RenderFood(player.FoodTextureId, player.FoodFound,
                       player.Left + (playerSize / 2.0), actualPlayerTop + playerSize,
                       player.FoodOpacity);
          }       
  
          GL.Disable(EnableCap.Texture2D);
        }
  
        // Draw player shadows
        for (int playerIndex = 0; playerIndex < Settings.Players; playerIndex++)
        {
          var shadow = playerShadows[playerIndex];
  
          if (shadow.Opacity <= 0)
          {
            continue;
          }
  
          GL.Color4(1, 1, 1, shadow.Opacity);
          GL.Enable(EnableCap.Texture2D);
          GL.BindTexture(TextureTarget.Texture2D, shadow.IsGray ?
            playerGrayTextureIds[shadow.PlayerId] : playerTextureIds[shadow.PlayerId]);
          
          GL.Begin(BeginMode.Quads);
          GL.TexCoord2(0, 0);
          GL.Vertex2(shadow.Left, shadow.Top);
  
          GL.TexCoord2(0, 1);
          GL.Vertex2(shadow.Left, shadow.Top + playerSize);
  
          GL.TexCoord2(1, 1);
          GL.Vertex2(shadow.Left + playerSize, shadow.Top + playerSize);
  
          GL.TexCoord2(1, 0);
          GL.Vertex2(shadow.Left + playerSize, shadow.Top);
          GL.End();
          
          GL.Disable(EnableCap.Texture2D);
        }

        // Draw status text
        GL.PushMatrix();
        GL.Translate(statusTextPoint.X, statusTextPoint.Y, 0);
        statusPrinter.Print(statusText, statusFont, Themes.Black.Foreground,
          RectangleF.Empty, TextPrinterOptions.NoCache);
          
        GL.PopMatrix();
      }
      else if ((gameState == GameState.Blurring) ||
               (gameState == GameState.BlurFinished) ||
               (gameState == GameState.ShowingScoreBoard))
      {
        // Render background texture
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, endGameState.TextureId);

        GL.Color3(Color.White);
        GL.Begin(BeginMode.Quads);

        GL.TexCoord2(0, 1);
        GL.Vertex2(0, 0);

        GL.TexCoord2(0, 0);
        GL.Vertex2(0, this.Height);
        
        GL.TexCoord2(1, 0);
        GL.Vertex2(this.Width, this.Height);
        
        GL.TexCoord2(1, 1);
        GL.Vertex2(this.Width, 0);
        GL.End();
        
        if (gameState == GameState.Blurring)
        {
          double centerLeft = this.Width / 2.0, centerTop = this.Height / 2.0;
          
          GL.Translate(centerLeft, centerTop, 0);
          GL.Rotate(endGameState.Rotation, 0, 0, 1);
          GL.Translate(-centerLeft, -centerTop, 0);
          
          // Render blur
          GL.Color4(1, 1, 1, endGameState.BlurOpacity);
          GL.Begin(BeginMode.Quads);

          GL.TexCoord2(0 + endGameState.TextureOffset, 1 - endGameState.TextureOffset);
          GL.Vertex2(0, 0);
  
          GL.TexCoord2(0 + endGameState.TextureOffset, 0 + endGameState.TextureOffset);
          GL.Vertex2(0, this.Height);
          
          GL.TexCoord2(1 - endGameState.TextureOffset, 0 + endGameState.TextureOffset);
          GL.Vertex2(this.Width, this.Height);
          
          GL.TexCoord2(1 - endGameState.TextureOffset, 1 - endGameState.TextureOffset);
          GL.Vertex2(this.Width, 0);
          GL.End();

          GL.CopyTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb,
            0, 0, this.Width, this.Height, 0);

          endGameState.BlurOpacity -= (0.5 * e.Time);
          endGameState.TextureOffset += (0.001 * e.Time);
          endGameState.Rotation += (1 * e.Time);

          if (endGameState.BlurOpacity <= 0.0)
          {
            gameState = GameState.BlurFinished;
          }
          
        } // Blurring
        else if (gameState == GameState.ShowingScoreBoard)
        {
          var scoreBoardOpacity = (byte)(255 * endGameState.ScoreBoardOpacity);
          
          // Score board background
          GL.Disable(EnableCap.Texture2D);
          GL.Color4(Color.FromArgb(scoreBoardOpacity, Themes.Blue.Background));
          GL.Begin(BeginMode.Quads);
          
          GL.Vertex2(endGameState.ScoreBoardLeft, endGameState.ScoreBoardTop);
          GL.Vertex2(endGameState.ScoreBoardLeft, this.Height - endGameState.ScoreBoardTop);
          GL.Vertex2(this.Width - endGameState.ScoreBoardLeft, this.Height - endGameState.ScoreBoardTop);
          GL.Vertex2(this.Width - endGameState.ScoreBoardLeft, endGameState.ScoreBoardTop);
          
          GL.End();

          // Score board border
          GL.Color4(Color.FromArgb(scoreBoardOpacity, Themes.Blue.Border));
          GL.LineWidth(2);
          GL.Begin(BeginMode.LineLoop);
          
          GL.Vertex2(endGameState.ScoreBoardLeft, endGameState.ScoreBoardTop);
          GL.Vertex2(endGameState.ScoreBoardLeft, this.Height - endGameState.ScoreBoardTop);
          GL.Vertex2(this.Width - endGameState.ScoreBoardLeft, this.Height - endGameState.ScoreBoardTop);
          GL.Vertex2(this.Width - endGameState.ScoreBoardLeft, endGameState.ScoreBoardTop);
          
          GL.End();

          // Player scores
          for (int playerIndex = 0; playerIndex < Settings.Players; playerIndex++)
          {
            var player = players[playerIndex];
            
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, playerTextureIds[playerIndex]);
            GL.Color4(1, 1, 1, endGameState.PlayerScoreBoardOpacity);
            
            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(player.Left, player.Top);
    
            GL.TexCoord2(0, 1);
            GL.Vertex2(player.Left, player.Top + playerSize);
    
            GL.TexCoord2(1, 1);
            GL.Vertex2(player.Left + playerSize, player.Top + playerSize);
    
            GL.TexCoord2(1, 0);
            GL.Vertex2(player.Left + playerSize, player.Top);
            GL.End();

            GL.Disable(EnableCap.Texture2D);

            var scoreString = string.Format(": {0}", player.Score);
            var scoreExtents = statusPrinter.Measure(scoreString, scoreFont);
            
            GL.PushMatrix();
            GL.Translate(Math.Floor(player.Left + playerSlotSize),
              Math.Floor(player.Top + ((playerSlotSize - scoreExtents.BoundingBox.Height) / 2.0)), 0);
              
            statusPrinter.Print(scoreString, scoreFont, Themes.Blue.Foreground);
            GL.PopMatrix();
          }
          
        } // ShowingScoreBoard

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
              ProcessPlayerButton(playerIndex, rand.Next(ForagerSettings.PlayerButtonCount) + 1);
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
      var newPlotNumber = (int)Math.Min(button - 1, plots.Length - 1);

      if ((gameState != GameState.Running) ||
        (player.State != PlayerState.WaitingForInput) ||
        (player.PlotNumber == newPlotNumber))
      {
        return;
      }

      player.State = PlayerState.ReadyToTravel;
      player.PlotNumber = newPlotNumber;
    }

    #endregion

    #region Timer Event Handlers

    private void roundTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      if (gameState == GameState.StartingUp)
      {
        gameState = GameState.Running;
      }
      else if (gameState == GameState.Running)
      {     
        gameSecondsLeft--;
  
        if (gameSecondsLeft <= 0)
        {
          gameState = GameState.Finished;
          roundTimer.Stop();
        }

        if ((Settings.PlotProbabilities.Count > 1) &&
            probabilitySecondsLeft > 0)
        {
          probabilitySecondsLeft--;

          if (probabilitySecondsLeft <= 0)
          {
            // Shift to next probability set
            plotProbabilityIndex =
              Math.Min(plotProbabilityIndex + 1,
                       Settings.PlotProbabilities.Count - 1);

            if (plotProbabilityIndex < Settings.ProbabilityShiftTimes.Count)
            {
              probabilitySecondsLeft = Settings.ProbabilityShiftTimes[plotProbabilityIndex];
            }
          }
        }
        
        UpdateStatusText();
        isFoodReady = true;
      }
    }

    #endregion
    
    #region Utility Methods

    protected void UpdateStatusText()
    {
      var roundTime = new TimeSpan(0, 0, gameSecondsLeft);
      statusText = string.Format("{0:00}:{1:00}", roundTime.Minutes, roundTime.Seconds);
    }

    protected void NormalizeProbabilities()
    {
      // Normalize all probability sets
      foreach (var probabilities in Settings.PlotProbabilities)
      {
        var totalProbability = probabilities.Sum();
        double rangeEnd = 1.0;

        for (int probabilityIndex = probabilities.Count - 1; probabilityIndex >= 0; probabilityIndex--)
        {
          var currentProbability = probabilities[probabilityIndex] / totalProbability;
          probabilities[probabilityIndex] = rangeEnd;

          rangeEnd -= currentProbability;
        }
      }
    }

    protected void AssignPlayerPositions()
    {
      // Place players randomly
      int[] positionIndices = new int[Settings.Plots];

      foreach (var player in players)
      {
        player.PlotNumber = rand.Next(Settings.Plots);
        player.Position = positionIndices[player.PlotNumber];

        positionIndices[player.PlotNumber]++;
      }
    }

    protected void CalculateSizesAndLoadFonts()
    {
      float windowHeight = this.Height;

      // Reserve part of the top screen space for the status text
      windowHeight *= 0.93f;

      playerSlotSize = this.Width / (double)playersPerSectionRow;
      playerSize = playerSlotSize * 0.9;
      playerSectionHeight = playerSlotSize * Math.Ceiling((double)Settings.Players / (double)playersPerSectionRow);
      playerSectionTop = this.Height - playerSectionHeight;

      foodSize = playerSize * 0.5;
      foodOffset = foodSize * 0.1;

      // Calculate plot layout
      plotAreaTop = this.Height - windowHeight;
      double plotAreaHeight = windowHeight - playerSectionHeight;
      double plotWidth = 0, plotHeight = 0, plotsPerRow = 0;

      switch (plots.Length)
      {
        case 4:
        case 10:
          plotsPerRow = 2;
          break;

        case 6:
        case 9:
          plotsPerRow = 3;
          break;
          
        case 8:
          plotsPerRow = 4;
          break;

        default:
          plotsPerRow = plots.Length;
          break;
      }

      plotWidth = this.Width / plotsPerRow;
      plotHeight = plotAreaHeight / ((double)plots.Length / plotsPerRow);

      // Load fonts
      float plotNumberFontSize = Math.Min(400.0f, (float)Math.Min(plotWidth, plotHeight) * 0.75f);
      plotNumberFont = new Font(FontFamily.GenericMonospace, plotNumberFontSize, FontStyle.Bold);

      plotNumberExtents = plotPrinter.Measure("0", plotNumberFont);

      // Set up plots
      for (int plotIndex = 0; plotIndex < plots.Length; plotIndex++)
      {
        plots[plotIndex] = new RectangleF(
          (float)((plotIndex % plotsPerRow) * plotWidth),
          (float)(plotAreaTop + ((plotIndex / (int)plotsPerRow) * plotHeight)),
          (float)plotWidth, (float)plotHeight
        );

        plotNumberOffsets[plotIndex] = new PointF(
          (float)Math.Truncate((plotWidth - plotNumberExtents.BoundingBox.Width) / 2.0),
          (float)Math.Truncate((plotHeight - plotNumberExtents.BoundingBox.Height) / 2.0)
        );
      }

      statusFont = new Font(FontFamily.GenericMonospace, (float)plotAreaTop * 0.55f, FontStyle.Bold);
      
      var statusExtents = statusPrinter.Measure("00:00", statusFont);
      statusTextPoint.X = (float)Math.Truncate((this.Width - statusExtents.BoundingBox.Width) / 2.0);
      statusTextPoint.Y = (float)Math.Truncate((plotAreaTop - statusExtents.BoundingBox.Height) / 2.0);

      scoreFont = new Font(FontFamily.GenericMonospace, (float)playerSlotSize * 0.5f, FontStyle.Bold);
      foodCountFont = new Font(FontFamily.GenericMonospace, (float)foodSize * 0.5f, FontStyle.Bold);
    }

    protected int FindOpenPosition(PlayerState state)
    {
      return (FindOpenPosition(state, null));
    }

    protected int FindOpenPosition(PlayerState state, int? plotNumber)
    {
      var takenPositions = new List<int>();
      
      for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
      {
        var player = players[playerIndex];

        if ((player.State == state) && (!plotNumber.HasValue ||
          (plotNumber.HasValue && (player.PlotNumber == plotNumber.Value))))
        {
          takenPositions.Add(player.Position);
        }
      }

      for (int position = 0; position < players.Length; position++)
      {
        if (!takenPositions.Contains(position))
        {
          return (position);
        }
      }

      return (players.Length);
    }

    private void OnPlayerFinishTravel(DoubleAnimationInfo info)
    {
      var player = (Player)info.Object;
      player.State = PlayerState.FinishedTraveling;
    }

    protected void RenderFood(int textureId, int count, double centerLeft,
                              double bottom, double opacity)
    {
      double foodLeft = 0, foodTop = 0;

      if (count > 1)
      {
        var foodFoundString = string.Format("x{0}", count);
        var foodTextExtents = plotPrinter.Measure(foodFoundString, foodCountFont);

        var quadInnerWidth = foodSize + foodTextExtents.BoundingBox.Width;
        var quadWidth = quadInnerWidth * 1.1;

        var quadInnerHeight = Math.Max(foodSize, foodTextExtents.BoundingBox.Height);
        var quadHeight = quadInnerHeight * 1.1;

        var quadLeft = centerLeft - (quadWidth / 2.0);
        var quadTop = bottom - quadHeight;
        var quadInnerTop = quadTop + ((quadHeight - quadInnerHeight) / 2.0);

        foodLeft = centerLeft - (quadInnerWidth / 2.0);
        foodTop = quadInnerTop + ((quadInnerHeight - foodSize) / 2.0);

        GL.Disable(EnableCap.Texture2D);

        // Background rectangle
        GL.Color4(0.2, 0.2, 0.2, Math.Max(0, opacity - 0.2));
        GL.Begin(BeginMode.Quads);
        GL.Vertex2(quadLeft, quadTop);
        GL.Vertex2(quadLeft, bottom);
        GL.Vertex2(quadLeft + quadWidth, bottom);
        GL.Vertex2(quadLeft + quadWidth, quadTop);
        GL.End();

        // Border around rectangle
        GL.LineWidth(2);
        GL.Color4(0, 0, 0, Math.Max(0, opacity - 0.2));
        GL.Begin(BeginMode.LineLoop);
        GL.Vertex2(quadLeft, quadTop);
        GL.Vertex2(quadLeft, bottom);
        GL.Vertex2(quadLeft + quadWidth, bottom);
        GL.Vertex2(quadLeft + quadWidth, quadTop);
        GL.End();

        GL.PushMatrix();
        GL.Translate(Math.Round(foodLeft + foodSize),
                     Math.Round(quadInnerTop +
                                ((quadInnerHeight - foodTextExtents.BoundingBox.Height) / 2.0)), 0);

        plotPrinter.Print(foodFoundString, foodCountFont, Color.White);
        GL.PopMatrix();
      }
      else
      {
        foodLeft = centerLeft - (foodSize / 2.0);
        foodTop = bottom - foodSize;
      }

      GL.Color4(1, 1, 1, opacity);
      GL.Enable(EnableCap.Texture2D);
      GL.BindTexture(TextureTarget.Texture2D, textureId);              
      GL.Begin(BeginMode.Quads);
  
      GL.TexCoord2(0, 0);
      GL.Vertex2(foodLeft, foodTop);
  
      GL.TexCoord2(0, 1);
      GL.Vertex2(foodLeft, foodTop + foodSize);
            
      GL.TexCoord2(1, 1);
      GL.Vertex2(foodLeft + foodSize, foodTop + foodSize);
            
      GL.TexCoord2(1, 0);
      GL.Vertex2(foodLeft + foodSize, foodTop);
            
      GL.End();
      GL.Disable(EnableCap.Texture2D);
    }

    #endregion
  }

  public enum GameState
  {
    StartingUp, Running, Finished, Blurring,
    BlurFinished, ShowingScoreBoard
  }

  public class EndGameState
  {
    public int TextureId { get; set; }
    public double TextureOffset { get; set; }
    public double BlurOpacity { get; set; }
    public double Rotation { get; set; }
    
    public double ScoreBoardOpacity { get; set; }
    public double ScoreBoardLeft { get; set; }
    public double ScoreBoardTop { get; set; }
    public double PlayerScoreBoardOpacity { get; set; }

    public EndGameState()
    {
      TextureOffset = 0.002;
      BlurOpacity = 1;
      Rotation = 0;
      ScoreBoardOpacity = 0;
      PlayerScoreBoardOpacity = 0;
    }
  }

  public class EmptyPlotFood
  {
    public bool IsPresent { get; set; }
    public int FoodTextureId { get; set; }
    public double Opacity { get; set; }
    public int Count { get; set; }
  }
}

