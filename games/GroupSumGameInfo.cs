using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Nexus.Shared;

namespace Nexus
{
	public class GroupSumGameInfo : IGameInfo	
	{
		public int FirstRoundSeconds { get; set; }
		public int RoundSeconds { get; set; }
		public int RangeStart { get; set; }
		public int RangeEnd { get; set; }
		public bool ShowNumericFeedback { get; set; }
		public bool UsePreviousRoundInput { get; set; }
		
		public GroupSumGameInfo()
		{
			FirstRoundSeconds = 10;
			RoundSeconds = 10;
			RangeStart = 0;
			RangeEnd = 9;
			ShowNumericFeedback = false;
			UsePreviousRoundInput = false;
		}
		
		public GroupSumGameInfo(XElement element) : this()
		{
			Load(element);
		}
	
		#region IGameInfo Members
		
		public string GameName
		{
			get { return ("Group Sum"); }
		}

		public string GameDescription { get; set; }
		
		public IDictionary<string, string> GetArguments()
		{
			var arguments = new Dictionary<string, string>();

			arguments["first-round-seconds"] = FirstRoundSeconds.ToString();
			arguments["round-seconds"] = RoundSeconds.ToString();
			arguments["range-start"] = RangeStart.ToString();
			arguments["range-end"] = RangeEnd.ToString();

			if (ShowNumericFeedback)
			{
				arguments["numeric-feedback"] = string.Empty;
			}
			
			if (UsePreviousRoundInput)
			{
				arguments["previous-input"] = string.Empty;
			}
			
			return (arguments);
		}
		
		public void Load(XElement element)
		{
			RoundSeconds = Convert.ToInt32(element.Element("roundSeconds").Value);
			
			var firstRoundSecondsElement = element.Element("firstRoundSeconds");

			if (firstRoundSecondsElement != null)
			{			
				FirstRoundSeconds = Convert.ToInt32(firstRoundSecondsElement.Value);
			}
			else
			{
				FirstRoundSeconds = RoundSeconds;
			}

			var rangeStartElement = element.Element("rangeStart");

			if (rangeStartElement != null)
			{			
				RangeStart = Convert.ToInt32(rangeStartElement.Value);
			}
			else
			{
				RangeStart = 0;
			}

			var rangeEndElement = element.Element("rangeEnd");

			if (rangeEndElement != null)
			{			
				RangeEnd = Convert.ToInt32(rangeEndElement.Value);
			}
			else
			{
				RangeEnd = 9;
			}
			
			ShowNumericFeedback = Convert.ToBoolean(element.Element("showNumericFeedback").Value);
			UsePreviousRoundInput = Convert.ToBoolean(element.Element("usePreviousRoundInput").Value);
		}
		
		public void Save(XmlTextWriter writer)
		{
			writer.WriteElementString("firstRoundSeconds", FirstRoundSeconds.ToString());
			writer.WriteElementString("roundSeconds", RoundSeconds.ToString());
			writer.WriteElementString("showNumericFeedback", ShowNumericFeedback.ToString());
			writer.WriteElementString("usePreviousRoundInput", UsePreviousRoundInput.ToString());
			writer.WriteElementString("rangeStart", RangeStart.ToString());
			writer.WriteElementString("rangeEnd", RangeEnd.ToString());
		}
		
		#endregion
	}
}
