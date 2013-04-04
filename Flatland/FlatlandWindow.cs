using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using Nexus.Shared;

namespace Flatland
{
  public class FlatlandWindow : GameWindow
  {
    #region Constants

    protected const double PlayerMoveAnimationSeconds = 0.5;

    #endregion

    #region Fields

    protected log4net.ILog logger = log4net.LogManager.GetLogger("Flatland.FlatlandWindow");
    protected readonly string basePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
    protected Random rand = new Random();
    protected DoubleAnimator animator = new DoubleAnimator();
    protected GameState gameState = GameState.Running;

    protected int pixelCount = 0;
    protected double pixelSize = 0, textureOffset = 0, buttonSize = 0, halfButtonSize = 0;
    protected double boardLeft = 0, boardTop = 0, boardSize = 0;
    protected double timerBoxSize = 0, timerBoxMargin = 0;

    protected List<Player> players = new List<Player>();
    protected int[] playerTextureIds = null;
    protected Queue<Player> respawnQueue = new Queue<Player>();

    protected TextPrinter descriptionText = null, teamNameText = null, smallScoreText = null, bigScoreText = null, communalScoreText = null;
    protected PointF descriptionPoint = PointF.Empty;

    protected TcpClient inputClient = new TcpClient();
    protected byte[] inputBuffer = new byte[2];
    protected System.Diagnostics.Stopwatch inputTimer = new System.Diagnostics.Stopwatch();

    protected YAMLOutputStream dataWriter = null;

    #endregion

    #region Properties

    public FlatlandSettings Settings { get; protected set; }

    #endregion

    #region Constructor

