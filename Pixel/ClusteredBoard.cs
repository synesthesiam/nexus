using System;

namespace Pixel
{
	public static class ClusteredBoard
	{
		private static log4net.ILog logger = log4net.LogManager.GetLogger("Pixel.ClusteredBoard");
		private static Random rand = new Random();
		
		public static byte[,] Create(int players, int tiles)
		{
			var board = new byte[tiles, tiles];

			for (int tileLeft = 0; tileLeft < tiles; tileLeft++)
			{
				for (int tileTop = 0; tileTop < tiles; tileTop++)
				{
					board[tileLeft, tileTop] = 255;
				}
			}

			int pixelCount = tiles * tiles;
			int playerId = rand.Next(players);
			int[] pixelsForPlayer = new int[players];
			
			for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
			{
				if (pixelsForPlayer[playerId] > 0)
				{
					// Remove all pixels from this player
					RemovePlayerPixels(ref board, (byte)playerId);
				}

				pixelsForPlayer[playerId]++;

				// Re-add all pixels for this player
				for (int playerPixelIndex = 0; playerPixelIndex < pixelsForPlayer[playerId]; playerPixelIndex++)
				{
					AssignBestClusteredPixel(ref board, (byte)playerId);
				}
				
				playerId = ++playerId % players;
			}

			// Find any stragglers (pixels that have all different neighbors)
			RemoveStragglers(ref board);

			return (board);
		}

		public static int GetStragglerCount(ref byte[,] board)
		{
			int stragglerCount = 0;

			for (int tileLeft = 0; tileLeft < board.GetLength(0); tileLeft++)
			{
				for (int tileTop = 0; tileTop < board.GetLength(1); tileTop++)
				{
					var playerId = board[tileLeft, tileTop];

					if (IsStraggler(ref board, tileLeft, tileTop, playerId))
					{
						stragglerCount++;
					}
				}
			}

			return (stragglerCount);
		}

		private static void RemovePlayerPixels(ref byte[,] board, byte playerId)
		{
			for (int tileLeft = 0; tileLeft < board.GetLength(0); tileLeft++)
			{
				for (int tileTop = 0; tileTop < board.GetLength(1); tileTop++)
				{
					if (board[tileLeft, tileTop] == playerId)
					{
						board[tileLeft, tileTop] = 255;
					}
				}
			}
		}

		private static void AssignBestClusteredPixel(ref byte[,] board, byte playerId)
		{
			var maxScore = double.MinValue;
			int bestTileLeft = -1, bestTileTop = -1;

			for (int tileLeft = 0; tileLeft < board.GetLength(0); tileLeft++)
			{
				for (int tileTop = 0; tileTop < board.GetLength(1); tileTop++)
				{
					if (board[tileLeft, tileTop] != 255)
					{
						continue;
					}
					
					// Calculate score from this pixel's point of view
					var score = GetClusterPixelScore(ref board, tileLeft, tileTop, playerId);

					if (score > maxScore)
					{
						maxScore = score;
						bestTileLeft = tileLeft;
						bestTileTop = tileTop;
					}
				}
			}

			if ((bestTileLeft < 0) || (bestTileTop < 0))
			{
				throw new Exception("No best pixel was found for clustering");
			}

			board[bestTileLeft, bestTileTop] = playerId;
		}

		private static double GetClusterPixelScore(ref byte[,] board, int freeTileLeft, int freeTileTop, byte playerId)
		{
			return (GetClusterPixelScore(ref board, freeTileLeft, freeTileTop, playerId, false));
		}

		private static double GetClusterPixelScore(ref byte[,] board, int freeTileLeft, int freeTileTop,
		                                           byte playerId, bool ignoreOtherPlayers)
		{
			double score = 0;
			
			for (int tileLeft = 0; tileLeft < board.GetLength(0); tileLeft++)
			{
				for (int tileTop = 0; tileTop < board.GetLength(1); tileTop++)
				{
					var checkPlayerId = board[tileLeft, tileTop];
					
					if ((checkPlayerId == 255) || ((tileLeft == freeTileLeft) && (tileTop == freeTileTop)))
					{
						continue;
					}
					
					double distanceSquared = Math.Pow(tileLeft - freeTileLeft, 2) +
						Math.Pow(tileTop - freeTileTop, 2);
		
					double mass = 0;
					
					if (checkPlayerId == playerId)
					{
						mass = 1;
					}
					else if (!ignoreOtherPlayers)
					{
						mass = -1;
					}
		
					score += mass / distanceSquared;
				}
			}

			return (score);
		}

