using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

//using Tao.Sdl;
//using WIICWrapper;

using Gtk;
using Gdk;
using Nexus;

public partial class MainWindow: Gtk.Window
{	
    private static string basePath = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
    protected log4net.ILog logger = log4net.LogManager.GetLogger("Nexus.MainWindow");

    protected List<string> playerIds = new List<string>();
    protected ListStore playerStore = new ListStore(typeof(string), typeof(string), typeof(Pixbuf));
    protected ListStore gameStore = new ListStore(typeof(bool), typeof(string), typeof(string), typeof(IGameInfo));
    protected bool isInGame = false;

    protected HITTSDK.BaseStation baseStation = new HITTSDK.BaseStation();

    //protected int wiimoteTimeout = 0, numWiimotesConnected = 0;
    //protected IDictionary<int, int> wiiButtonMap = new Dictionary<int, int>();
    //protected IDictionary<int, int> ps3ButtonMap = new Dictionary<int, int>();

    //protected bool running = true;
    //protected Thread sdlThread = null, wiimoteThread = null;
	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		
		// Set up controls
		spinButtonMaxPlayers.Adjustment.Upper = Images.GetPlayerImageCount();
		
		iconViewPlayers.Model = playerStore;
		iconViewPlayers.MarkupColumn = 1;
		iconViewPlayers.PixbufColumn = 2;
		iconViewPlayers.ModifyBase(StateType.Normal, new Gdk.Color(0, 0, 0));
		iconViewPlayers.ModifyText(StateType.Normal, new Gdk.Color(255, 255, 255));
		
		treeViewGames.Model = gameStore;
		treeViewGames.AppendColumn("Run", new CellRendererToggle(), "active", 0);
		treeViewGames.AppendColumn("Game", new CellRendererText(), "text", 1);
		treeViewGames.AppendColumn("Description", new CellRendererText(), "text", 2);
		
		RefreshSerialPortNames();
		
		HITTServer.Start();
		labelNetworkAddress.Markup = string.Format("<span weight=\"bold\" color=\"#333333\">IP Address</span>: {0}", HITTServer.Address);
		labelNetworkPort.Markup = string.Format("<span weight=\"bold\" color=\"#333333\">Port</span>: {0}", HITTServer.Port);
		baseStation.KeyReceived += baseStation_OnKeyReceived;

        //Wiimotes.ButtonClicked += wiimotes_OnButtonClicked;

        //ps3ButtonMap[(int)PS3Buttons.Up] = 3;
        //ps3ButtonMap[(int)PS3Buttons.Down] = 4;
        //ps3ButtonMap[(int)PS3Buttons.Left] = 5;
        //ps3ButtonMap[(int)PS3Buttons.Right] = 10;
        //sdlThread = new Thread(SDLMain);
        //sdlThread.Start();

		LoadSettings();
		
		logger.Debug("Initialization complete");

		if (Nexus.Settings.Default.HITTAutoConnect)
		{
			if (!ConnectToBaseStation())
			{
				notebook1.Page = 2;
			}
        }

        // Load manual
        using (var manualReader = new StreamReader(System.IO.Path.Combine(basePath, "etc/manual.md")))
        {
            Markdown.LoadIntoBuffer(textViewManual.Buffer,manualReader);
        }

