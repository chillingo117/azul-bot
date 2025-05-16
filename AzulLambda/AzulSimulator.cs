using System;
using System.Collections.Generic;
using System.Linq;

namespace AzulLambda
{
    public class AzulSimulator
    {
        public GameState GameState { get; private set; }
        private List<Tile> TileBag = new List<Tile>();

        public AzulSimulator(GameState gameState)
        {
            GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        }

        public void MakeMove(int playerId, int? factoryId, Color color, int patternLine, out bool gameOver)
        {
            if (playerId != GameState.CurrentPlayerIndex)
            {
                throw new InvalidOperationException($"Not player {playerId} turn, it is {GameState.CurrentPlayerIndex}.");
            }
            gameOver = false;
            var player = GameState.Players[GameState.CurrentPlayerIndex];
            var selectedTiles = SelectTiles(factoryId, color);

            if (patternLine == -1)
            {
                AddToFloorLine(player, selectedTiles);
            }
            else if (CanPlaceTiles(player, patternLine, selectedTiles))
            {
                PlaceTiles(player, patternLine, selectedTiles);
            }
            else
            {
                AddToFloorLine(player, selectedTiles);
            }

            AdvanceToNextPlayer();

            if (IsRoundOver())
            {
                EndRound();
            }

            gameOver = IsGameOver();
        }

        private List<Tile> SelectTiles(int? factoryId, Color color)
        {
            if (factoryId == null)
            {
                var selected = GameState.Center.Where(tile => tile.Color == color).ToList();
                GameState.Center.RemoveAll(tile => tile.Color == color);
                return selected;
            }

            var factory = GameState.Factories.First(f => f.Id == factoryId);
            var selectedTiles = factory.Tiles.Where(tile => tile.Color == color).ToList();
            var leftoverTiles = factory.Tiles.Where(tile => tile.Color != color).ToList();

            GameState.Center.AddRange(leftoverTiles);
            factory.Tiles.Clear();

            return selectedTiles;
        }

        private bool CanPlaceTiles(Player player, int patternLine, List<Tile> tiles)
        {
            var line = player.Board.PatternLines[patternLine];
            var wallRow = player.Board.Wall[patternLine];

            if (wallRow.Any(tile => tile?.Color == tiles[0].Color)) return false;
            if (line.Any(tile => tile != null && tile.Color != tiles[0].Color)) return false;
            if (line.All(tile => tile != null)) return false;

            return true;
        }

        private void PlaceTiles(Player player, int patternLine, List<Tile> tiles)
        {
            var line = player.Board.PatternLines[patternLine];

            for (int i = 0; i < line.Count && tiles.Count > 0; i++)
            {
                if (line[i] == null)
                {
                    line[i] = tiles[0];
                    tiles.RemoveAt(0);
                }
            }

            AddToFloorLine(player, tiles);
        }

        private void AddToFloorLine(Player player, List<Tile> tiles)
        {
            player.Board.FloorLine.AddRange(tiles);
        }

        private void AdvanceToNextPlayer()
        {
            GameState.CurrentPlayerIndex = (GameState.CurrentPlayerIndex + 1) % 2;
        }

        private bool IsRoundOver()
        {
            return GameState.Factories.All(factory => !factory.Tiles.Any()) && !GameState.Center.Any();
        }

        private void EndRound()
        {
            foreach (var player in GameState.Players)
            {
                MoveTilesToWallAndScore(player);
                ClearFloorLine(player);
            }

            FillFactories();
            GameState.Round++;
        }

        private void MoveTilesToWallAndScore(Player player)
        {
            for (int row = 0; row < player.Board.PatternLines.Count; row++)
            {
                var line = player.Board.PatternLines[row];
                if (line.All(tile => tile != null))
                {
                    var tile = line[0];
                    var col = Array.IndexOf(Constants.DefaultMosaicColors[row], tile!.Color);

                    if (player.Board.Wall[row][col] == null)
                    {
                        player.Board.Wall[row][col] = tile;
                        player.Score += 1 + CountAdjacentTiles(player.Board.Wall, row, col);
                    }

                    player.Board.PatternLines[row] = new List<Tile?>(new Tile?[line.Count]);
                }
            }
        }

