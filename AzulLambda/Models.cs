using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzulLambda
{
    public enum Color
    {
        [StringValue("Blue")]
        Blue,
        [StringValue("Yellow")]
        Yellow,
        [StringValue("Red")]
        Red,
        [StringValue("Black")]
        Black,
        [StringValue("White")]
        White
    }

    public class Tile
    {
        [JsonPropertyName("color")]
        public Color Color { get; set; }

        [JsonPropertyName("selected")]
        public bool Selected { get; set; }
    }

    public class Factory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tiles")]
        public List<Tile> Tiles { get; set; } = new List<Tile>();
    }

    public class PlayerBoard
    {
        [JsonPropertyName("wall")]
        public List<List<Tile?>> Wall { get; set; } = new List<List<Tile?>>();

        [JsonPropertyName("patternLines")]
        public List<List<Tile?>> PatternLines { get; set; } = new List<List<Tile?>>();

        [JsonPropertyName("floorLine")]
        public List<Tile> FloorLine { get; set; } = new List<Tile>();
    }

    public class Player
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("board")]
        public PlayerBoard Board { get; set; } = new PlayerBoard();

        [JsonPropertyName("score")]
        public int Score { get; set; }
    }

    public class GameState
    {
        [JsonPropertyName("players")]
        public List<Player> Players { get; set; } = new List<Player>();

        [JsonPropertyName("factories")]
        public List<Factory> Factories { get; set; } = new List<Factory>();

        [JsonPropertyName("center")]
        public List<Tile> Center { get; set; } = new List<Tile>();

        [JsonPropertyName("currentPlayerIndex")]
        public int CurrentPlayerIndex { get; set; }

        [JsonPropertyName("round")]
        public int Round { get; set; }
    }

    public class Move
    {
        [JsonPropertyName("factoryId")]
        public int? FactoryId { get; set; }

        [JsonPropertyName("color")]
        public Color Color { get; set; }

        [JsonPropertyName("patternLine")]
        public int PatternLine { get; set; }
    }
}