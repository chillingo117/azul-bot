using System.Text.Json;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace AzulLambda
{
    public static class AzulMCTSFunction
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static Func<GameState, Move> RunMCTS()
        {
            return gameState =>
            {
                // Use CurrentPlayerIndex directly
                int currentPlayerId = gameState.CurrentPlayerIndex;

                // Instantiate the MyTeamAgent
                var agent = new MCTSAgent(currentPlayerId);

                // Generate the list of legal actions using GameState
                List<Move> actions = new AzulSimulator(gameState).GenerateLegalActions();
                if (actions.Count == 0)
                {
                    throw new InvalidOperationException("No legal actions available.");
                }
                // Use the agent to select the best action
                return agent.SelectAction(actions, gameState);
            };
        }

        public static string Handle(string input)
        {
            try
            {
                // Parse the game state from the input
                var gameState = ParseGameState(input);

                // Run MCTS to determine the best move
                var bestMove = RunMCTS()(gameState);

                // Return the best move as a JSON string
                return SerializeMove(bestMove);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex}");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        private static GameState ParseGameState(string input)
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

        private static string SerializeMove(Move move)
        {
            return JsonSerializer.Serialize(move, JsonOptions);
        }

        private static List<Move> GenerateLegalActions(GameState gameState)
        {
            var actions = new List<Move>();

            // Generate moves based on factories
            foreach (var factory in gameState.Factories)
            {
                if (factory.Tiles.Count == 0)
                {
                    // Skip empty factories
                    continue;
                }

                foreach (var tile in factory.Tiles)
                {
                    for (int i = 0; i < 5; i++) // 5 pattern lines
                    {
                        actions.Add(new Move
                        {
                            FactoryId = factory.Id,
                            Color = tile.Color,
                            PatternLine = i
                        });
                    }

                    // Add move for floor line (-1 indicates floor line)
                    actions.Add(new Move
                    {
                        FactoryId = factory.Id,
                        Color = tile.Color,
                        PatternLine = -1
                    });
                }
            }

            // Generate moves based on center
            foreach (var tile in gameState.Center)
            {
                for (int i = 0; i < 5; i++) // 5 pattern lines
                {
                    actions.Add(new Move
                    {
                        FactoryId = null,
                        Color = tile.Color,
                        PatternLine = i
                    });
                }

                // Add move for floor line (-1 indicates floor line)
                actions.Add(new Move
                {
                    FactoryId = null,
                    Color = tile.Color,
                    PatternLine = -1
                });
            }

            return actions;
        }
    }
}