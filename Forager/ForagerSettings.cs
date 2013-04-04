using System;
using System.Collections.Generic;

using Nexus.Shared;

namespace Forager
{
	public class ForagerSettings : DefaultGameSettings
	{
		public const int FileVersion = 1;

		public int Players { get; set; }
		public int Plots { get; set; }

		public double TravelTime { get; set; }
		public int FoodRate { get; set; }
		public List<List<double>> PlotProbabilities { get; set; }

		public int GameSeconds { get; set; }
		public IList<int> ProbabilityShiftTimes { get; set; }

    public ForagerSettings()
    {
      PlotProbabilities = new List<List<double>>();
      ProbabilityShiftTimes = new List<int>();
    }
	}
}