        private int CountAdjacentTiles(List<List<Tile?>> wall, int row, int col)
        {
            int score = 0;

            // Horizontal
            for (int c = col - 1; c >= 0 && wall[row][c] != null; c--) score++;
            for (int c = col + 1; c < wall[row].Count && wall[row][c] != null; c++) score++;

            // Vertical
            for (int r = row - 1; r >= 0 && wall[r][col] != null; r--) score++;
            for (int r = row + 1; r < wall.Count && wall[r][col] != null; r++) score++;

            return score;
        }

        private void ClearFloorLine(Player player)
        {
            var penalties = Constants.FloorLinePenalties;
            int penalty = player.Board.FloorLine.Take(penalties.Length).Select((_, i) => penalties[i]).Sum();
            player.Score = Math.Max(0, player.Score + penalty);
            player.Board.FloorLine.Clear();
        }

        private void RefillTileBag()
        {
            TileBag = Enum.GetValues<Color>()
                .SelectMany(color => Enumerable.Range(0, 20).Select(_ => new Tile { Color = color, Selected = false }))
                .ToList();
            TileBag = TileBag.OrderBy(_ => Guid.NewGuid()).ToList(); // Shuffle
        }

        private void FillFactories()
        {
            foreach (var factory in GameState.Factories)
            {
                if (TileBag.Count < 4) RefillTileBag();
                factory.Tiles = TileBag.Take(4).ToList();
                TileBag.RemoveRange(0, 4);
            }
        }

        private bool IsGameOver()
        {
            return GameState.Players.Any(player => player.Board.Wall.Any(row => row.All(tile => tile != null)));
        }

        public List<Move> GenerateLegalActions()
        {
            var actions = new List<Move>();

            // Generate moves based on factories
            foreach (var factory in GameState.Factories)
            {
                if (factory.Tiles.Count == 0)
                    continue;

                // distinct by color to avoid duplicate actions
                foreach (var tile in factory.Tiles.DistinctBy(tile => tile.Color))
                {
                    // Check each pattern line (0-4)
                    for (int i = 0; i < GameState.Players[GameState.CurrentPlayerIndex].Board.PatternLines.Count; i++)
                    {
                        var line = GameState.Players[GameState.CurrentPlayerIndex].Board.PatternLines[i];

                        bool lineHasDifferentColor = line.Any(t => t != null && t.Color != tile.Color);
                        bool lineIsFull = line.All(t => t != null);
                        var wallRow = GameState.Players[GameState.CurrentPlayerIndex].Board.Wall[i];
                        bool wallHasColor = wallRow.Any(t => t != null && t.Color == tile.Color);

                        // Check if the line is not full, does not have a different color, and the wall does not have the same color
                        // Technically the wall check is not necessary, but you may as well just send the tiles to the floor in that case
                        if (!lineHasDifferentColor && !lineIsFull && !wallHasColor)
                        {
                            actions.Add(new Move
                            {
                                FactoryId = factory.Id,
                                Color = tile.Color,
                                PatternLine = i
                            });
                        }
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
            foreach (var tile in GameState.Center)
            {
                for (int i = 0; i < GameState.Players[GameState.CurrentPlayerIndex].Board.PatternLines.Count; i++)
                {
                    var line = GameState.Players[GameState.CurrentPlayerIndex].Board.PatternLines[i];
                    bool lineHasDifferentColor = line.Any(t => t != null && t.Color != tile.Color);
                    bool lineIsFull = line.All(t => t != null);
                    var wallRow = GameState.Players[GameState.CurrentPlayerIndex].Board.Wall[i];
                    bool wallHasColor = wallRow.Any(t => t != null && t.Color == tile.Color);

                    if (!lineHasDifferentColor && !lineIsFull && !wallHasColor)
                    {
                        actions.Add(new Move
                        {
                            FactoryId = null,
                            Color = tile.Color,
                            PatternLine = i
                        });
                    }
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