    public FlatlandWindow(FlatlandSettings settings) : base(settings.ScreenWidth,
      settings.ScreenHeight, GraphicsMode.Default, "Flatland",
      settings.Fullscreen ? GameWindowFlags.Fullscreen : 0)
    {
      logger.Info("Initializing Flatland game window");
      this.Settings = settings;

      Keyboard.KeyDown += Keyboard_KeyDown;

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

      pixelCount = Settings.Tiles * Settings.Tiles;

      // Calculate pixel and board sizes
      CalculateSizesAndLoadFonts();

      // Load textures
      logger.DebugFormat("Loading textures from {0}", textureBasePath);
      playerTextureIds = Textures.LoadPlayers(textureBasePath, Settings.Players).ToArray();

      // Create players
      players.AddRange(Enumerable.Range(0, Settings.Players)
        .Select(i => new Player(i))
      );

      bool assignRandomTeams = true;

      if (Settings.GameType == FlatlandGameType.OnePrey)
      {
        bool preyAssigned = false;
        var preyTeam = Settings.Teams.FirstOrDefault(t => t.Kind == FlatlandTeamKind.Prey);
        var predatorTeam = Settings.Teams.FirstOrDefault(t => t.Kind == FlatlandTeamKind.Predator);

        if ((preyTeam == null) || (predatorTeam == null))
        {
          logger.Error("Prey and predator teams are required for One Prey game. Assigning teams randomly...");
          assignRandomTeams = true;
        }
        else
        {
          // Choose a random person to be the prey
          foreach (var player in players.OrderBy(p => rand.Next()))
          {
            if (!preyAssigned)
            {
              player.Team = preyTeam;
              player.SetBoxTheme(preyTeam.Theme);
              player.IsPredator = false;
              player.IsPrey = true;
              preyAssigned = true;
            }
            else
            {
              player.Team = predatorTeam;
              player.SetBoxTheme(predatorTeam.Theme);
              player.IsPredator = true;
              player.IsPrey = false;
              preyAssigned = true;
            }
          } // for each player

          assignRandomTeams = false;
        }

      } // if game type is One Prey

      if (assignRandomTeams)
      {
        // Assign teams in order (players order will be random, but first teams get priority)
        var teamPlayerQueue = new Queue<Player>(players.OrderBy(p => rand.Next()));
        var orderedTeams = Settings.Teams.OrderBy(t => t.TeamIndex).ToList();
        int teamIndex = 0;

        while (teamPlayerQueue.Count > 0)
        {
          var team = orderedTeams[teamIndex];
          var player = teamPlayerQueue.Dequeue();

          player.Team = team;
          player.SetBoxTheme(team.Theme);
          player.IsPredator = (team.Kind == FlatlandTeamKind.Predator);
          player.IsPrey = (team.Kind == FlatlandTeamKind.Prey);

          teamIndex = (teamIndex + 1) % orderedTeams.Count;
        }
      }

      // Only show team names if there's more than one
      Settings.ShowTeamNames = (Settings.Teams.Count > 1);

      // Place players on the board
      SetupPlayerSort();

      // Output settings and initial player positions
      if (dataWriter != null)
      {
        dataWriter.WriteStartDocument();
        dataWriter.WriteHashSingle("Version", FlatlandSettings.FileVersion);

        dataWriter.WriteLine();
        dataWriter.WriteStartList("Settings");
        dataWriter.WriteHashSingle("Description", Settings.GameDescription);
        dataWriter.WriteHashSingle("Players", players.Count);
        dataWriter.WriteHashSingle("Size", string.Format("{0}x{0}", Settings.Tiles));
        dataWriter.WriteHashSingle("Collision Behavior", Settings.CollisionBehavior);
        dataWriter.WriteHashSingle("Game Type", Settings.GameType);

        if (Settings.Teams.Count > 0)
        {
          dataWriter.WriteStartList("Teams");
          dataWriter.WriteComment("team number, theme, move seconds, kind, wrap, scoring system");

          foreach (var team in Settings.Teams)
          {
            dataWriter.WriteText
              (string.Format("{0}, {1}, {2}, {3}, {4}, {5}",
                             team.TeamIndex, team.ThemeName,
                             team.MoveSeconds, team.Kind,
                             team.WrappedMovement, team.ScoringSystem));
          }

          dataWriter.WriteEndList();
        }

        dataWriter.WriteEndList();

        dataWriter.WriteLine();
        dataWriter.WriteStartList("Player Information");
        dataWriter.WriteComment("player, x, y, team");

        foreach (var player in players)
        {
          dataWriter.WriteText(string.Format("{0}, {1}, {2}, {3}",
            player.Id, player.TileLeft, player.TileTop, player.Team.TeamName));
        }

        dataWriter.WriteEndList();

        dataWriter.WriteLine();
        dataWriter.WriteStartList("Moves");
        dataWriter.WriteComment("time in milliseconds, Move, player, x, y");
        dataWriter.WriteComment("time in milliseconds, Eat, predator, prey");
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
      logger.Debug("Shutting down Flatland");

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

      descriptionText.Free();
      teamNameText.Free();
      smallScoreText.Free();
      bigScoreText.Free();
      communalScoreText.Free();
    }

    protected override void OnResize (EventArgs e)
    {
      // Setup 2-D viewing mode with a viewport the size of the window
      GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
      GL.MatrixMode(MatrixMode.Projection);
      GL.LoadIdentity();
      GL.Ortho(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);

      GL.MatrixMode(MatrixMode.Modelview);
      GL.LoadIdentity();

      // Cancel any pending animations and re-calculate pixel and board sizes
      lock (animator)
      {
        animator.FinishAndClear();
      }

      CalculateSizesAndLoadFonts();

      // Re-position players on the correct tiles
      foreach (var player in players)
      {
        player.Left = (player.TileLeft * pixelSize);
        player.Top = (player.TileTop * pixelSize);
      }
    }

    protected override void OnUpdateFrame (OpenTK.FrameEventArgs e)
    {
      if (gameState != GameState.Running)
      {
        return;
      }

      lock (animator)
      {
        animator.Update(e.Time);
      }

      bool isTeamMoving = false;

      foreach (var team in Settings.Teams)
      {
        if (!team.IsMoving)
        {
          team.SecondsLeftForMove -= e.Time;

          if (team.SecondsLeftForMove <= 0)
          {
            team.SecondsLeftForMoving = PlayerMoveAnimationSeconds;
            team.SecondsLeftForMove = Math.Max(0, team.MoveSeconds + team.SecondsLeftForMove);
            isTeamMoving = true;
          }
        }
        else
        {
          team.SecondsLeftForMoving -= e.Time;
        }

      } // foreach team

      if (isTeamMoving)
      {
        MovePlayers();
      }

      lock (respawnQueue)
      {
        while (respawnQueue.Count > 0)
        {
          var player = respawnQueue.Dequeue();
          RespawnPlayer(player);

          ExecutePlayerMove(inputTimer.ElapsedMilliseconds, player,
              player.TileLeft, player.TileTop);

          lock (animator)
          {
            animator.PropertyTo(player, "Opacity", 1, 0.5);
          }

        } // while respawn queue is not empty

      } // lock respawnQueue

    } // OnUpdateFrame

    protected override void OnRenderFrame (OpenTK.FrameEventArgs e)
    {
      if (gameState == GameState.Paused)
      {
        RenderScoreBoard();
        SwapBuffers();
        return;
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
        descriptionText.Render(Settings.GameDescription);
        GL.PopMatrix();
      }

      // Draw entire board
      GL.PushMatrix();

      GL.Disable(EnableCap.Texture2D);
      GL.Translate(boardLeft, boardTop, 0);
      GL.Color3(Color.White);
      GL.Begin(BeginMode.Quads);

      GL.Vertex2(0, 0);
      GL.Vertex2(0, boardSize);
      GL.Vertex2(boardSize, boardSize);
      GL.Vertex2(boardSize, 0);

      GL.End();

      // Draw fixed tiles
      foreach (var tileCoords in Settings.FixedTiles)
      {
        var tileLeft = tileCoords.X * pixelSize;
        var tileTop = tileCoords.Y * pixelSize;

        GL.Color3(Themes.Black.Background);
        GL.Begin(BeginMode.Quads);

        GL.Vertex2(tileLeft, tileTop);
        GL.Vertex2(tileLeft, tileTop + pixelSize);
        GL.Vertex2(tileLeft + pixelSize, tileTop + pixelSize);
        GL.Vertex2(tileLeft + pixelSize, tileTop);

        GL.End();
      }

      // Draw checkerboard for N-Queens game
      if (Settings.GameType == FlatlandGameType.NQueens)
      {
        GL.Begin(BeginMode.Quads);
        GL.Color3(Color.LightGray);

        for (int tileLeft = 0; tileLeft < Settings.Tiles; tileLeft++)
        {
          for (int tileTop = 0; tileTop < Settings.Tiles; tileTop++)
          {
            if (((tileLeft % 2 == 1) && (tileTop % 2 == 0)) ||
                ((tileLeft % 2 == 0) && (tileTop % 2 == 1))) {


              double gridLeft = tileLeft * pixelSize,
                     gridTop = tileTop * pixelSize;

              GL.Vertex2(gridLeft, gridTop);
              GL.Vertex2(gridLeft, gridTop + pixelSize);
              GL.Vertex2(gridLeft + pixelSize, gridTop + pixelSize);
              GL.Vertex2(gridLeft + pixelSize, gridTop);
            }
          }
        }

        GL.End();

      } // if N-Queens game

      // Draw board lines
      GL.Color3(Color.Black);
      GL.LineWidth(2);

      for (int tileIndex = 0; tileIndex <= Settings.Tiles; tileIndex++)
      {
        GL.Begin(BeginMode.Lines);

        // Vertical line
        GL.Vertex2(tileIndex * pixelSize, 0);
        GL.Vertex2(tileIndex * pixelSize, boardSize);

        // Horizontal line
        GL.Vertex2(0, tileIndex * pixelSize);
        GL.Vertex2(boardSize, tileIndex * pixelSize);

        GL.End();
      }

      // Draw players
      foreach (var player in players)
      {
        // Draw texture
        GL.Color4(1.0, 1.0 - player.OverlayRed,
                  1.0 - player.OverlayRed, player.Opacity);

        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, playerTextureIds[player.Id]);
        GL.Begin(BeginMode.Quads);

        GL.TexCoord2(0, 0);
        GL.Vertex2(player.Left + textureOffset, player.Top + textureOffset);

        GL.TexCoord2(0, 1);
        GL.Vertex2(player.Left + textureOffset, player.Top + pixelSize - textureOffset);

        GL.TexCoord2(1, 1);
        GL.Vertex2(player.Left + pixelSize - textureOffset, player.Top + pixelSize - textureOffset);

        GL.TexCoord2(1, 0);
        GL.Vertex2(player.Left + pixelSize - textureOffset, player.Top + textureOffset);

        GL.End();
        GL.Disable(EnableCap.Texture2D);

        if (player.IsSpawning)
        {
          continue;
        }

        // Draw button box
        double buttonLeft = player.Left + pixelSize - buttonSize;
        double buttonTop = player.Top + pixelSize - buttonSize;

        GL.Color3(player.BoxBackColor);

        GL.Begin(BeginMode.Quads);
        GL.Vertex2(buttonLeft, buttonTop);
        GL.Vertex2(buttonLeft, buttonTop + buttonSize);
        GL.Vertex2(buttonLeft + buttonSize, buttonTop + buttonSize);
        GL.Vertex2(buttonLeft + buttonSize, buttonTop);

        GL.End();

        // Draw box outline
        GL.Color3(player.BoxBorderColor);
        GL.LineWidth(1);

        GL.Begin(BeginMode.LineLoop);
        GL.Vertex2(buttonLeft, buttonTop);
        GL.Vertex2(buttonLeft, buttonTop + buttonSize);
        GL.Vertex2(buttonLeft + buttonSize, buttonTop + buttonSize);
        GL.Vertex2(buttonLeft + buttonSize, buttonTop);

        GL.End();

        if (player.BoxHighlightOpacity > 0)
        {
          var opacityA = (int)(player.BoxHighlightOpacity * 255.0);

          GL.Color4(Color.FromArgb(opacityA, Color.White));
          GL.Begin(BeginMode.Quads);
          GL.Vertex2(buttonLeft, buttonTop);
          GL.Vertex2(buttonLeft, buttonTop + buttonSize);
          GL.Vertex2(buttonLeft + buttonSize, buttonTop + buttonSize);
          GL.Vertex2(buttonLeft + buttonSize, buttonTop);
          GL.End();
        }

        // Draw score
        if (player.Team.ScoringSystem == FlatlandScoringSystem.Selfish)
        {
          var scoreString = player.Score.ToString();
          var scoreSize = smallScoreText.Measure(scoreString);

          GL.PushMatrix();
          GL.Translate(buttonLeft + halfButtonSize - (scoreSize.Width / 2.0),
                       buttonTop + halfButtonSize - (scoreSize.Height / 2.0), 0);

          smallScoreText.Render(scoreString, player.Team.Theme.Foreground);
          GL.PopMatrix();
        }

        // Draw arrow
        if ((player.ArrowOpacity > 0) && (Settings.GameType != FlatlandGameType.OnePrey))
        {
          GL.PushMatrix();
          GL.Color4(player.ArrowColor.R, player.ArrowColor.G,
            player.ArrowColor.B, (byte)(255.0 * player.ArrowOpacity));

          GL.Translate(buttonLeft + halfButtonSize, buttonTop + halfButtonSize, 0);
          GL.Rotate(player.ArrowAngle, 0, 0, 1);

          GL.Enable(EnableCap.PolygonSmooth);
          GL.Begin(BeginMode.Triangles);

          GL.Vertex2(0, -halfButtonSize);
          GL.Vertex2(-halfButtonSize, 0);
          GL.Vertex2(halfButtonSize, 0);

          GL.End();
          GL.Disable(EnableCap.PolygonSmooth);
          GL.PopMatrix();
        }

        if (Settings.ShowTeamNames)
        {
          // Draw button box
          GL.Disable(EnableCap.Texture2D);
          GL.Color3(player.BoxBackColor);

          GL.Begin(BeginMode.Quads);
          GL.Vertex2(player.Left, buttonTop);
          GL.Vertex2(player.Left, buttonTop + buttonSize);
          GL.Vertex2(player.Left + buttonSize, buttonTop + buttonSize);
          GL.Vertex2(player.Left + buttonSize, buttonTop);

          GL.End();

          // Draw box outline
          GL.Color3(player.BoxBorderColor);
          GL.LineWidth(1);

          GL.Begin(BeginMode.LineLoop);
          GL.Vertex2(player.Left, buttonTop);
          GL.Vertex2(player.Left, buttonTop + buttonSize);
          GL.Vertex2(player.Left + buttonSize, buttonTop + buttonSize);
          GL.Vertex2(player.Left + buttonSize, buttonTop);

          GL.End();

          // Draw team name
          var teamNameSize = teamNameText.Measure(player.Team.TeamName);

          GL.PushMatrix();
          GL.Translate(player.Left + ((buttonSize - (double)teamNameSize.Width) / 2.0),
            buttonTop + ((buttonSize - (double)teamNameSize.Height) / 2.0), 0);

          teamNameText.Render(player.Team.TeamName, player.ArrowColor);
          GL.PopMatrix();
        }
      }

      GL.PopMatrix();

      // Draw timer boxes
      foreach (var team in Settings.Teams)
      {
        GL.PushMatrix();
        GL.Translate(timerBoxMargin, boardTop + timerBoxMargin +
            (team.TeamIndex * (timerBoxSize + timerBoxMargin)), 0);

        // Draw box background
        GL.Color3(team.Theme.Background);
        GL.Begin(BeginMode.Quads);

        GL.Vertex2(0, 0);
        GL.Vertex2(0, timerBoxSize);
        GL.Vertex2(timerBoxSize, timerBoxSize);
        GL.Vertex2(timerBoxSize, 0);

        GL.End();

        // Draw time left
        if (!team.IsMoving)
        {
          GL.Color3(team.Theme.Foreground);
          GL.Begin(BeginMode.Quads);

          double fillTop = timerBoxSize - (timerBoxSize * (team.SecondsLeftForMove / team.MoveSeconds));

          GL.Vertex2(0, fillTop);
          GL.Vertex2(0, timerBoxSize);
          GL.Vertex2(timerBoxSize, timerBoxSize);
          GL.Vertex2(timerBoxSize, fillTop);

          GL.End();
        }

        // Draw box border
        GL.Color3(team.Theme.Border);
        GL.LineWidth(2);
        GL.Begin(BeginMode.LineLoop);

        GL.Vertex2(0, 0);
        GL.Vertex2(0, timerBoxSize);
        GL.Vertex2(timerBoxSize, timerBoxSize);
        GL.Vertex2(timerBoxSize, 0);

        GL.End();
        GL.PopMatrix();

        // Draw score
        if (team.ScoringSystem == FlatlandScoringSystem.Communal)
        {
          var scoreString = team.Score.ToString();
          var scoreSize = communalScoreText.Measure(scoreString);

          GL.PushMatrix();
          GL.Translate(ClientRectangle.Width - timerBoxMargin - timerBoxSize,
                       boardTop + timerBoxMargin + (team.TeamIndex * (timerBoxSize + timerBoxMargin)), 0);

          // Fill score box
          GL.Color3(team.Theme.Background);
          GL.Begin(BeginMode.Quads);
          GL.Vertex2(0, 0);
          GL.Vertex2(0, timerBoxSize);
          GL.Vertex2(timerBoxSize, timerBoxSize);
          GL.Vertex2(timerBoxSize, 0);
          GL.End();

          // Draw box border
          GL.Color3(team.Theme.Border);
          GL.LineWidth(2);
          GL.Begin(BeginMode.LineLoop);
          GL.Vertex2(0, 0);
          GL.Vertex2(0, timerBoxSize);
          GL.Vertex2(timerBoxSize, timerBoxSize);
          GL.Vertex2(timerBoxSize, 0);
          GL.End();

          // Draw score text
          GL.Translate((timerBoxSize / 2.0) - (scoreSize.Width / 2.0),
                       (timerBoxSize / 2.0) - (scoreSize.Height / 2.0), 0);

          communalScoreText.Render(scoreString, team.Theme.Foreground);
          GL.PopMatrix();
        }
      }

      SwapBuffers();
    }

