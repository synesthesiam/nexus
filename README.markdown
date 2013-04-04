# Nexus

Nexus is a general purpose host for several experimental games.  It handles communication with an H-ITT remote control base station, player avatar assignment, game configuration management, and general game settings. Wiimotes may also be used as controllers if a Bluetooth adapter is available.

## Games

* **Pixel** - a cooperative art program where players create a shared picture on a black and white grid.
* **Flatland** - a game played on a two-dimensional grid where player avatars move around and possibly interact.
* **Group Sum** - a collective binary search problem where players try to guess a random number as a group by summing their individual guesses.
* **Forager** - a game where players compete to acquire the most food before the game ends.

## Platform and Libraries

Nexus and all games are written in C# for the .NET platform.  Specifically, Nexus uses the Mono GTK# libraries for its user interface (http://mono-project.com/GtkSharp) and the games use OpenTK (http://www.opentk.com) for all graphical needs.  Other libraries include HITTSDK (a .NET wrapped for libh-ittsdk), Apache log4net for logging (http://logging.apache.org/log4net/index.html), NDesk.Options for command line parsing (http://www.ndesk.org/Options), and NPlot for Group Sum's graph rendering (http://netcontrols.org/nplot).

## Building

Nexus is meant to be build on a Unix-like platform, so Makefiles are used.  In addition to the master Makefile in the Nexus source directory, each game has its own Makefile.  A Visual Studio 2008 solution file is included, though it does not copy all necessary files to the output directory (see the `BUNDLED_FILES` macro in the Nexus Makefile).

The default target in the Nexus Makefile will build all games and the Nexus binary.  The "clean" target will delete the Nexus binary and the "mac" target will use Mono's macpack utility to generate a Mac-compatible .app bundle with everything inside.

## File Layout

Nexus
* DialogEditFixedTiles.cs -> Dialog used for Flatland settings (editing fixed tiles)
* DialogEditGame.cs -> Sets up DialogNewGame for editing mode (loads game configuration)
* DialogNewGame.cs -> Master dialog for all game settings
* etc
    * `food_images` -> Food images for Forager
    * icons.icns -> Nexus icon set for the Mac, used in the "mac" Makefile target
    * icon.svg -> Source file for the Nexus icon
    * manual.md -> Manual for Nexus and included games
    * `player_images` -> Player avatars, shared between all games
* Flatland
    * FlatlandSettings.cs -> Command-line settings for Flatland
    * FlatlandWindow.cs -> All game code for Flatland
    * Main.cs -> Command-line parsing and game start-up for Flatland
    * Makefile -> Build file for Flatland
    * Player.cs -> Player class for Flatland
* Forager
    * ForagerSettings.cs -> Command-line settings for Forager
    * ForagerWindow.cs -> All game code for Forager
    * Main.cs -> Command-line parsing and game start-up for Forager
    * Makefile -> Build file for Forager
    * Player.cs -> Player class for Forager
* games
    * FlatlandGameInfo.cs -> Settings reader/writer for Flatland
    * ForagerGameInfo.cs -> Settings reader/writer for Forager
    * GroupSumGameInfo.cs -> Settings reader/writer for Group Sum
    * IGameInfo.cs -> Interface for Nexus game settings
    * PixelGameInfo.cs -> Settings reader/writer for Pixel
* GroupSum
    * GroupSumPlotSurface.cs -> Setup/display code the Group Sum graph
    * GroupSumSettings.cs -> Command-line settings for Group Sum
    * GroupSumWindow.cs -> All game code for Group Sum
    * Main.cs -> Command-line parsing and game start-up for Group Sum
    * Makefile -> Build file for Group Sum
    * NPlot -> Slightly modified NPlot code (to support Group Sum's number labels)
* gtk-gui -> Auto-generated code from MonoDevelop (http://monodevelop.com) for the Nexus UI
* lib -> Library files that Nexus and the games need to run
* Main.cs -> Command-line parsing for Nexus
* MainWindow -> All UI code for the Nexus main window
* Makefile -> Master build file for Nexus
* Nexus.sln -> Visual Studio 2008 solution file for Nexus. NOTE: See the `BUNDLED_FILES` macro in Makefile for a list of files that Nexus needs to run.
* Pixel
    * ClusteredBoard.cs -> Code for the board layout algorithms
    * Main.cs -> Command-line parsing and game start-up for Pixel
    * Makefile -> Build file for Pixel
    * PixelSettings.cs -> Command-line settings for Pixel
    * PixelWindow.cs -> All game code for Pixel
    * PlayerPixel.cs -> Code for a player's pixel
* Settings.cs -> Nexus settings that persist between sessions
* shared
    * CLIObject.cs -> Command-Line Object class used to serialize settings to the games
    * DefaultGameSettings.cs -> Base class with a shared subset of game settings
    * DoubleAnimator.cs -> Linear interpolating property animator (used for animation mostly)
    * Textures.cs -> Utility methods for loading textures into OpenGL
    * Themes.cs -> RGB values for various color schemes
    * YAMLOutputStream.cs -> Writer for YAML game data files
* utility
    * DrawingAreaGrid.cs -> Fixed tile UI grid used for Pixel and Flatland settings
    * GTKUtility.cs -> Common utility methods for GTK
    * HITTServer.cs -> Network server that broadcasts remote control button presses to the games
    * Images.cs -> Utility methods to load player images
    * Markdown.cs -> Class to load the manual (etc/markdown.md) into a GTK TextBuffer and make it pretty