		private static void RemoveStragglers(ref byte[,] board)
		{
			for (int tileLeft = 0; tileLeft < board.GetLength(0); tileLeft++)
			{
				for (int tileTop = 0; tileTop < board.GetLength(1); tileTop++)
				{
					var playerId = board[tileLeft, tileTop];

//					if (!IsStraggler(ref board, tileLeft, tileTop, playerId))
//					{
//						continue;
//					}

					MigrateStraggler(ref board, tileLeft, tileTop, playerId);
					//straggerFound = true;
				}
			}
		}

		private static bool IsStraggler(ref byte[,] board, int tileLeft, int tileTop, byte playerId)
		{
			int lastTile = board.GetLength(0) - 1;
			bool hasTop = (tileTop > 0), hasBottom = (tileTop < lastTile);
			
			// Left
			if (tileLeft > 0)
			{
				if (board[tileLeft - 1, tileTop] == playerId)
				{
					return (false);
				}

				// Top-left
				if (hasTop && (board[tileLeft - 1, tileTop - 1] == playerId))
				{
					return (false);
				}

				// Bottom-left
				if (hasBottom && (board[tileLeft - 1, tileTop + 1] == playerId))
				{
					return (false);
				}
			}

			// Right
			if (tileLeft < lastTile)
			{
				if (board[tileLeft + 1, tileTop] == playerId)
				{
					return (false);
				}

				// Top-right
				if (hasTop && (board[tileLeft + 1, tileTop - 1] == playerId))
				{
					return (false);
				}

				// Bottom-right
				if (hasBottom && (board[tileLeft + 1, tileTop + 1] == playerId))
				{
					return (false);
				}
			}

			// Top
			if (hasTop && (board[tileLeft, tileTop - 1] == playerId))
			{
				return (false);
			}

			// Top
			if (hasBottom && (board[tileLeft, tileTop + 1] == playerId))
			{
				return (false);
			}

			return (true);
		}

		private static void MigrateStraggler(ref byte[,] board, int stragglerTileLeft,
		                                int stragglerTileTop, byte playerId)
		{
			double bestScore = double.MinValue;
			int bestTileLeft = stragglerTileLeft, bestTileTop = stragglerTileTop;
			bool foundBetterScore = true;

			logger.DebugFormat("Migrating player {0} at {1}, {2}", playerId, stragglerTileLeft, stragglerTileTop);
			
			while (foundBetterScore)
			{
				foundBetterScore = false;
				
				for (int tileLeft = 0; tileLeft < board.GetLength(0); tileLeft++)
				{
					for (int tileTop = 0; tileTop < board.GetLength(1); tileTop++)
					{
						var swapPlayerId = board[tileLeft, tileTop];
						
						if ((swapPlayerId == playerId) ||
						    ((tileLeft == stragglerTileLeft) && (tileTop == stragglerTileTop)) ||
						    IsStraggler(ref board, stragglerTileLeft, stragglerTileTop, swapPlayerId))
						{
							continue;
						}

						// DEBUG
//						board[stragglerTileLeft, stragglerTileTop] = swapPlayerId;
//
//						if (!IsStraggler(ref board, tileLeft, tileTop, playerId))
//						{
//							logger.DebugFormat("Swapping with player {0} at {1}, {2} (done)",
//							                   swapPlayerId, tileLeft, tileTop);
//							
//							board[tileLeft, tileTop] = playerId;
//							return;
//						}
//
//						board[stragglerTileLeft, stragglerTileTop] = playerId;

						var newScore = GetClusterPixelScore(ref board, tileLeft, tileTop, playerId, true);
	
						if (newScore > bestScore)
						{
							logger.DebugFormat("Better score ({0} > {1}) found at {2}, {3}",
							                   newScore, bestScore, tileLeft, tileTop);
							
							bestScore = newScore;
							bestTileLeft = tileLeft;
							bestTileTop = tileTop;

							foundBetterScore = true;
						}
					}
				}

				if (foundBetterScore)
				{
					// Make the swap
					var swapPlayerId = board[bestTileLeft, bestTileTop];
					
					logger.DebugFormat("Swapping with player {0} at {1}, {2}",
					                   swapPlayerId, bestTileLeft, bestTileTop);
					
					
					board[bestTileLeft, bestTileTop] = playerId;
					board[stragglerTileLeft, stragglerTileTop] = swapPlayerId;

					stragglerTileLeft = bestTileLeft;
					stragglerTileTop = bestTileTop;
					return;
				}
			}
		}
	}
}
