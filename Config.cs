using Newtonsoft.Json;
using SkiaSharp;
using System.Globalization;


namespace CalendarGenerator
{
    /// <summary>
    /// Represents the configuration settings for the calendar generator application.
    /// </summary>
    class Config
    {
        public static Config Default { get; private set; } = new Config();

        [JsonProperty] public static string? IcsUrl { get; set; }
        [JsonProperty("CsvFilepath")] private static string? LegacyCsvFilepath { set => IcsUrl ??= value; }
        [JsonProperty] public static string? SaveFilepath { get; set; }
        [JsonProperty] public static string? AdditionalClassesFilepath { get; set; }
        [JsonProperty] public static int Dpi { get; set; }
        [JsonProperty] public static string? Font { get; set; } //FL Pismo Techniczne
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static SKColor BackgroundColor { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static SKColor HightlightColor { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static SKColor LowPriorityEventColor { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static SKColor MediumPriorityEventColor { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static SKColor HighPriorityEventColor { get; set; }
        [JsonProperty] public static int BorderMargin { get; set; } //5
        /// <summary>time and type font size</summary>        
        [JsonProperty] public static float SmallFontSize { get; set; } //6.5
        /// <summary>class name font size</summary>
        [JsonProperty] public static float StandardFontSize { get; set; } //8.5
        /// <summary>day of week font size</summary>
        [JsonProperty] public static float BigFontSize { get; set; } //11
        /// <summary>/// If true, the lectures will be highlighted with the HightlightColor in the calendar. /// </summary>
        [JsonProperty] public static bool HightlightLectures { get; set; }




        /// <summary>
        /// Loads the configuration from a JSON file at the specified path.
        /// </summary>
        /// <param name="path">The path to the JSON file.</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="Exception">Some of configurations in the JSON file are missing or invalid.</exception>
        public static void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Config file not found.", path);

            Default = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))
                      ?? throw new JsonException($"Config file '{path}' is empty or invalid.");

            if (string.IsNullOrWhiteSpace(IcsUrl) || SaveFilepath == null || AdditionalClassesFilepath == null || Dpi <= 0 || Font == null)
            {
                Console.WriteLine("Same configuration are not exist. Please check the config.json file.");
                throw new Exception("Configuration error");
            }
        }


        /// <summary>
        /// Saves the current configuration to a JSON file at the specified path.
        /// </summary>
        /// <param name="path">The path to the JSON file.</param>
        public static void Save(string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(Default, Formatting.Indented));
        }
    }


    /// <summary>
    /// A custom JSON converter for the SkiaSharp.SKColor type.
    /// </summary>
    public class ColorJsonConverter : JsonConverter<SKColor>
    {
        public override void WriteJson(JsonWriter writer, SKColor value, JsonSerializer serializer)
            => writer.WriteValue($"#{(uint)value:X8}");   // #AARRGGBB

        public override SKColor ReadJson(JsonReader reader, Type objectType, SKColor existingValue,
                                         bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return default;

            var s = (reader.Value as string)?.Trim()
                    ?? throw new JsonSerializationException("Expected a color string.");

            // Handles #RGB, #RRGGBB (opaque) and #AARRGGBB
            if (SKColor.TryParse(s, out var color))
                return color;

            throw new JsonSerializationException($"Invalid color value '{s}'.");
        }
    }
}
