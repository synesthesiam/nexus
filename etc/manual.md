# Manual

Nexus is a general purpose host for several experimental "games".  It handles communication with the H-ITT remote control base station, player avatar assignment, game configuration management, and general game settings.

### Players

[Max Players] - The maximum number of players that are allowed to join the session.  This can be changed at any time, though lowering it below the number of current players will have no effect (other than not allowing anyone else to join).

[Clear] - Remove all players who have joined.

[Remove] - Remove the highlighted players from the list.

[Save] - Save the current player list as a text file.  This contains the remote ID number and the player's avatar number, so players can retain the same avatars between Nexus sessions.

[Open] - Load a player list from a text file.  The file is expected to have a line entry for each player with the format "RemoteId AvatarIndex" for each line.  A line like "409221 7" would mean that the player with remote ID 409221 should have the avatar named "player07.png" in Nexus' "etc/player_images" folder.

### Games

[Run, Game, Description] - Contains a list of game configurations with a check-mark if the user has Run the game, the name of the Game itself, and the Description that the user gave the game configuration.

[Open] - Loads a game configuration XML file into Nexus.  This will erase any existing configurations the user has, so they should be saved beforehand!  The configuration file can contain a heterogeneous collection of games in it.

[New] - Creates a new game configuration.  The settings for individual games are described later in the manual.

[Edit] - Edits the highlighted game configuration.

