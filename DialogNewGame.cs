using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using Gtk;

using Nexus.Shared;

namespace Nexus
{
  public partial class DialogNewGame : Gtk.Dialog
  {
    #region Fields
    
    // Flatland fields
    protected ListStore flatlandTeamStore = new ListStore(typeof(int), typeof(string), typeof(string), typeof(int), typeof(bool), typeof(string));
    protected IList<Point> flatlandFixedTiles = new List<Point>();

    // Forager fields
    protected ListStore foragerProbabilityStore = new ListStore(typeof(int), typeof(int));
    protected List<List<int>> foragerProbabilities = new List<List<int>>();
    protected List<int> foragerProbabilityShiftTimes = new List<int>();
    
    // Pixel fields
    protected DrawingAreaGrid pixelGrid = new DrawingAreaGrid();

    #endregion
    
    public DialogNewGame()
    {
      this.Build();

      // ====================
      // Pixel Initialization
      // ====================

      pixelGrid.CenterGridHorizontally = false;
      pixelGrid.HeightRequest = 150;
      tablePixel.Add(pixelGrid);

      var pixelGridChild = (Gtk.Table.TableChild)tablePixel[pixelGrid];
      pixelGridChild.LeftAttach = 1;
      pixelGridChild.RightAttach = 3;
      pixelGridChild.TopAttach = 3;
      pixelGridChild.BottomAttach = 4;
      
      pixelGrid.Tiles = 12;
      pixelGrid.Show();
      
      // =======================
      // Flatland Initialization
      // =======================

      treeViewFlatlandTeams.Model = flatlandTeamStore;
      treeViewFlatlandTeams.AppendColumn("Team", new CellRendererText(),
              delegate(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
        {
          var teamIndex = (int)model.GetValue(iter, 0);
          ((CellRendererText)cell).Text = ((char)(((int)'A') + teamIndex)).ToString();
        }
      );

      // Set up "Color" column
      var themeModel = new ListStore(typeof(string));

      foreach (var colorName in Enum.GetNames(typeof(ThemeColor)).OrderBy(x => x))
      {
        themeModel.AppendValues(colorName);
      }
      
      var colorColumnRenderer = new CellRendererCombo()
      {
        Model = themeModel,
        TextColumn = 0,
        Editable = true,
        HasEntry = false,
      };
      
      colorColumnRenderer.Edited += delegate(object ds, EditedArgs de)
      {
        TreeIter iter;
        
        if (flatlandTeamStore.GetIterFromString(out iter, de.Path))
        {
          flatlandTeamStore.SetValue(iter, 1, de.NewText);
        }
      };

      treeViewFlatlandTeams.AppendColumn("Color", colorColumnRenderer, "text", 1);
      treeViewFlatlandTeams.Columns[1].MinWidth = 100;

      // Set up "Kind" column
      var kindModel = new ListStore(typeof(string));
      kindModel.AppendValues("Normal");
      kindModel.AppendValues("Predator");
      kindModel.AppendValues("Prey");

      var kindColumnRenderer = new CellRendererCombo()
      {
        Model = kindModel,
        TextColumn = 0,
        Editable = true,
        HasEntry = false
      };

      kindColumnRenderer.Edited += delegate(object ds, EditedArgs de)
      {
        TreeIter iter;
        
        if (flatlandTeamStore.GetIterFromString(out iter, de.Path))
        {
          flatlandTeamStore.SetValue(iter, 2, de.NewText);
        }
      };

      treeViewFlatlandTeams.AppendColumn("Kind", kindColumnRenderer, "text", 2);
      treeViewFlatlandTeams.Columns[2].MinWidth = 100;

      // Set up "Move Delay" column
      var moveDelayColumnRenderer = new CellRendererSpin();
      moveDelayColumnRenderer.Editable = true;
      moveDelayColumnRenderer.Adjustment = new Adjustment(3, 1, 9999, 1, 5, 1);
      moveDelayColumnRenderer.Edited += delegate(object ds, EditedArgs de)
      {
        TreeIter iter;
        
        if (flatlandTeamStore.GetIterFromString(out iter, de.Path))
        {
          flatlandTeamStore.SetValue(iter, 3, Convert.ToInt32(de.NewText));
        }
      };

      treeViewFlatlandTeams.AppendColumn("Move Delay", moveDelayColumnRenderer, "text", 3);

      // Set up "Wrap" column
      var wrapColumnRenderer = new CellRendererToggle()
      {
        Activatable = true
      };

      wrapColumnRenderer.Toggled += delegate(object ds, ToggledArgs de)
      {
        TreeIter iter;
        
        if (flatlandTeamStore.GetIterFromString(out iter, de.Path))
        {
          flatlandTeamStore.SetValue(iter, 4, !Convert.ToBoolean(flatlandTeamStore.GetValue(iter, 4)));
        }
      };

      treeViewFlatlandTeams.AppendColumn("Wrap", wrapColumnRenderer, "active", 4);

      // Set up "Scoring" column
      var scoringModel = new ListStore(typeof(string));
      scoringModel.AppendValues("None");
      scoringModel.AppendValues("Selfish");
      scoringModel.AppendValues("Communal");

      var scoringColumnRenderer = new CellRendererCombo()
      {
        Model = scoringModel,
        TextColumn = 0,
        Editable = true,
        HasEntry = false
      };

      scoringColumnRenderer.Edited += delegate(object ds, EditedArgs de)
      {
        TreeIter iter;
        
        if (flatlandTeamStore.GetIterFromString(out iter, de.Path))
        {
          flatlandTeamStore.SetValue(iter, 5, de.NewText);
        }
      };

      treeViewFlatlandTeams.AppendColumn("Scoring", scoringColumnRenderer, "text", 5);
      treeViewFlatlandTeams.Columns[2].MinWidth = 100;

      // Add default team
      flatlandTeamStore.AppendValues(0, "Black", "Normal", 3, false, "None");

      // ======================
      // Forager Initialization
      // ======================

      treeViewForagerProbabilities.Model = foragerProbabilityStore;
      treeViewForagerProbabilities.AppendColumn("Plot", new CellRendererText(), "text", 0);

      // Probability column
      var probabilityColumnRenderer = new CellRendererSpin();
      probabilityColumnRenderer.Editable = true;
      probabilityColumnRenderer.Adjustment = new Adjustment(1, 0, 100, 1, 5, 1);
      probabilityColumnRenderer.Edited += delegate(object ds, EditedArgs de)
      {
        TreeIter iter;
        
        if (foragerProbabilityStore.GetIterFromString(out iter, de.Path))
        {
          var setIndex = comboBoxForagerProbabilitySet.Active;
          var plotNumber = (int)foragerProbabilityStore.GetValue(iter, 0);
          var newProbability = Convert.ToInt32(de.NewText);
          
          foragerProbabilities[setIndex][plotNumber - 1] = newProbability;
          foragerProbabilityStore.SetValue(iter, 1, newProbability);
        }
      };
      
      treeViewForagerProbabilities.AppendColumn("Probability",
                                                 probabilityColumnRenderer, "text", 1);

      // Add default probability set
      foragerProbabilities.Add(GetDefaultForagerProbabilitySet());

      comboBoxForagerProbabilitySet.AppendText("1");
      comboBoxForagerProbabilitySet.Active = 0;
    }

    #region Public Methods
    
    public IGameInfo GetGameInfo()
    {
      IGameInfo gameInfo = null;
      
      switch (comboBoxGame.Active)
      {
        case 1:
          var pixelInfo = new PixelGameInfo()
          {
            MaxSize = spinButtonPixelMaxSize.ValueAsInt,
            InitialState = comboBoxPixelInitialState.ActiveText,
            PlayerSort = comboBoxPixelPlayerSort.ActiveText
          };

          foreach (var point in pixelGrid.EnabledPixels)
          {
            if ((point.X < pixelInfo.MaxSize) &&
              (point.Y < pixelInfo.MaxSize))
            {
              pixelInfo.FixedPixels.Add(point);
            }
          }
          
          gameInfo = pixelInfo;
          break;
          
        case 2:
          var flatlandInfo = new FlatlandGameInfo()
          {
            Tiles = checkBoxFlatlandCustomBoardSize.Active ?
              spinButtonFlatlandBoardSize.ValueAsInt : (int?)null,

            CollisionBehavior = comboBoxFlatlandCollisionBehavior.ActiveText,
            GameType = comboBoxFlatlandGameType.ActiveText
          };

          // Add teams
          flatlandTeamStore.Foreach(delegate(TreeModel model, TreePath path, TreeIter iter)
          {
            flatlandInfo.Teams.Add(new FlatlandTeamInfo()
            {
              TeamIndex = (int)model.GetValue(iter, 0),
              ThemeName = (string)model.GetValue(iter, 1),
              Kind = (string)model.GetValue(iter, 2),
              MoveSeconds = (int)model.GetValue(iter, 3),
              WrappedMovement = (bool)model.GetValue(iter, 4),
              ScoringSystem = (string)model.GetValue(iter, 5)
            });
            
            return (false);
          });

          flatlandInfo.FixedTiles.AddRange(flatlandFixedTiles);

          gameInfo = flatlandInfo;
          break;

        case 3:
          gameInfo = new GroupSumGameInfo()
          {
            FirstRoundSeconds = spinButtonGroupSumFirstRoundSeconds.ValueAsInt,
            RoundSeconds = spinButtonGroupSumRoundSeconds.ValueAsInt,
            RangeStart = spinButtonGroupSumRangeStart.ValueAsInt,
            RangeEnd = spinButtonGroupSumRangeEnd.ValueAsInt,
            ShowNumericFeedback = checkButtonGroupSumNumericFeedback.Active,
            UsePreviousRoundInput = checkbuttonGroupSumPreviousInput.Active
          };
          break;

        case 4:
          var foragerInfo = new ForagerGameInfo()
          {
            Plots = spinButtonForagerPlots.ValueAsInt,
            TravelTime = spinButtonForagerTravelTime.ValueAsInt,
            FoodRate = spinButtonForagerFoodRate.ValueAsInt,
            GameSeconds = spinButtonForagerGameMinutes.ValueAsInt * 60,
          };

          foreach (var plotProbabilities in foragerProbabilities)
          {
            foragerInfo.PlotProbabilities.Add(plotProbabilities.ToList());
          }

          foragerInfo.ProbabilityShiftTimes.AddRange(foragerProbabilityShiftTimes.ToList());

          gameInfo = foragerInfo;
          break;
      }
      
      if (gameInfo != null)
      {
        gameInfo.GameDescription = entryGameDescription.Text;
      }
      
      return (gameInfo);
    }
    
    #endregion
    
    #region Shared Event Handlers
    
    protected virtual void OnComboBoxGameChanged(object sender, System.EventArgs e)
    {
      noteBookGameSettings.CurrentPage = comboBoxGame.Active;
      entryGameDescription.Sensitive = buttonOk.Sensitive = (comboBoxGame.Active > 0);
    }

    #endregion

    #region Pixel Event Handlers

    protected virtual void OnSpinButtonPixelMaxSizeValueChanged(object sender, System.EventArgs e)
    {
      pixelGrid.Tiles = spinButtonPixelMaxSize.ValueAsInt;  
      pixelGrid.QueueDraw();
    } 

    #endregion

    #region Flatland Event Handlers

    protected virtual void OnCheckBoxFlatlandCustomBoardSizeToggled(object sender, System.EventArgs e)
    {
      spinButtonFlatlandBoardSize.Sensitive = checkBoxFlatlandCustomBoardSize.Active;
      buttonFlatlandEditFixedTiles.Sensitive = checkBoxFlatlandCustomBoardSize.Active;   
    }

    protected virtual void OnButtonFlatlandClearTeamsClicked(object sender, System.EventArgs e)
    {
      flatlandTeamStore.Clear();

      // Add default team
      flatlandTeamStore.AppendValues(0, "Black", "Normal", 3, false, "None");
    }

    protected virtual void OnButtonFlatlandRemoveTeamClicked(object sender, System.EventArgs e)
    {
      if (flatlandTeamStore.IterNChildren() < 2)
      {
        // Can't remove the last team
        return;
      }
      
      TreeIter iter;
      
      if (treeViewFlatlandTeams.Selection.GetSelected(out iter))
      {
        flatlandTeamStore.Remove(ref iter);

        // Go back through and re-index all the teams
        int teamIndex = 0;

        flatlandTeamStore.GetIterFirst(out iter);

        do
        {
          flatlandTeamStore.SetValue(iter, 0, teamIndex++);
        } while (flatlandTeamStore.IterNext(ref iter));
      }
    }

    protected virtual void OnButtonFlatlandAddTeamClicked(object sender, System.EventArgs e)
    {
      var usedThemeColors = new List<string>();

      flatlandTeamStore.Foreach(delegate(TreeModel model, TreePath path, TreeIter iter)
      {
        usedThemeColors.Add((string)model.GetValue(iter, 1));
        return (false);
      });


      var nextThemeColor = "Black";

      foreach (var themeColor in Enum.GetNames(typeof(ThemeColor)).OrderBy(x => x))
      {
        if (!usedThemeColors.Contains(themeColor))
        {
          nextThemeColor = themeColor;
          break;
        }
      }

      flatlandTeamStore.AppendValues(flatlandTeamStore.IterNChildren(),
          nextThemeColor, "Normal", 3, false, "None");
    }

    protected virtual void buttonFlatlandEditFixedTiles_Clicked(object sender, System.EventArgs e)
    {
      var tileDialog = new DialogEditFixedTiles(spinButtonFlatlandBoardSize.ValueAsInt);
      tileDialog.Grid.SetPixels(flatlandFixedTiles);
      
      if (tileDialog.Run() == (int)ResponseType.Ok)
      {
        flatlandFixedTiles = new List<Point>(tileDialog.Grid.EnabledPixels);
      }

      tileDialog.Destroy();
    } 
    
    #endregion

    #region Forager Event Handlers

    protected virtual void OnSpinButtonForagerPlotsValueChanged(object sender, System.EventArgs e)
    {
      var plots = spinButtonForagerPlots.ValueAsInt;

      foreach (var plotProbabilities in foragerProbabilities)
      {
        // Remove the extraneous probabilities
        while (plotProbabilities.Count > plots)
        {
          plotProbabilities.RemoveAt(plotProbabilities.Count - 1);
        }

        while (plotProbabilities.Count < plots)
        {
          // Add the missing probabilities
          plotProbabilities.Add(1);
        }
      }

      CopyForagerProbabilitySetToStore();
    }

    protected List<int> GetDefaultForagerProbabilitySet()
    {
      return (Enumerable.Range(0, spinButtonForagerPlots.ValueAsInt).Select(i => 1).ToList());
    }

    protected void CopyForagerProbabilitySetToStore()
    {
      foragerProbabilityStore.Clear();

      int plotNumber = 1;
      foreach (var probability in foragerProbabilities[comboBoxForagerProbabilitySet.Active])
      {
        foragerProbabilityStore.AppendValues(plotNumber, probability);
        plotNumber++;
      }
    }

    protected virtual void OnButtonForagerAddProbabilitySetClicked(object sender, System.EventArgs e)
    {
      foragerProbabilities.Add(GetDefaultForagerProbabilitySet());
      foragerProbabilityShiftTimes.Add(60);

      var newSetIndex = foragerProbabilities.Count - 1;
      comboBoxForagerProbabilitySet.AppendText((newSetIndex + 1).ToString());
      comboBoxForagerProbabilitySet.Active = newSetIndex;
    }

    protected virtual void OnButtonForagerRemoveProbabilitySetClicked(object sender, System.EventArgs e)
    {
      var setIndex = comboBoxForagerProbabilitySet.Active;
      
      if (setIndex > 0)
      {
        comboBoxForagerProbabilitySet.Active = setIndex - 1;

        comboBoxForagerProbabilitySet.RemoveText(setIndex);
        foragerProbabilities.RemoveAt(setIndex);
        foragerProbabilityShiftTimes.RemoveAt(setIndex - 1);

        // Re-index existing sets
        int newIndex = 1;        
        comboBoxForagerProbabilitySet.Model.Foreach(delegate(TreeModel model, TreePath path, TreeIter iter)
        {
          model.SetValue(iter, 0, newIndex.ToString());
          newIndex++;

          return (false);
        });       
      }
    }

    protected virtual void OnComboBoxForagerProbabilitySetChanged(object sender, System.EventArgs e)
    {
      var setIndex = comboBoxForagerProbabilitySet.Active;
      CopyForagerProbabilitySetToStore();

      if (setIndex > 0)
      {
        spinButtonForagerProbabilityShiftMinutes.Sensitive = true;
        spinButtonForagerProbabilityShiftMinutes.Value =
          foragerProbabilityShiftTimes[setIndex - 1] / 60;
      }
      else
      {
        spinButtonForagerProbabilityShiftMinutes.Value = 1;
        spinButtonForagerProbabilityShiftMinutes.Sensitive = false;
      }
    }

    protected virtual void OnSpinButtonForagerProbabilityShiftMinutesValueChanged(object sender, System.EventArgs e)
    {
      foragerProbabilityShiftTimes[comboBoxForagerProbabilitySet.Active - 1] =
        spinButtonForagerProbabilityShiftMinutes.ValueAsInt * 60;
    } 

    #endregion

    #region Utility Methods

    protected void SetGameInfo(PixelGameInfo gameInfo)
    {
      comboBoxGame.Active = 1;
      entryGameDescription.Text = gameInfo.GameDescription;

      spinButtonPixelMaxSize.Value = gameInfo.MaxSize;
      GTKUtility.SetComboBoxValue(comboBoxPixelInitialState, gameInfo.InitialState);
      GTKUtility.SetComboBoxValue(comboBoxPixelPlayerSort, gameInfo.PlayerSort);

      pixelGrid.ClearPixels();
      pixelGrid.SetPixels(gameInfo.FixedPixels);
    }

    protected void SetGameInfo(FlatlandGameInfo gameInfo)
    {
      comboBoxGame.Active = 2;
      entryGameDescription.Text = gameInfo.GameDescription;

      if (gameInfo.Tiles.HasValue)
      {
        checkBoxFlatlandCustomBoardSize.Active = true;
        spinButtonFlatlandBoardSize.Value = gameInfo.Tiles.Value;
      }
      else
      {
        checkBoxFlatlandCustomBoardSize.Active = false;
      }
      
      GTKUtility.SetComboBoxValue(comboBoxFlatlandCollisionBehavior, gameInfo.CollisionBehavior);
      GTKUtility.SetComboBoxValue(comboBoxFlatlandGameType, gameInfo.GameType);

      flatlandTeamStore.Clear();
      foreach (var team in gameInfo.Teams)
      {
        flatlandTeamStore.AppendValues(team.TeamIndex, team.ThemeName,
            team.Kind, team.MoveSeconds, team.WrappedMovement, team.ScoringSystem);
      }

      flatlandFixedTiles = new List<Point>(gameInfo.FixedTiles); 
    }

    protected void SetGameInfo(GroupSumGameInfo gameInfo)
    {
      comboBoxGame.Active = 3;
      entryGameDescription.Text = gameInfo.GameDescription;

      spinButtonGroupSumFirstRoundSeconds.Value = gameInfo.FirstRoundSeconds;
      spinButtonGroupSumRoundSeconds.Value = gameInfo.RoundSeconds;
      spinButtonGroupSumRangeStart.Value = gameInfo.RangeStart;
      spinButtonGroupSumRangeEnd.Value = gameInfo.RangeEnd;
      checkButtonGroupSumNumericFeedback.Active = gameInfo.ShowNumericFeedback;
      checkbuttonGroupSumPreviousInput.Active = gameInfo.UsePreviousRoundInput;
    }

    protected void SetGameInfo(ForagerGameInfo gameInfo)
    {
      comboBoxGame.Active = 4;
      entryGameDescription.Text = gameInfo.GameDescription;

      spinButtonForagerPlots.Value = gameInfo.Plots;
      spinButtonForagerTravelTime.Value = gameInfo.TravelTime;
      spinButtonForagerFoodRate.Value = gameInfo.FoodRate;
      spinButtonForagerGameMinutes.Value = gameInfo.GameSeconds / 60;

      foragerProbabilityStore.Clear();
      foragerProbabilities.Clear();
      foragerProbabilityShiftTimes.Clear();

      int plotNumber = 1;

      foreach (var plotProbabilities in gameInfo.PlotProbabilities)
      {
        foragerProbabilities.Add(plotProbabilities.ToList());

        if (plotNumber > comboBoxForagerProbabilitySet.Model.IterNChildren())
        {
          comboBoxForagerProbabilitySet.AppendText(plotNumber.ToString());
        }

        plotNumber++;
      }

      foragerProbabilityShiftTimes.AddRange(gameInfo.ProbabilityShiftTimes.ToList());

      // Display first set of plot probabilities
      CopyForagerProbabilitySetToStore();
    }
    
    #endregion
  
  }
}
