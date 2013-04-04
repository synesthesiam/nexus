using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Nexus.Shared;

namespace Nexus
{
	public class ForagerGameInfo : IGameInfo	
	{
		public int Plots { get; set; }
		public double TravelTime { get; set; }
		public int FoodRate { get; set; }
		public int GameSeconds { get; set; }
    public List<List<int>> PlotProbabilities { get; protected set; }
    public List<int> ProbabilityShiftTimes { get; protected set; }
		
		public ForagerGameInfo()
		{
			Plots = 8;
			TravelTime = 3;
			FoodRate = 1;
			GameSeconds = 120;
      PlotProbabilities = new List<List<int>>();
      ProbabilityShiftTimes = new List<int>();
		}
		
		public ForagerGameInfo(XElement element) : this()
		{
			Load(element);
		}
	
		#region IGameInfo Members
		
		public string GameName
		{
			get { return ("Forager"); }
		}

		public string GameDescription { get; set; }
		
		public IDictionary<string, string> GetArguments()
		{
			var arguments = new Dictionary<string, string>();
			
			arguments["plots"] = Plots.ToString();
			arguments["travel-time"] = TravelTime.ToString();
			arguments["food-rate"] = FoodRate.ToString();
			arguments["game-duration"] = GameSeconds.ToString();
      arguments["plot-probabilities"] = CLIObject.ToMultiArrayString(PlotProbabilities.Cast<IEnumerable<int>>());
      arguments["probability-shift-times"] = CLIObject.ToArrayString(ProbabilityShiftTimes);
			
			return (arguments);
		}
		
		public void Load(XElement element)
		{
			Plots = Convert.ToInt32(element.Element("plots").Value);
			TravelTime = Convert.ToDouble(element.Element("travelTime").Value);
			FoodRate = Convert.ToInt32(element.Element("foodRate").Value);
			GameSeconds = Convert.ToInt32(element.Element("gameSeconds").Value);

      PlotProbabilities.Clear();
      ProbabilityShiftTimes.Clear();

      var plotProbabilitiesElement = element.Element("plot-probabilities");

      if (plotProbabilitiesElement != null)
      {
        foreach (var probabilitySetElement in plotProbabilitiesElement.Elements("probability-set"))
        {
          PlotProbabilities.Add(CLIObject.FromArrayString<int>(probabilitySetElement.Value).ToList());
        }
      }
      
      var probabilityShiftTimesElement = element.Element("probability-shift-times");

      if (probabilityShiftTimesElement != null)
      {
        ProbabilityShiftTimes.AddRange(CLIObject.FromArrayString<int>(probabilityShiftTimesElement.Value));
      }
		}
		
		public void Save(XmlTextWriter writer)
		{
			writer.WriteElementString("plots", Plots.ToString());
			writer.WriteElementString("travelTime", TravelTime.ToString());
			writer.WriteElementString("foodRate", FoodRate.ToString());
			writer.WriteElementString("gameSeconds", GameSeconds.ToString());

      if (PlotProbabilities.Count > 0)
      {
        writer.WriteStartElement("plot-probabilities");

        foreach (var probabilitySet in PlotProbabilities)
        {
          writer.WriteElementString("probability-set", CLIObject.ToArrayString(probabilitySet));
        }

        // plot-probabilities
        writer.WriteEndElement();
      }

      if (ProbabilityShiftTimes.Count > 0)
      {
        writer.WriteElementString("probability-shift-times", CLIObject.ToArrayString(ProbabilityShiftTimes));
      }
		}
		
		#endregion
	}
}
