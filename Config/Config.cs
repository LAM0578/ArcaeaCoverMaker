using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcaeaCoverMaker.Json;
using Newtonsoft.Json;

#pragma warning disable 0649
namespace ArcaeaCoverMaker.Config
{
    [Serializable]
    public class CoverMakerConfig
    {
        [JsonProperty("index")]
        public int LastSelectedSongIndex { get; set; }
        [JsonProperty("title")]
        public string? LastSelectedSongTitle { get; set; }
        [JsonProperty("localized")]
        public string Localized { get; set; } = "en";
        [JsonProperty("rating_class")]
        public int Difficult { get; set; } = 2;
        [JsonProperty("rating_class_alias")]
        public int DifficultAlias { get; set; } = 0;
        [JsonProperty("top_title_ascii")]
        public string TopTitle { get; set; } = "Arcaea Fanmade";
        [JsonProperty("title_font_file_path")]
        public string TitleFontFilePath { get; set; } = "";
        [JsonProperty("artist_font_file_path")]
        public string ArtistFontFilePath { get; set; } = "";
        [JsonProperty("difficulty_font_file_path")]
        public string DifficultyFontFilePath { get; set; } = "";
        [JsonProperty("custom_difficult")]
        public string CustomDifficultString { get; set; } = "";
        [JsonProperty("custom_difficulty_text_scale")]
        public float CustomDifficultyTextScale { get; set; } = 1f;
		[JsonProperty("custom_difficult_color_hex")]
		public string CustomDifficultColorHex { get; set; } = "";
		[JsonProperty("custom_difficult_text_outline_color_hex")]
		public string CustomDifficultTextOutlineColorHex { get; set; } = "";
		[JsonProperty("custom_security_zone_color_hex")]
        public string CustomSecurityZoneColorHex { get; set; } = "";
        [JsonProperty("security_zone_color_alpha")]
        public int SecurityZoneColorAlpha { get; set; } = 64;
        [JsonProperty("background_alpha")]
        public byte BackgroundAlpha { get; set; } = 255;
        [JsonProperty("top_title_offset")]
        public Vector2 TopTitleOffset { get; set; } = new();
        [JsonProperty("top_title_text_offset")]
        public Vector2 TopTitleTextOffset { get; set; } = new();
        [JsonProperty("security_zone_aspect")]
        public Vector2 SecurityZoneAspect { get; set; } = new(4, 3);

        [JsonProperty("hotkey_config")]
        public HotkeyConfig HotkeyConfig = new();

        public void EnsureDifficulty()
        {
            if (Difficult < 0 || Difficult > 4)
                Difficult = 0;
        }
    }
}