        textViewManual.LeftMargin = 5;
        textViewManual.WrapMode = WrapMode.Word;
        textViewManual.Editable = false;
    }

	#region Main Window Event Handlers
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs e)
	{
		logger.Info("Shutting down");
		
		try
		{
			SaveSettings();
			
			if (baseStation.IsConnected)
			{
				baseStation.Disconnect();
			}
			
			HITTServer.Stop();

            // Wiimotes
            //if (wiimoteThread != null) {
                //Wiimotes.DisconnectAll();
                //wiimoteThread.Join();
            //}

            // Joysticks
            //running = false;

            //if (sdlThread != null) {
                //sdlThread.Join();
            //}
        }
        catch (Exception ex)
        {
            logger.WarnFormat("Cleanup error: {0}", ex.Message);
        }

        Application.Quit ();
        e.RetVal = true;
	}
	
	#endregion
	
	#region Player Button Event Handlers
	
	protected virtual void OnButtonClearPlayersClicked (object sender, System.EventArgs e)
	{
		var confirmDialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Question,
			ButtonsType.YesNo, false, "Really clear players?");
			
		if (confirmDialog.Run() == (int)ResponseType.Yes)
		{
			lock(playerIds)
			{
				playerIds.Clear();
			}
			
			playerStore.Clear();
		}
		
		confirmDialog.Destroy();
	}
	
	protected virtual void OnButtonRemovePlayersClicked (object sender, System.EventArgs e)
	{
		var selectedItems = iconViewPlayers.SelectedItems;
		
		foreach (var path in selectedItems)
		{
			TreeIter iter;
			playerStore.GetIter(out iter, path);
			
			lock(playerIds)
			{
				playerIds.Remove(Convert.ToString(playerStore.GetValue(iter, 0)));
			}
			
			playerStore.Remove(ref iter);
		}
	}
	
	protected virtual void OnButtonSavePlayersFileClicked (object sender, System.EventArgs e)
	{
		var saveDialog = new FileChooserDialog("Save Players File", this, FileChooserAction.Save,
			"Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
			
		SetUniqueFileName(saveDialog, Nexus.Settings.Default.PlayersFileDirectory, "players.txt");
		saveDialog.DoOverwriteConfirmation = true;
			
		if (saveDialog.Run() == (int)ResponseType.Accept)
		{
			using (var writer = new StreamWriter(saveDialog.Filename))
			{
				playerStore.Foreach(delegate(TreeModel model, TreePath path, TreeIter iter)
				{
					var playerId = Convert.ToString(playerStore.GetValue(iter, 0));
					var playerIndex = playerIds.IndexOf(playerId);
					
					writer.WriteLine(string.Format("{0} {1}", playerId, playerIndex));
					return (false);
				});
			}
			
			Nexus.Settings.Default.PlayersFileDirectory = System.IO.Path.GetDirectoryName(saveDialog.Filename);
		}
		
		saveDialog.Destroy();
	}

	protected virtual void OnButtonOpenPlayersFileClicked (object sender, System.EventArgs e)
	{
		var openDialog = new FileChooserDialog("Open Players File", this, FileChooserAction.Open,
			"Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
			
		openDialog.SetCurrentFolder(Nexus.Settings.Default.PlayersFileDirectory);
		
		try
		{
			if (openDialog.Run() == (int)ResponseType.Accept)
			{
				using (var reader = new StreamReader(openDialog.Filename))
				{
					var line = reader.ReadLine();
					
					while (line != null)
					{
						// Expecting each line to the player id, a space, and an image index
						var lineParts = line.Split(' ');
						var playerId = lineParts[0];
						var playerIndex = Convert.ToInt32(lineParts[1]);
						
						playerStore.AppendValues(playerId, GetPlayerString(playerId, 1),
							Images.GetPlayerImage(playerIndex));
						
						line = reader.ReadLine();
					}
				}
				
				Nexus.Settings.Default.PlayersFileDirectory = System.IO.Path.GetDirectoryName(openDialog.Filename);
			}
		}
		catch(Exception ex)
		{
			logger.Error("OnButtonOpenPlayersFileClicked", ex);
			ShowErrorDialog(ex);			
		}
		finally
		{
			openDialog.Destroy();
		}
	}
	
	#endregion
	
	#region Game Button Event Handlers
	
	protected virtual void OnButtonOpenGamesFileClicked (object sender, System.EventArgs e)
	{
		var openDialog = new FileChooserDialog("Open Games File", this, FileChooserAction.Open,
			"Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
			
		openDialog.SetCurrentFolder(Nexus.Settings.Default.GamesFileDirectory);
		
		try
		{
			if (openDialog.Run() == (int)ResponseType.Accept)
			{
				gameStore.Clear();
				
				// Parse games file
				var document = System.Xml.Linq.XDocument.Load(openDialog.Filename);
				
				foreach (var gameElement in document.Descendants("game"))
				{
					var gameName = ((string)gameElement.Attribute("name")).ToLower();
					IGameInfo gameInfo = null;
					
					switch (gameName)
					{
						case "flatland":
							gameInfo = new FlatlandGameInfo(gameElement);
							break;
							
						case "pixel":
							gameInfo = new PixelGameInfo(gameElement);
							break;

						case "group sum":
							gameInfo = new GroupSumGameInfo(gameElement);
							break;

                        case "forager":
                            gameInfo = new ForagerGameInfo(gameElement);
                            break;

                        default:
                            logger.WarnFormat("Unrecognized game \"{0}\"", gameName);
                            break;
                    }

                    if (gameInfo != null)
                    {
                        gameInfo.GameDescription = (string)gameElement.Attribute("description");
                        gameStore.AppendValues(false, gameInfo.GameName, gameInfo.GameDescription, gameInfo);
                    }
                }

                Nexus.Settings.Default.GamesFileDirectory = System.IO.Path.GetDirectoryName(openDialog.Filename);
            }
		}
		catch(Exception ex)
		{
			logger.Error("OnButtonOpenGamesFileClicked", ex);
			ShowErrorDialog(ex);			
		}
		finally
		{
			openDialog.Destroy();
		}
	}
	
	protected virtual void OnButtonNewGameClicked (object sender, System.EventArgs e)
    {
    	var newGameDialog = new DialogNewGame();
    	
    	if (newGameDialog.Run() == (int)ResponseType.Ok)
    	{
    		var gameInfo = newGameDialog.GetGameInfo();
    		
    		if (gameInfo != null)
    		{
    			var iter = gameStore.AppendValues(false, gameInfo.GameName,
    				gameInfo.GameDescription, gameInfo);
    				
    			treeViewGames.Selection.SelectIter(iter);
    		}
    	}
    	
    	newGameDialog.Destroy();
    }

	protected virtual void OnButtonEditGameClicked (object sender, System.EventArgs e)
  {
    TreeIter iter;
    
    // Check if a game is selected
    if (!treeViewGames.Selection.GetSelected(out iter))
    {
      return;
    }

    var gameInfo = (IGameInfo)gameStore.GetValue(iter, 3);		
    var editGameDialog = new DialogEditGame(gameInfo);
    
    if (editGameDialog.Run() == (int)ResponseType.Ok)
    {
      var newGameInfo = editGameDialog.GetGameInfo();
      gameStore.SetValues(iter, false, newGameInfo.GameName,
                          newGameInfo.GameDescription, newGameInfo);
    }

    editGameDialog.Destroy();
  }
	
	protected virtual void OnButtonPlayGameClicked (object sender, System.EventArgs e)
	{
		TreeIter iter;
		
		// Check if a game is selected
		if (!treeViewGames.Selection.GetSelected(out iter))
		{
			return;
		}
		
		// Verify players have joined
		var playerCount = playerStore.IterNChildren();
		
		if (playerCount < 1)
		{
			ShowErrorDialog("One or more players must join before starting a game");
			return;
		}
		
		// Have the user choose a location for the game's data file
		var gameInfo = (IGameInfo)gameStore.GetValue(iter, 3);
    string dataFileName = "";

    if (checkButtonSaveData.Active)
    {
      var saveDialog = new FileChooserDialog("Save Data File", this, FileChooserAction.Save,
                                             "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
		
      SetUniqueFileName(saveDialog, Nexus.Settings.Default.DataFileDirectory,
                        string.Format("{0}.txt", gameInfo.GameName));
			
      saveDialog.DoOverwriteConfirmation = true;
		
      var saveResponse = saveDialog.Run();
      dataFileName = saveDialog.Filename;
      saveDialog.Destroy();
		
      if (saveResponse != (int)ResponseType.Accept)
      {
        logger.Debug("Game canceled by user");
        return;
      }
		
      Nexus.Settings.Default.DataFileDirectory = System.IO.Path.GetDirectoryName(dataFileName);
    }
		
		try
		{
			// Prepare command-line arguments
			var arguments = gameInfo.GetArguments();
			
			// Automatically set default arguments
      if (!string.IsNullOrEmpty(dataFileName))
      {
        arguments["data-file"] = dataFileName;
      }

			arguments["port"] = HITTServer.Port.ToString();
			arguments["players"] = playerCount.ToString();
			arguments["description"] = gameInfo.GameDescription;
			
			if (radioButtonFullscreen.Active)
			{
				arguments["full-screen"] = "";
			}
			else
			{
				arguments["screen-width"] = spinButtonWindowWidth.ValueAsInt.ToString();
				arguments["screen-height"] = spinButtonWindowHeight.ValueAsInt.ToString();
			}

			if (checkbuttonDebugMode.Active)
			{
				arguments["debug"] = "";
			}
			
			// Execute external program			
			var gameAssemblyPath = System.IO.Path.Combine(basePath,
				string.Format("{0}.exe", gameInfo.GameName.Replace(" ", string.Empty)));
				
			var argumentArray = arguments.SelectMany(kv => new string[]
			{
				string.Format("--{0}", kv.Key), string.Format("\"{0}\"", kv.Value)
			}).ToArray();

			var argumentString = string.Join(" ", argumentArray);
			
			logger.DebugFormat("Running {0} from {1} with the arguments \"{2}\"",
					gameInfo.GameName, gameAssemblyPath, argumentString);

			// Run process externally
      isInGame = true;

			var proc = new Process();
      proc.StartInfo.FileName = gameAssemblyPath;
      proc.StartInfo.Arguments = argumentString;
      proc.StartInfo.UseShellExecute = false;
      proc.EnableRaisingEvents = true;

      proc.Exited += (de, ds) => {
        isInGame = false;
        logger.DebugFormat("{0} exited", gameInfo.GameName);
      };

      proc.Start();

			// Mark game as run
			gameStore.SetValue(iter, 0, true);
		}
		catch (Exception ex)
		{
			logger.Error("OnButtonPlayGameClicked", ex);
			ShowErrorDialog(ex);
		}
	}
	
	protected virtual void OnButtonClearGamesClicked (object sender, System.EventArgs e)
	{
		var confirmDialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Question,
			ButtonsType.YesNo, false, "Really clear games?");
			
		if (confirmDialog.Run() == (int)ResponseType.Yes)
		{
			gameStore.Clear();
		}
		
		confirmDialog.Destroy();
	}

	protected virtual void OnButtonRemoveGamesClicked (object sender, System.EventArgs e)
	{
		TreeIter iter;
		
		// Remove selected game
		if (treeViewGames.Selection.GetSelected(out iter))
		{
			gameStore.Remove(ref iter);
		}
	}

	protected virtual void OnButtonSaveGamesFileClicked (object sender, System.EventArgs e)
	{
		var saveDialog = new FileChooserDialog("Save Games File", this, FileChooserAction.Save,
			"Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
		
		SetUniqueFileName(saveDialog, Nexus.Settings.Default.GamesFileDirectory, "game_config.xml");
		saveDialog.DoOverwriteConfirmation = true;
		
		try
		{
			if (saveDialog.Run() == (int)ResponseType.Accept)
			{
				// Save games file
				using (var writer = new System.Xml.XmlTextWriter(saveDialog.Filename,
					System.Text.Encoding.UTF8))
				{			
					writer.Formatting = System.Xml.Formatting.Indented;					
					writer.WriteStartDocument();
					writer.WriteStartElement("games");
					
					gameStore.Foreach(delegate(TreeModel model, TreePath path, TreeIter iter)
					{
						// Save individual game info
						var gameInfo = (IGameInfo)model.GetValue(iter, 3);
						
						writer.WriteStartElement("game");
						writer.WriteAttributeString("name", gameInfo.GameName);
						writer.WriteAttributeString("description", gameInfo.GameDescription);
						
						gameInfo.Save(writer);
						
						writer.WriteEndElement();
						
						return (false);
					});				
					
					// games
					writer.WriteEndElement();
					writer.WriteEndDocument();
					
					Nexus.Settings.Default.GamesFileDirectory = System.IO.Path.GetDirectoryName(saveDialog.Filename);
				}
			}
		}
		catch(Exception ex)
		{
			logger.Error("OnButtonSaveGamesFileClicked", ex);
			ShowErrorDialog(ex);			
		}
		finally
		{
			saveDialog.Destroy();
		}
	}
	
	#endregion
	
	#region Settings Button Event Handlers
	
	protected virtual void OnButtonHITTConnectDisconnectClicked (object sender, System.EventArgs e)
	{
		try
		{
			if (baseStation.IsConnected)
			{
				// Disconnect from base station
				baseStation.Disconnect();
				buttonHITTConnectDisconnect.Label = "gtk-connect";
				comboBoxEntryHITTPortName.Sensitive = true;
				buttonHITTRefreshPortNames.Sensitive = true;
			}
			else
			{
				ConnectToBaseStation();
			}
		}
		catch (Exception ex)
		{
			logger.Error("OnButtonHITTConnectDisconnectClicked", ex);
			ShowErrorDialog(ex.Message);
		}
	}
	
	protected virtual void OnButtonHITTRefreshPortNamesClicked (object sender, System.EventArgs e)
	{
		RefreshSerialPortNames();
	}
	
	protected virtual void OnRadioButtonCustomResolutionToggled (object sender, System.EventArgs e)
	{
		spinButtonWindowWidth.Sensitive = radioButtonCustomWindowSize.Active;
		spinButtonWindowHeight.Sensitive = radioButtonCustomWindowSize.Active;
	}
	
  protected virtual void OnButtonNetworkSendClicked (object sender, System.EventArgs e)
  {
    HandlePlayerButton((spinButtonNetworkPlayer.ValueAsInt - 1).ToString(),
                       spinButtonNetworkButton.ValueAsInt);
  }

  protected virtual void OnButtonRevertSettingsClicked (object sender, System.EventArgs e)
  {
    LoadSettings();
  }

  protected virtual void OnButtonWiimotesDisconnectClicked (object sender, System.EventArgs e)
  {
    //try
    //{
      //if (wiimoteThread != null) {
        //Wiimotes.DisconnectAll();
        //wiimoteThread.Join();
      //}

      //numWiimotesConnected = 0;
      //spinButtonWiimotes.Sensitive = true;
      //buttonWiimotesSearch.Sensitive = true;
      //buttonWiimotesDisconnect.Sensitive = false;
      //labelWiimotesConnected.Markup = "<b>Connected</b>: 0";
    //}
    //catch (Exception ex)
    //{
			//logger.Error("OnButtonWiimotesDisconnectClicked", ex);
			//ShowErrorDialog(string.Format("Unable to disconnect Wiimotes. Please restart Nexus and try again: {0}", ex.Message));
    //}
  }

  protected virtual void OnButtonWiimotesSearchClicked (object sender, System.EventArgs e)
  {
    //try
    //{
      //wiimoteTimeout = spinButtonWiimotes.ValueAsInt;
      //spinButtonWiimotes.Sensitive = false;
      //buttonWiimotesSearch.Sensitive = false;
      //buttonWiimotesDisconnect.Sensitive = false;

      //wiimoteThread = new Thread(WiimoteMain);
      //wiimoteThread.Start();
    //}
    //catch (Exception ex)
    //{
      //spinButtonWiimotes.Sensitive = true;
      //buttonWiimotesSearch.Sensitive = true;
      //buttonWiimotesDisconnect.Sensitive = false;

			//logger.Error("OnButtonWiimotesSearchClicked", ex);
			//ShowErrorDialog(string.Format("Unable to search for Wiimotes. Please restart Nexus and try again: {0}", ex.Message));
    //}
  }

	#endregion
	
	#region Base Station Methods
	
	protected void baseStation_OnKeyReceived(object sender, HITTSDK.KeyReceivedEventArgs e)
	{
		HandlePlayerButton(e.RemoteId.ToString(), (int)e.Key);
	}

	protected bool ConnectToBaseStation()
	{
		try
		{
			// Connect to base station	
			baseStation.Connect(comboBoxEntryHITTPortName.Entry.Text);
			buttonHITTConnectDisconnect.Label = "gtk-disconnect";
			comboBoxEntryHITTPortName.Sensitive = false;
			buttonHITTRefreshPortNames.Sensitive = false;

			return (true);
		}
		catch (Exception ex)
		{
			logger.Error("ConnectToBaseStation", ex);
			ShowErrorDialog(string.Format("Unable to connect to base station. Please check your settings and try again: {0}", ex.Message));
		}

		return (false);
	}

	protected void HandlePlayerButton(string playerId, int button)
	{
		int playerIndex = -1;
		bool playerAdded = false;

		// Check if player is already registered
		lock(playerIds)
		{
			if (playerIds.Contains(playerId))
			{
				playerIndex = playerIds.IndexOf(playerId);
			}
			else
			{
				playerIds.Add(playerId);
				playerIndex = playerIds.Count - 1;
				playerAdded = true;
			}
		}
		
		if (playerAdded)
		{
			// Add player to store
			Application.Invoke(delegate
			{
				playerStore.AppendValues(playerId, GetPlayerString(playerId, button),
					Images.GetPlayerImage(playerIndex));
			});
		}
		else
		{
      // Send button press to clients
      HITTServer.SendToAll(new byte[] { (byte)playerIndex, (byte)button });
			
      if (!isInGame) {
        // Update players display
        Application.Invoke(delegate
        {
          TreeIter iter;
          
          if (playerStore.IterNthChild(out iter, playerIndex))
          {
            playerStore.SetValue(iter, 1, GetPlayerString(playerId, button));						
          }
        });
      }
		}
	}
	
	#endregion

  #region Wiimote Methods

  //protected void WiimoteMain()
  //{
    //numWiimotesConnected  = Wiimotes.Connect(wiimoteTimeout);

    //Gdk.Threads.Enter();
    //labelWiimotesConnected.Markup = string.Format("<b>Connected</b>: {0}", numWiimotesConnected);

    //if (numWiimotesConnected == 0)
    //{
      //spinButtonWiimotes.Sensitive = true;
      //buttonWiimotesSearch.Sensitive = true;
      //buttonWiimotesDisconnect.Sensitive = false;
    //}
    //else
    //{
      //buttonWiimotesDisconnect.Sensitive = true;
    //}

    //Gdk.Threads.Leave();

    //if (numWiimotesConnected > 0) {
      //Wiimotes.Poll();
    //}
  //}

  //protected void wiimotes_OnButtonClicked(object sender, WiiButtonEventArgs e)
  //{
    //if (wiiButtonMap.ContainsKey(e.Button))
    //{
      //HandlePlayerButton(e.Id.ToString(), wiiButtonMap[e.Button]);
    //}
  //}

  #endregion

  #region Joystick Methods

  //enum PS3Buttons {
    //Up = 4,
    //Down = 6,
    //Left = 7,
    //Right = 5
  //}

  //protected void SDLMain() {

    //Sdl.SDL_Init(Sdl.SDL_INIT_VIDEO | Sdl.SDL_INIT_JOYSTICK);

    //var numJoysticks = Sdl.SDL_NumJoysticks();

    //if (numJoysticks < 1) {
      //Sdl.SDL_Quit();
      //return;
    //}

    //logger.DebugFormat("Number of joysticks: {0}", numJoysticks);

    //for (int i = 0; i < numJoysticks; ++i) {
      //Sdl.SDL_JoystickOpen(i);
    //}

    //Sdl.SDL_Event e;

    //while (running && (Sdl.SDL_WaitEvent(out e) > 0)) {
      //switch(e.type) {
        //case Sdl.SDL_JOYBUTTONDOWN:

          //if (ps3ButtonMap.ContainsKey(e.jbutton.button)) {
            //HandlePlayerButton("J" + e.jbutton.which.ToString(), ps3ButtonMap[e.jbutton.button]);
          //}
          //break;
      //}
    //}

    //Sdl.SDL_Quit();		
  //}

  #endregion
	
	#region Utility Methods
	
	protected string GetPlayerString(string playerId, int button)
  {
      return (string.Format("<span size='x-large' weight='bold'><span color='green'>{0}</span> ({1})</span>",
        playerId, (HITTSDK.Keys)button));
  }
    
  protected void RefreshSerialPortNames()
  {
    // Clear strings from combo box
    while (comboBoxEntryHITTPortName.Model.IterNChildren() > 0)
    {
      comboBoxEntryHITTPortName.RemoveText(0);
    }

    // Gather serial port names
    var portNames = new List<string>(System.IO.Ports.SerialPort.GetPortNames());

    if (HITTSDK.Platform.IsOSX)
    {
      portNames.AddRange(Directory.GetFiles("/dev", "tty.*"));
    }

    foreach (var portName in portNames)
    {
      comboBoxEntryHITTPortName.AppendText(portName);
    }

    if (portNames.Count > 0)
    {
      comboBoxEntryHITTPortName.Active = 0;
    }
  }
    
    /// <summary>
    /// Loads the user's settings from the default settings file
    /// </summary>
    protected void LoadSettings()
    {
    	logger.Debug("Loading user settings");
    	
    	// HITT settings
    	comboBoxEntryHITTPortName.Entry.Text = Nexus.Settings.Default.HITTPortName;
    	checkButtonHITTAutoConnect.Active = Nexus.Settings.Default.HITTAutoConnect;
    	
    	// Graphics settings
    	radioButtonFullscreen.Active = Nexus.Settings.Default.GraphicsFullscreen;
    	spinButtonWindowWidth.Value = Nexus.Settings.Default.GraphicsScreenWidth;
    	spinButtonWindowHeight.Value = Nexus.Settings.Default.GraphicsScreenHeight;
    	
    	// Path settings
    	var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    	
    	if (string.IsNullOrEmpty(Nexus.Settings.Default.DataFileDirectory))
    	{
    		Nexus.Settings.Default.DataFileDirectory = myDocumentsPath;
    	}
    	
    	if (string.IsNullOrEmpty(Nexus.Settings.Default.GamesFileDirectory))
    	{
    		Nexus.Settings.Default.GamesFileDirectory = myDocumentsPath;
    	}
    	
    	if (string.IsNullOrEmpty(Nexus.Settings.Default.PlayersFileDirectory))
    	{
    		Nexus.Settings.Default.PlayersFileDirectory = myDocumentsPath;
    	}
    	
    	// Other settings
    	spinButtonMaxPlayers.Value = Nexus.Settings.Default.MaxPlayers;

      // Wii button map
      //var wiimoteMapPath = System.IO.Path.Combine(basePath, System.IO.Path.Combine("etc", "wiimote_map"));

      //if (File.Exists(wiimoteMapPath))
      //{
        //logger.Debug(string.Format("Loading Wiimote button map from {0}", wiimoteMapPath));

        //try
        //{
          //foreach (var line in File.ReadAllLines(wiimoteMapPath))
          //{
            //var match = Regex.Match(line, @"(\d+) (\d+)");

            //if (match.Success)
            //{
              //var mask = Convert.ToInt32(match.Groups[1].Value);
              //var button = Convert.ToInt32(match.Groups[2].Value);

              //wiiButtonMap[mask] = button;
            //}
          //}
        //}
        //catch (Exception ex)
        //{
          //logger.Error("Failed to load Wiimote button map", ex);
        //}
      //}

    } // LoadSettings
    
    /// <summary>
    /// Saves the user's settings to the default settings file
    /// </summary>
    protected void SaveSettings()
    {
    	logger.Debug("Saving user settings");
    	
    	// HITT settings
    	Nexus.Settings.Default.HITTPortName =  comboBoxEntryHITTPortName.Entry.Text;
    	Nexus.Settings.Default.HITTAutoConnect = checkButtonHITTAutoConnect.Active;
    	
    	// Graphics settings
    	Nexus.Settings.Default.GraphicsFullscreen = radioButtonFullscreen.Active;
    	Nexus.Settings.Default.GraphicsScreenWidth = spinButtonWindowWidth.ValueAsInt;
    	Nexus.Settings.Default.GraphicsScreenHeight = spinButtonWindowHeight.ValueAsInt;
    	
    	// Other settings
    	Nexus.Settings.Default.MaxPlayers = spinButtonMaxPlayers.ValueAsInt;
    	
    	// Persist to file
    	Nexus.Settings.Default.Save();
    }
    
    protected void SetUniqueFileName(FileChooserDialog dialog, string folderName, string fileName)
    {
    	var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
    	var fileNameExtension = System.IO.Path.GetExtension(fileName);
    	
    	var uniqueFileName = fileName;
    	uint appendNumber = 2;
    	
    	// Loop until a unique file name is found
    	while (System.IO.File.Exists(System.IO.Path.Combine(folderName, uniqueFileName)))
    	{
    		uniqueFileName = string.Format("{0}[{1}]{2}", fileNameWithoutExtension,
    			appendNumber, fileNameExtension);
    			
    		appendNumber++;
    	}
    	
    	dialog.SetCurrentFolder(folderName);
    	dialog.CurrentName = uniqueFileName;
    }
    
    protected void ShowErrorDialog(Exception ex)
    {
    	ShowErrorDialog(ex.Message);
    }
    
    protected void ShowErrorDialog(string text)
    {
    	var errorDialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error,
                                          ButtonsType.Ok, false, text);
			
      errorDialog.Title = "Nexus Error";			
      errorDialog.Run();
      errorDialog.Destroy();
    }

	#endregion
}
