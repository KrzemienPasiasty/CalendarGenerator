using Newtonsoft.Json;
using System.Drawing;
using System.Globalization;


namespace CalendarGenerator
{
    class Config
    {
        public static Config Default { get; private set; } = new Config();

        [JsonProperty] public static string? CsvFilepath { get; set; }
        [JsonProperty] public static string? SaveFilepath { get; set; }
        [JsonProperty] public static string? AdditionalClassesFilepath { get; set; }
        [JsonProperty] public static int Dpi { get; set; }
        [JsonProperty] public static string? Font { get; set; } //FL Pismo Techniczne
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static Color BackgroundColor { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static Color HightlightColor { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static Color LowPriorityEvent { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static Color MediumPriorityEvent { get; set; }
        [JsonProperty][JsonConverter(typeof(ColorJsonConverter))] public static Color HighPriorityEvent { get; set; }
        [JsonProperty] public static int BorderMargin { get; set; } //5
        /// <summary>time and type font size</summary>
        [JsonProperty] public static float SmallFontSize { get; set; } //6.5
        /// <summary>class name font size</summary>
        [JsonProperty] public static float StandardFontSize { get; set; } //8.5
        /// <summary>day of week font size</summary>
        [JsonProperty] public static float BigFontSize { get; set; } //11
        [JsonProperty] public static bool HightlightLectures { get; set; }





        public static void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Config file not found.", path);

            Default = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))
                      ?? throw new JsonException($"Config file '{path}' is empty or invalid.");

            if (CsvFilepath == null || SaveFilepath == null || AdditionalClassesFilepath == null || Dpi <= 0 || Font == null)
            {
                Console.WriteLine("Same configuration are not exist. Please check the config.json file.");
                throw new Exception("Configuration error");
            }
        }

        public static void Save(string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(Default, Formatting.Indented));
        }



    }

    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
            => writer.WriteValue($"#{value.ToArgb():X8}");

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue,
                                       bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return default;

            var s = (reader.Value as string)?.Trim() ?? throw new JsonSerializationException("Expected a color string.");

            if (!s.StartsWith("#")) return Color.FromName(s);

            var hex = s.Substring(1);
            if (hex.Length == 6) hex = "FF" + hex;   // no alpha given -> opaque

            return Color.FromArgb(int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }
    }
}
