using System;

namespace Pixel
{
	public class PlayerPixel
	{
		#region Properties
		
		public PixelState State { get; set; }
		public int PlayerId { get; set; }
		public int Button { get; set; }
		
		public int TileLeft { get; set; }
		public int TileTop { get; set; }
		
		public double Red { get; set; }
		public double Green { get; set; }
		public double Blue { get; set; }
		public double AllColors
		{
			get { return (Red); }
			set
			{
				Red = value;
				Green = value;
				Blue = value;
			}
		}
		
		#endregion
		
		public PlayerPixel(int tileLeft, int tileTop, PixelState state)
		{
			this.PlayerId = -1;
			this.TileLeft = tileLeft;
			this.TileTop = tileTop;
			this.State = state;
			
			switch (state)
			{
				case PixelState.On:
					AllColors = 1.0f;
					break;
					
				case PixelState.Off:
					AllColors = 0.0f;
					break;
			}
		}
		
		#region Public Methods
		
		public string GetButtonString()
		{
			string buttonString = "";
			
			switch (Button)
			{
				case 1:
					buttonString = "A";
					break;
					
				case 2:
					buttonString = "B";
					break;
					
				case 3:
					buttonString = "C";
					break;
					
				case 4:
					buttonString = "D";
					break;
					
				case 5:
					buttonString = "E";
					break;
					
				case 6:
					buttonString = "F";
					break;
					
				case 7:
					buttonString = "G";
					break;
					
				case 8:
					buttonString = "H";
					break;
					
				case 9:
					buttonString = "I";
					break;
					
				case 10:
					buttonString = "J";
					break;
			}
					
			return (buttonString);
		}
		
		#endregion
	}
	
	public enum PixelState
	{
		Off = 0, On = 1, Fixed = 2
	}
}
