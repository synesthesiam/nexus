using System;

using Nexus.Shared;

namespace GroupSum
{
	public class GroupSumSettings : DefaultGameSettings
	{
		public const int FileVersion = 1;

		public int Players { get; set; }
		public int MinNumber { get; set; }
		public int MaxNumber { get; set; }
		public int TargetNumber { get; set; }
		public int FirstRoundSeconds { get; set; }
		public int RoundSeconds { get; set; }
		public bool ShowNumericFeedback { get; set; }
		public bool UsePreviousRoundInput { get; set; }
	}
}