    protected void RenderScoreBoard()
    {
      double scoreBoardWidth = (this.Width * 0.95), scoreBoardHeight = (this.Height * 0.95);
      var scoreBoardLeft = (this.Width - scoreBoardWidth) / 2.0;
      var scoreBoardTop = (this.Height - scoreBoardHeight) / 2.0;

      var playerSize = scoreBoardHeight / 8.0;
      var slotOffset = playerSize * 0.1;
      int playersPerColumn = 6;
      double columnOffset = scoreBoardWidth / Math.Ceiling((double)Settings.Players / (double)playersPerColumn);

      int scoreRow = 0, scoreColumn = 0;
      var playerScoreOrder = players.OrderByDescending(p => p.Score).ToArray();

      GL.Clear(ClearBufferMask.ColorBufferBit);
      GL.LoadIdentity();

      // Score board background
      GL.Disable(EnableCap.Texture2D);
      GL.Color3(Themes.Blue.Background);
      GL.Begin(BeginMode.Quads);

      GL.Vertex2(scoreBoardLeft, scoreBoardTop);
      GL.Vertex2(scoreBoardLeft, this.Height - scoreBoardTop);
      GL.Vertex2(this.Width - scoreBoardLeft, this.Height - scoreBoardTop);
      GL.Vertex2(this.Width - scoreBoardLeft, scoreBoardTop);

      GL.End();

      // Score board border
      GL.Color3(Themes.Blue.Border);
      GL.LineWidth(2);
      GL.Begin(BeginMode.LineLoop);

      GL.Vertex2(scoreBoardLeft, scoreBoardTop);
      GL.Vertex2(scoreBoardLeft, this.Height - scoreBoardTop);
      GL.Vertex2(this.Width - scoreBoardLeft, this.Height - scoreBoardTop);
      GL.Vertex2(this.Width - scoreBoardLeft, scoreBoardTop);

      GL.End();

      // Position and draw players
      foreach (var player in playerScoreOrder)
      {
        var slotLeft = scoreBoardLeft + (scoreColumn * columnOffset);
        var slotTop = scoreBoardTop + (scoreRow * playerSize);

        var playerLeft = slotLeft + slotOffset;
        var playerTop = slotTop + (slotOffset * (scoreRow + 1));

        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, playerTextureIds[player.Id]);
        GL.Color3(Color.White);

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

        var scoreString = string.Format(": {0}", player.Score);
        var scoreSize = bigScoreText.Measure(scoreString);

        GL.PushMatrix();
        GL.Translate(Math.Floor(playerLeft + playerSize),
                     Math.Floor(playerTop + ((playerSize - scoreSize.Height) / 2.0)), 0);

        bigScoreText.Render(scoreString, Themes.Blue.Foreground);
        GL.PopMatrix();

        scoreRow++;

        if (scoreRow > playersPerColumn)
        {
          scoreRow = 0;
          scoreColumn++;
        }
      }
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
            foreach (var player in players)
            {
              ProcessPlayerButton(player, rand.Next(FlatlandSettings.PlayerButtonCount + 1));
            }
          }
          break;