[Play] - Runs the highlighted game.  If the [Require game data to be recorded] option is checked in Settings, the user will be prompted for data file location.  Data files are saved in the YAML format (http://www.yaml.org), chosen to be highly human-readable.  NOTE: Hitting the 'Q' key during any game will quit back to Nexus.

[Clear] - Removes all existing game configurations.

[Remove] - Removes the highlighted game configuration.

[Save] - Saves all game configurations to an XML file.  The exact contents of the file vary depending on which games you have configurations for, but it should be verbose enough for most people to understand and edit by hand if necessary.

### Settings

H-ITT Base Station
==================

[Port Name] - On Windows systems, this the name of the COM port that the H-ITT base station is attached to (i.e. COM3, COM4).  On Mac and Linux systems, this is the path to the actual serial port device, usually at /dev/tty.* on Macs and /dev/ttyS* on Linux.  A list of detected ports is available, or the user may manually enter the port.

[Automatically connect at start-up] - If checked, Nexus will automatically connect to the base station at start-up.

[Refresh] - Reload the list of available ports (can be used if the user forgot to plug in the base station before starting Nexus).

[Connect/Disconnect] - Connect to or disconnect from the base station.

Graphics
========

[Full screen] - Run games in full-screen mode with the desktop display settings.  NOTE: This is *required* on Macs.
[Custom window size] - Run games in a window with the given dimensions (in pixels)

Network
=======

This information is only for debugging purposes.  The [Port] listed is the TCP port that games must connect to locally to receive remote control button presses.

Testing
=======

[Enable debug mode for games] - If checked, the user may press the 'R' key at any time during games to issue random play commands to all the players.  This is useful when the user doesn't have a remote or base station handy and just want to demonstrate the game mechanics.

[Require game data to be recorded] - If checked, the user will be prompted to choose a data file before each game is played.  WARNING: If unchecked, no game data will be recorded!

[Send] - This will emulate a remote control button press from the given player.  Useful if the user wants to register extra players for a game or doesn't have a remote handy.

Overall
=======

[Revert] - Reloads the settings Nexus started up with.

## Pixel

Pixel is a cooperative art program where players create a shared picture on a black and white grid.

### Settings

[Description] - Text displayed at the top of the game window.  Usually used as instructions for what picture to draw.

[Max Size] - The maximum size the grid will reach, regardless of how many players are present.  If this is 12, for example, the largest grid will be 12x12 or have 144 pixels.

[Initial Pixel State] - Random (pixels are randomly on or off), On (all white), Off (all black).

[Player Sort] - Random (players are placed randomly), Clustered (players are given large contiguous blocks of the part), Diffuse (players avatars are kept as far apart as possible).

[Fixed Pixels] - Click on the pixels that should be reserved.  These will be filled in with blue during the game and no players will be placed on them.  Used mainly for the Traveling Salesperson Problem to represent the cities.  NOTE: Fixed pixels are only used when [Player Sort] is Random.

## Flatland

Flatland is a game played on a two-dimensional grid where player avatars move around and possibly interact.  Players choose a direction to move using the remote (1 = Up-Left, 2 = Down-Left, 3/8 = Up, 5 = Left, 6 = Up-Right, 7 = Down-Right, 4/9 = Down, 10 = Right) and, when the timer reaches zero, all pending movements are done.

If one team is marked as "Predator" and another as "Prey", then the game rules change slightly.  If [Collision Behavior] is set to Allow, Predators will consume the Prey whenever the two occupy the same tile (Prey are re-spawned to random, empty tile after the next round).  If [Collision Behavior] is set to Block, a Predator will consume a Prey only if the Predator attempts to move onto the same tile as the Prey and the Prey does not or cannot move away (i.e. the Prey is trapped!).  NOTE: If two or more Predators are going after the Prey, one of them will randomly be chosen as the winner.  Predators get +1 to their score for consuming a Prey.

Hitting the 'P' key during the game will pause and show the score for all players.

### Settings

[Description] - Text displayed at the top of the game window.

[Custom Board Size] - Force Flatland to use the given board size.  If this is 8, for example, the board will be 8x8 or have 64 tiles.  Flatland normally minimizes the board size for the number of players.

[Edit Fixed Tiles] - Click on the tiles that should be reserved.  These will be filled in with blue during the game and players will not be able to move onto them.

[Collision Behavior] - Allow (players may be on the same tile), Block (players may not be on the same tile)

[Game Type] - Normal (players have usual avatars, board is all white), NQueens (players have chess queen image - red when in conflict, board is checkered), OnePrey (one player is randomly choosen for prey team, rest are predators)

[Team, Color, Kind, Move Delay, Wrap, Scoring] - Players are randomly assigned a team when the game begins.  Teams will all have the same size if possible.  The user can add an arbitrary number of teams (though teams greater than the number of players are ignored).  Teams have the following characteristics: Team (A-Z), Color (Black, Blue, Green, Orange, Purple, Red, Yellow), Kind (Normal, Predator, Prey), Move Delay (Seconds before each move), Wrap (Grid world is toroidal for team), Scoring (Points are shared or not for predators).

[Clear] - Reset all teams back to the default (A, Black, Normal).

[Remove] - Removes the highlighted team and automatically adjusts the team names.  NOTE: The last team cannot be removed.

[Add] - Adds a new team to the end of the list.

## Group Sum

Group Sum is a collective binary search problem where players try to guess a random number as a group by summing their individual guesses.  After each round, the group sees their guess history on a graph and receives feedback in the form of "Too High/Low" or "High/Low by X" messages.  The game ends when the group correctly guesses the random number.

### Settings

[Description] - Only displayed in Nexus itself, not in-game.

[First Round Duration] - Number of seconds that the first round will last.

[Subsequent Round Duration] - Number of seconds that all rounds after the first round will last.

[Number Range] - Multipliers used to determine the range of the random number that Group Sum can pick.  If there were 5 players in the game, for example, and the user were to set the number range to 0 and 9, then Group Sum will pick a random number uniformly between (0 * 5) and (9 * 5) or 0 and 45.

[Show numeric feedback] - If checked, players will be given "High/Low by X" messages where "X" is the absolute distance from the summed group guess to the random number.  If unchecked, players are given the more cryptic "Too High/Low" messages.

[Use input from previous round] - If checked, players' guesses will be retained between rounds.  For example, if a player guessed "8" in the first round and then didn't press a button during the second round, their guess would still be set to "8".  If unchecked, player's guesses are reset to 0 after each round.

## Forager

Forager is a game where players compete to acquire the most food before the game ends.  The screen is divided into a specific number of "plots", where food is randomly disbursed with a hidden probability.  Players are free to move to plots that they believe to be better, which could mean less crowded or have a higher food rate, but they are penalized by a "travel time" when doing so.  Traveling players show up at the bottom of the screen and cannot receive food while in transit.  When the game ends, player scores are displayed with the winner in the upper-left corner.

### Settings

[Description] - Only displayed in Nexus itself, not in-game.

[Number of Plots] - Number of places where players may acquire food.  Plots will be automatically numbered (corresponding to the number of the remote controls).

[Travel Time] - Number of seconds that players are penalized for traveling between plots.

[Food Rate] - Number of food items that are given out per second.  Food is disbursed according to the plot probabilities.

[Game Duration] - Number of minutes that the game will last.

[Probability Set] - The user may specify an arbitrary number of "probability sets" for the plots.  These are simply a collection of relative probabilities that a food item will be given to a specific plot.  Over the course of the game, the user may have multiple probability sets that are quietly switched to in order to keep players on their toes.  Plot probabilities are relative, so plot one has a probability of "1" and plot 2 has a probability of "10", then plot 2 is 10 times more likely to receive food then plot 1.

[Add] - Add a new probability set.  Sets will be shifted to in numeric, ascending order.

[Remove] - Remove the selected probability set.

[Use this probability set after] - Number of minutes before shifting to the selected probability (after the previous probability set).

## Technical Documentation

### Platform and Libraries

Nexus and all games are written in C# for the .NET platform.  Specifically, Nexus uses the Mono GTK# libraries for its user interface (http://mono-project.com/GtkSharp) and the games use OpenTK (http://www.opentk.com) for all graphical needs.  Other libraries include HITTSDK (a .NET wrapped for libh-ittsdk), Apache log4net for logging (http://logging.apache.org/log4net/index.html), NDesk.Options for command line parsing (http://www.ndesk.org/Options), and NPlot for Group Sum's graph rendering (http://netcontrols.org/nplot).

### Building

Nexus is meant to be build on a Unix-like platform, so Makefiles are used.  In addition to the master Makefile in the Nexus source directory, each game has its own Makefile.  A Visual Studio 2008 solution file is included, though it does not copy all necessary files to the output directory (see the BUNDLED_FILES macro in the Nexus Makefile).

The default target in the Nexus Makefile will build all games and the Nexus binary.  The "clean" target will delete the Nexus binary and the "mac" target will use Mono's macpack utility to generate a Mac-compatible .app bundle with everything inside.

### File Layout

Nexus
|- DialogEditFixedTiles.cs -> Dialog used for Flatland settings (editing fixed tiles)
|- DialogEditGame.cs -> Sets up DialogNewGame for editing mode (loads game configuration)
|- DialogNewGame.cs -> Master dialog for all game settings
|- etc
   |- food_images -> Food images for Forager
   |- icons.icns -> Nexus icon set for the Mac, used in the "mac" Makefile target
   |- icon.svg -> Source file for the Nexus icon
   |- manual.md -> You're reading it!
   |- player_images -> Player avatars, shared between all games
|- Flatland
   |- FlatlandSettings.cs -> Command-line settings for Flatland
   |- FlatlandWindow.cs -> All game code for Flatland
   |- Main.cs -> Command-line parsing and game start-up for Flatland
   |- Makefile -> Build file for Flatland
   |- Player.cs -> Player class for Flatland
|- Forager
   |- ForagerSettings.cs -> Command-line settings for Forager
   |- ForagerWindow.cs -> All game code for Forager
   |- Main.cs -> Command-line parsing and game start-up for Forager
   |- Makefile -> Build file for Forager
   |- Player.cs -> Player class for Forager
|- games
   |- FlatlandGameInfo.cs -> Settings reader/writer for Flatland
   |- ForagerGameInfo.cs -> Settings reader/writer for Forager
   |- GroupSumGameInfo.cs -> Settings reader/writer for Group Sum
   |- IGameInfo.cs -> Interface for Nexus game settings
   |- PixelGameInfo.cs -> Settings reader/writer for Pixel
|- GroupSum
   |- GroupSumPlotSurface.cs -> Setup/display code the Group Sum graph
   |- GroupSumSettings.cs -> Command-line settings for Group Sum
   |- GroupSumWindow.cs -> All game code for Group Sum
   |- Main.cs -> Command-line parsing and game start-up for Group Sum
   |- Makefile -> Build file for Group Sum
   |- NPlot -> Slightly modified NPlot code (to support Group Sum's number labels)
|- gtk-gui -> Auto-generated code from MonoDevelop (http://monodevelop.com) for the Nexus UI
|- lib -> Library files that Nexus and the games need to run
|- Main.cs -> Command-line parsing for Nexus
|- MainWindow -> All UI code for the Nexus main window
|- Makefile -> Master build file for Nexus
|- Nexus.sln -> Visual Studio 2008 solution file for Nexus.  NOTE: See the BUNDLED_FILES macro in Makefile for a list of files that Nexus needs to run.
|- Pixel
   |- ClusteredBoard.cs -> Code for the board layout algorithms
   |- Main.cs -> Command-line parsing and game start-up for Pixel
   |- Makefile -> Build file for Pixel
   |- PixelSettings.cs -> Command-line settings for Pixel
   |- PixelWindow.cs -> All game code for Pixel
   |- PlayerPixel.cs -> Code for a player's pixel
|- Settings.cs -> Nexus settings that persist between sessions
|- shared
   |- CLIObject.cs -> Command-Line Object class used to serialize settings to the games
   |- DefaultGameSettings.cs -> Base class with a shared subset of game settings
   |- DoubleAnimator.cs -> Linear interpolating property animator (used for animation mostly)
   |- Textures.cs -> Utility methods for loading textures into OpenGL
   |- Themes.cs -> RGB values for various color schemes
   |- YAMLOutputStream.cs -> Writer for YAML game data files
|- utility
   |- DrawingAreaGrid.cs -> Fixed tile UI grid used for Pixel and Flatland settings
   |- GTKUtility.cs -> Common utility methods for GTK
   |- HITTServer.cs -> Network server that broadcasts remote control button presses to the games
   |- Images.cs -> Utility methods to load player images
   |- Markdown.cs -> Class to load the manual (etc/markdown.md) into a GTK TextBuffer and make it pretty
