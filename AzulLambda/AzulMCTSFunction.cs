using System;
using System.Text.Json;
using AzulLambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace AzulLambda
{
    public class AzulMCTSFunction
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public string Handle(string input)
        {
            try
            {
                // Parse the game state from the input
                var gameState = ParseGameState(input);

                // Run MCTS to determine the best move
                var bestMove = RunMCTS(gameState);

                // Return the best move as a JSON string
                return SerializeMove(bestMove);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex}");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        private GameState ParseGameState(string input)
        {
            try
            {
                var gameState = JsonSerializer.Deserialize<GameState>(input, JsonOptions);
                if (gameState == null)
                {
                    throw new InvalidOperationException("Failed to parse game state from input");
                }
                return gameState;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid game state format: {ex.Message}", ex);
            }
        }

        private Move RunMCTS(GameState gameState)
        {
            // TODO: Implement the actual MCTS algorithm here
            // For now, return a simple placeholder move
            
            // This is just a placeholder implementation
            // Find the first factory with tiles
            var factoryWithTiles = gameState.Factories.Find(f => f.Tiles.Count > 0);
            
            if (factoryWithTiles != null && factoryWithTiles.Tiles.Count > 0)
            {
                var color = factoryWithTiles.Tiles[0].Color;
                return new Move
                {
                    FactoryId = factoryWithTiles.Id,
                    Color = color,
                    PatternLine = 0 // Just place in the first pattern line for now
                };
            }
            else if (gameState.Center.Count > 0)
            {
                // If no factories have tiles, use the center
                return new Move
                {
                    FactoryId = null, // null indicates the center
                    Color = gameState.Center[0].Color,
                    PatternLine = 0
                };
            }
            
            // Fallback - shouldn't happen in a valid game state
            return new Move { FactoryId = null, Color = Color.Blue, PatternLine = 0 };
        }

        private string SerializeMove(Move move)
        {
            return JsonSerializer.Serialize(move, JsonOptions);
        }
    }
}