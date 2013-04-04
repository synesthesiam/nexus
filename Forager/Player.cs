using System;

namespace Forager
{
	public class Player
	{
		public int Id { get; protected set; }
		
		public int Score { get; set; }
		public int PlotNumber { get; set; }
		public int Position { get; set; }
		
		public double Left { get; set; }
		public double Top { get; set; }
		public double Opacity { get; set; }

		public double TopOffset { get; set; }
		public double FoodOpacity { get; set; }
    public int FoodFound { get; set; }
		
		public double TravelTimeLeft { get; set; }
		public PlayerState State { get; set; }

		public int FoodTextureId { get; set; }

		public Player(int id)
		{
			this.Id = id;
			this.State = PlayerState.WaitingForInput;
			this.Opacity = 1;
			this.TopOffset = 0;
			this.FoodOpacity = 0;
			this.FoodTextureId = 0;
			this.Score = 0;
      this.FoodFound = 0;
		}
	}

	public enum PlayerState
	{
		WaitingForInput, ReadyToTravel, Traveling, FinishedTraveling
	}

	public class PlayerShadow
	{
		public int PlayerId { get; protected set; }
		public double Left { get; set; }
		public double Top { get; set; }
		public double Opacity { get; set; }
		public bool IsGray { get; set; }

		public PlayerShadow(int playerId)
		{
			this.PlayerId = playerId;
			this.Opacity = 0;
		}
	}
}