        case Key.P:
          if (gameState == GameState.Running)
          {
            gameState = GameState.Paused;
          }
          else if (gameState == GameState.Paused)
          {
            gameState = GameState.Running;
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

        if ((playerId >= 0) && (playerId < players.Count))
        {
          ProcessPlayerButton(players[playerId], button);
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

    protected void ProcessPlayerButton(Player player, int button)
    {
      Monitor.Enter(player);

      // Ignore player commands if they're spawning
      if ((gameState != GameState.Running) || player.IsSpawning)
      {
        Monitor.Exit(player);
        return;
      }

      var newDirection = player.NextDirection;
      var newArrowAngle = player.ArrowAngle;

      switch (button)
      {
        case 1:
          newDirection = PlayerDirection.UpLeft;
          newArrowAngle = 315;
          break;

        case 2:
          newDirection = PlayerDirection.DownLeft;
          newArrowAngle = 225;
          break;

        case 3:
        case 8:
          newDirection = PlayerDirection.Up;
          newArrowAngle = 0;
          break;

        case 5:
          newDirection = PlayerDirection.Left;
          newArrowAngle = 270;
          break;

        case 6:
          newDirection = PlayerDirection.UpRight;
          newArrowAngle = 45;
          break;

        case 7:
          newDirection = PlayerDirection.DownRight;
          newArrowAngle = 135;
          break;

        case 4:
        case 9:
          newDirection = PlayerDirection.Down;
          newArrowAngle = 180;
          break;

        case 10:
          newDirection = PlayerDirection.Right;
          newArrowAngle = 90;
          break;

      } // switch (button)

      if (newDirection != player.NextDirection)
      {
        player.NextDirection = newDirection;

        lock (animator)
        {
          if (Settings.GameType == FlatlandGameType.OnePrey)
          {
            if ((player.BoxHighlightOpacity == 0) && (player.OverlayRed == 0)) {

              // Acknowledge movement
              animator.PropertyTo(player, "BoxHighlightOpacity", 1, 0.25, 1, 0, null);
              animator.PropertyTo(player, "OverlayRed", 1, 0.25, 1, 0, null);
            }
          }
          else
          {
            // Show arrow
            if (player.ArrowOpacity < 1)
            {
              if (player.Team.ScoringSystem == FlatlandScoringSystem.Selfish)
              {
                animator.PropertyTo(player, "ArrowOpacity", 0.75, 0.25);
              }
              else
              {
                animator.PropertyTo(player, "ArrowOpacity", 1, 0.25);
              }
            }

            animator.PropertyTo(player, "ArrowAngle", newArrowAngle, 0.5);
          }

        } // lock animator

      } // if direction changed

      Monitor.Exit(player);

    } // ProcessPlayerButton

    #endregion

    #region Initial State and Player Sort Methods

    protected void SetupPlayerSort()
    {
      if (Settings.PlayerSort == PlayerSort.Random)
      {
        foreach (var player in players)
        {
          RespawnPlayer(player);
        }
      }

      // Assign board coordinates
      foreach (var player in players)
      {
        player.Left = player.TileLeft * pixelSize;
        player.Top = player.TileTop * pixelSize;
      }

      // Do initial NQueens check
      if (Settings.GameType == FlatlandGameType.NQueens)
      {
        NQueensCheckBoard();
      }
    }

    #endregion

    #region Player Movement Methods

    protected void MovePlayers()
    {
      var timeStamp = inputTimer.ElapsedMilliseconds;

      // Lock all players
      foreach (var player in players)
      {
        Monitor.Enter(player);
      }

      if (Settings.CollisionBehavior == CollisionBehavior.Allow)
      {
        // Don't bother checking for collisions between players
        foreach (var player in players)
        {
          if ((player.NextDirection != PlayerDirection.None) &&
              player.Team.IsMoving)
          {
            int newTileLeft = 0, newTileTop = 0;
            GetNextPlayerTile(player, player.NextDirection, out newTileLeft, out newTileTop);

            // Don't allow players to move onto fixed tiles
            if (!Settings.FixedTiles.Contains(new FixedTile(newTileLeft, newTileTop)))
            {
              ExecutePlayerMove(timeStamp, player, newTileLeft, newTileTop);
            }

            HidePlayerArrow(player);

          } // if player is moving

        } // foreach player

        // Check for predator/prey collisions
        foreach (var player in players)
        {
          if (!player.IsPrey)
          {
            continue;
          }

          var predatorPlayer = players.FirstOrDefault(p => p.IsPredator &&
              (p.TileLeft == player.TileLeft) &&
              (p.TileTop == player.TileTop));


          if (predatorPlayer != null)
          {
            CatchPrey(predatorPlayer, player);
          }

        } // foreach player

      } // if allow collisions
      else if (Settings.CollisionBehavior == CollisionBehavior.Block)
      {
        var movingPlayers = players.Where(p => p.Team.IsMoving).ToList();

        foreach (var player in movingPlayers)
        {
          if (!IsPlayerMoving(player))
          {
            player.NextDirection = PlayerDirection.None;
          }
        }

        // Skip conflict checking if no one is moving
        if (movingPlayers.All(p => (p.NextDirection == PlayerDirection.None)))
        {
          foreach (var player in movingPlayers)
          {
            HidePlayerArrow(player);
          }

          return;
        }

        // Discover conflicts
        var playersOnTile = new Dictionary<Point, List<Player>>();

        foreach (var player in players.OrderBy(p => rand.Next()))
        {
          // Skip players that are spawning
          if (player.IsSpawning)
          {
            continue;
          }

          var nextTile = player.Team.IsMoving?
            GetNextPlayerTile(player, player.NextDirection) :
            new Point(player.TileLeft, player.TileTop);

          if (!playersOnTile.ContainsKey(nextTile))
          {
            playersOnTile[nextTile] = new List<Player>();
          }

          playersOnTile[nextTile].Add(player);
        }

        // Resolve conflicts
        var conflictingTiles = playersOnTile.Where(tileAndPlayers => tileAndPlayers.Value.Count > 1)
          .Select(tileAndPlayers => tileAndPlayers.Key)
          .ToArray();

        while (conflictingTiles.Length > 0)
        {
          foreach (var currentTile in conflictingTiles)
          {
            var tilePlayers = playersOnTile[currentTile];

            if (tilePlayers.Count < 2)
            {
              // Tile is no longer conflicting
              continue;
            }

            // By default, the tile goes to the original owner
            var tileOwner = tilePlayers.FirstOrDefault(p =>
                (p.TileLeft == currentTile.X) && (p.TileTop == currentTile.Y));

            var winningPlayer = tileOwner;

            // Check if the tile owner is going to be eaten
            if ((tileOwner != null) && tileOwner.IsPrey &&
                tilePlayers.Any(p => p.IsPredator))
            {
              // Tile goes to a random predator
              winningPlayer = tilePlayers.First(p => p.IsPredator);
              CatchPrey(winningPlayer, tileOwner);

              // Remove the prey here, so they are not moved back to a
              // non-existent tile.
              tilePlayers.Remove(tileOwner);
            }

            if (winningPlayer == null)
            {
              // Tile goes to a random player
              winningPlayer = tilePlayers.First();
            }

            // Move all non-winning players back to their original tiles
            foreach (var nonWinningPlayer in tilePlayers)
            {
              if (nonWinningPlayer.Id == winningPlayer.Id)
              {
                continue;
              }

              var oldTile = new Point(nonWinningPlayer.TileLeft,
                  nonWinningPlayer.TileTop);

              if (oldTile == currentTile)
              {
                logger.DebugFormat("{0}", oldTile);
                continue;
              }

              if (!playersOnTile.ContainsKey(oldTile))
              {
                playersOnTile[oldTile] = new List<Player>();
              }

              playersOnTile[oldTile].Add(nonWinningPlayer);
            }

            tilePlayers.RemoveAll(p => p.Id != winningPlayer.Id);

          } // if tile has a conflict

          // Refresh list of conflicting tiles
          conflictingTiles = playersOnTile.Where(tileAndPlayers => tileAndPlayers.Value.Count > 1)
            .Select(tileAndPlayers => tileAndPlayers.Key)
            .ToArray();

        } // while conflicts exist


        // Executing non-conflicting moves
        foreach (var tileAndPlayers in playersOnTile)
        {
          var playerToMove = tileAndPlayers.Value.FirstOrDefault();
          var newTile = tileAndPlayers.Key;

          // Only move the player if their tile has changed
          if ((playerToMove != null) &&
              ((playerToMove.TileLeft != newTile.X) ||
               (playerToMove.TileTop != newTile.Y)))
          {
            ExecutePlayerMove(timeStamp, playerToMove,
                newTile.X, newTile.Y);
          }
        }

        // Reset directions
        foreach (var player in movingPlayers)
        {
          HidePlayerArrow(player);
          player.NextDirection = PlayerDirection.None;
        }

      } // if block collisions

      if (Settings.GameType == FlatlandGameType.NQueens)
      {
        NQueensCheckBoard();
      }

      // Unlock all players
      foreach (var player in players)
      {
        Monitor.Exit(player);
      }

    }  // MovePlayers

    protected void ExecutePlayerMove(long timeStamp, Player player, int newTileLeft, int newTileTop)
    {
      player.TileLeft = newTileLeft;
      player.TileTop = newTileTop;

      // Record movement
      if (dataWriter != null)
      {
        dataWriter.WriteText(string.Format("{0}, Move, {1}, {2}, {3}",
          timeStamp, player.Id, player.TileLeft, player.TileTop));
      }

      // Move to new location
      lock (animator)
      {
        animator.PropertyTo(player, "Left", player.TileLeft * pixelSize, PlayerMoveAnimationSeconds);
        animator.PropertyTo(player, "Top", player.TileTop * pixelSize, PlayerMoveAnimationSeconds);
      }

      player.NextDirection = PlayerDirection.None;
    }

    protected void GetNextPlayerTile(Player player, PlayerDirection direction,
                                     out int tileLeft, out int tileTop)
    {
      tileLeft = player.TileLeft;
      tileTop = player.TileTop;

      if (player.Team.WrappedMovement)
      {
        switch (direction)
        {
          case PlayerDirection.Up:
            tileTop = (player.TileTop == 0) ? Settings.Tiles - 1 : player.TileTop - 1;
            break;

          case PlayerDirection.UpRight:
            tileLeft = (player.TileLeft == (Settings.Tiles - 1)) ? 0 : player.TileLeft + 1;
            tileTop = (player.TileTop == 0) ? Settings.Tiles - 1 : player.TileTop - 1;
            break;

          case PlayerDirection.Right:
            tileLeft = (player.TileLeft == (Settings.Tiles - 1)) ? 0 : player.TileLeft + 1;
            break;

          case PlayerDirection.DownRight:
            tileLeft = (player.TileLeft == (Settings.Tiles - 1)) ? 0 : player.TileLeft + 1;
            tileTop = (player.TileTop == (Settings.Tiles - 1)) ? 0 : player.TileTop + 1;
            break;

          case PlayerDirection.Down:
            tileTop = (player.TileTop == (Settings.Tiles - 1)) ? 0 : player.TileTop + 1;
            break;

          case PlayerDirection.DownLeft:
            tileLeft = (player.TileLeft == 0) ? Settings.Tiles - 1 : player.TileLeft - 1;
            tileTop = (player.TileTop == (Settings.Tiles - 1)) ? 0 : player.TileTop + 1;
            break;

          case PlayerDirection.Left:
            tileLeft = (player.TileLeft == 0) ? Settings.Tiles - 1 : player.TileLeft - 1;
            break;

          case PlayerDirection.UpLeft:
            tileLeft = (player.TileLeft == 0) ? Settings.Tiles - 1 : player.TileLeft - 1;
            tileTop = (player.TileTop == 0) ? Settings.Tiles - 1 : player.TileTop - 1;
            break;
        }
      } // if wrap world
      else
      {
        switch (direction)
        {
          case PlayerDirection.Up:
            tileTop = Math.Max(0, player.TileTop - 1);
            break;

          case PlayerDirection.UpRight:
            tileLeft = Math.Min(Settings.Tiles - 1, player.TileLeft + 1);
            tileTop = Math.Max(0, player.TileTop - 1);
            break;

          case PlayerDirection.Right:
            tileLeft = Math.Min(Settings.Tiles - 1, player.TileLeft + 1);
            break;

          case PlayerDirection.DownRight:
            tileLeft = Math.Min(Settings.Tiles - 1, player.TileLeft + 1);
            tileTop = Math.Min(Settings.Tiles - 1, player.TileTop + 1);
            break;

          case PlayerDirection.Down:
            tileTop = Math.Min(Settings.Tiles - 1, player.TileTop + 1);
            break;

          case PlayerDirection.DownLeft:
            tileLeft = Math.Max(0, player.TileLeft - 1);
            tileTop = Math.Min(Settings.Tiles - 1, player.TileTop + 1);
            break;

          case PlayerDirection.Left:
            tileLeft = Math.Max(0, player.TileLeft - 1);
            break;

          case PlayerDirection.UpLeft:
            tileLeft = Math.Max(0, player.TileLeft - 1);
            tileTop = Math.Max(0, player.TileTop - 1);
            break;
        }
      }
    }

    protected int GetNextPlayerTileIndex(Player player, PlayerDirection direction)
    {
      int tileLeft = player.TileLeft, tileTop = player.TileTop;
      GetNextPlayerTile(player, direction, out tileLeft, out tileTop);

      return (ToTileIndex(tileLeft, tileTop));
    }

    protected Point GetNextPlayerTile(Player player, PlayerDirection direction)
    {
      int tileLeft = player.TileLeft, tileTop = player.TileTop;
      GetNextPlayerTile(player, direction, out tileLeft, out tileTop);

      return (new Point(tileLeft, tileTop));
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

      pixelSize = Math.Min(windowWidth, windowHeight) / (double)Settings.Tiles;
      textureOffset = (pixelSize - (pixelSize * 0.85f)) / 2.0f;
      buttonSize = pixelSize * 0.35f;
      halfButtonSize = buttonSize / 2.0f;
      boardSize = pixelSize * Settings.Tiles;
      boardLeft = ((double)windowWidth - boardSize) / 2.0f;
      boardTop = descriptionHeight + (((double)windowHeight - boardSize) / 2.0f);
      //timerBoxSize = Math.Max(boardLeft, boardTop) * 0.5f;
      timerBoxSize = boardLeft * 0.65f;
      timerBoxMargin = timerBoxSize * 0.1f;

      // Load fonts
      teamNameText = new TextPrinter(Brushes.White, new Font(FontFamily.GenericMonospace, (float)buttonSize * 0.9f, FontStyle.Bold));
      smallScoreText = new TextPrinter(Brushes.White, new Font(FontFamily.GenericMonospace, (float)buttonSize * 0.50f, FontStyle.Bold));
      bigScoreText = new TextPrinter(Brushes.White, new Font(FontFamily.GenericMonospace, (float)buttonSize * 0.75f, FontStyle.Bold));
      communalScoreText = new TextPrinter(Brushes.White, new Font(FontFamily.GenericMonospace, (float)timerBoxSize * 0.40f, FontStyle.Bold));
      descriptionText = new TextPrinter(Brushes.White, new Font(FontFamily.GenericSansSerif, (float)descriptionHeight * 0.45f, FontStyle.Bold));

      var descriptionSize = descriptionText.Measure(Settings.GameDescription);
      descriptionPoint.X = ((float)windowWidth - descriptionSize.Width) / 2.0f;
      descriptionPoint.Y = ((float)descriptionHeight - descriptionSize.Height) / 2.0f;
    }

    protected int ToTileIndex(int tileLeft, int tileTop)
    {
      return ((tileTop * Settings.Tiles) + tileLeft);
    }

    protected bool IsPlayerMoving(Player player)
    {
      if (player.NextDirection == PlayerDirection.None)
      {
        return (false);
      }

      int tileLeft = player.TileLeft, tileTop = player.TileTop;
      GetNextPlayerTile(player, player.NextDirection, out tileLeft, out tileTop);

      if (Settings.FixedTiles.Contains(new FixedTile(tileLeft, tileTop)))
      {
        return (false);
      }

      return (!((tileLeft == player.TileLeft) && (tileTop == player.TileTop)));
    }

    protected void HidePlayerArrow(Player player)
    {
      lock (animator)
      {
        animator.PropertyTo(player, "ArrowOpacity", 0, 0.35);
      }
    }

    protected string ToTileString(int tileIndex)
    {
      return (string.Format("{0}, {1}", tileIndex % Settings.Tiles, tileIndex / Settings.Tiles));
    }

    protected void RespawnPlayer(Player player)
    {
      var horizontalTiles = Enumerable.Range(0, Settings.Tiles).OrderBy(x => rand.Next()).ToArray();
      var verticalTiles = Enumerable.Range(0, Settings.Tiles).OrderBy(x => rand.Next()).ToArray();
      bool isConflicting = true;

      foreach (var tileLeft in horizontalTiles)
      {
        if (!isConflicting)
        {
          break;
        }

        foreach (var tileTop in verticalTiles)
        {
          player.TileLeft = tileLeft;
          player.TileTop = tileTop;

          var conflictingPlayers =
            players.Where(p => (p.Id != player.Id) &&
                          (p.TileLeft == player.TileLeft) &&
                          (p.TileTop == player.TileTop)).ToArray();

          if ((conflictingPlayers.Length == 0) &&
              !Settings.FixedTiles.Contains(new FixedTile(player.TileLeft, player.TileTop)))
          {
            isConflicting = false;
            break;
          }

          if ((Settings.CollisionBehavior == CollisionBehavior.Allow) &&
              ((player.IsPrey && !conflictingPlayers.Any(p => p.IsPredator)) ||
               (player.IsPredator && !conflictingPlayers.Any(p => p.IsPrey))))

          {
            isConflicting = false;
            break;
          }

        } // foreach vertical tile

      } // foreach horizontal tiles

      player.IsSpawning = false;

    } // RespawnPlayer

    protected void CatchPrey(Player predator, Player prey)
    {
      if (predator.Team.ScoringSystem == FlatlandScoringSystem.Communal)
      {
        predator.Team.Score++;

        // All players have the same score
        foreach (var player in players.Where(p => p.Team == predator.Team))
        {
          player.Score = predator.Team.Score;
        }
      }
      else
      {
        // Selfish
        predator.Score++;
      }

      prey.TileLeft = -1;
      prey.TileTop = -1;
      prey.SpawnTimeout = prey.Team.MoveSeconds;
      prey.IsSpawning = true;
      prey.Score--;

      // Record catch
      if (dataWriter != null)
      {
        dataWriter.WriteText
          (string.Format("{0}, Eat, {1}, {2}",
                         inputTimer.ElapsedMilliseconds, predator.Id, prey.Id));
      }

      lock (animator)
      {
        animator.PropertyTo(prey, "Opacity", 0, 0.5);
        animator.PropertyTo(prey, "SpawnTimeout", 0, prey.Team.MoveSeconds,
            0, 0, animator_PreyRespawn);
      }
    }

    protected void animator_PreyRespawn(DoubleAnimationInfo info)
    {
      var infoPlayer = (Player)info.Object;

      lock (respawnQueue)
      {
        respawnQueue.Enqueue(infoPlayer);
      }
    }

    protected void NQueensCheckBoard()
    {
      var conflictingPlayers = new HashSet<int>();

      foreach (var player1 in players)
      {
        foreach (var player2 in players)
        {
          if (player1.Id == player2.Id)
          {
            continue;
          }

          if ((player1.TileLeft == player2.TileLeft) ||
              (player1.TileTop == player2.TileTop))
          {
            conflictingPlayers.Add(player1.Id);
            conflictingPlayers.Add(player2.Id);
          }
          else
          {
            // Check diagonal conflicts
            foreach (var diagonalCoords in EnumDiagonalTiles(player1.TileLeft,
                                                             player1.TileTop))
            {
              if ((player2.TileLeft == diagonalCoords.X) &&
                  (player2.TileTop == diagonalCoords.Y))
              {
                conflictingPlayers.Add(player1.Id);
                conflictingPlayers.Add(player2.Id);
                break;
              }
            }
          }

        } // foreach player2

      } // foreach player1

      lock (animator)
      {
        foreach (var player in players)
        {
          if (conflictingPlayers.Contains(player.Id))
          {
            animator.PropertyTo(player, "OverlayRed", 1.0, 1);
          }
          else if (player.OverlayRed > 0)
          {
            animator.PropertyTo(player, "OverlayRed", 0, 1);
          }
        }
      }

    } // NQueensCheckBoard

    IEnumerable<Vector2> EnumDiagonalTiles(int tileLeft, int tileTop)
    {
      foreach (var leftOffset in new int[] { 1, -1 })
      {
        foreach (var topOffset in new int[] { 1, -1 })
        {
          int newLeft = tileLeft + leftOffset;
          int newTop = tileTop + topOffset;

          while ((newLeft >= 0) && (newLeft < Settings.Tiles) &&
                 (newTop >= 0) && (newTop < Settings.Tiles))
          {
            yield return (new Vector2(newLeft, newTop));

            newLeft += leftOffset;
            newTop += topOffset;
          }
        }
      }
    }

    #endregion
  }

  public enum GameState
  {
    Running, Paused
  }

}

