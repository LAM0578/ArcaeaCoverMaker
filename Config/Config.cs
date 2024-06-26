﻿using System;
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
    internal class CoverMakerConfig
	{
        [JsonProperty("index")]
        public int LastSelectedSongIndex;
        [JsonProperty("title")]
        public string? LastSelectedSongTitle;
        [JsonProperty("read_remote_dl_with_head")]
        public bool ReadNeedDownloadSongWithHead = true;
        [JsonProperty("localized")]
        public string Localized = "en";
        [JsonProperty("rating_class")]
        public int Difficult = 2;
        [JsonProperty("top_title_ascii")]
        public string TopTitle = "Arcaea Fanmade";
        [JsonProperty("title_font_file_path")]
        public string TitleFontFilePath = "";
        [JsonProperty("artist_font_file_path")]
        public string ArtistFontFilePath = "";
        [JsonProperty("difficulty_font_file_path")]
        public string DifficultyFontFilePath = "";
		[JsonProperty("custom_difficult")]
		public string CustomDifficultString = "";
		[JsonProperty("custom_difficulty_text_scale")]
		public float CustomDifficultyTextScale = 1f;
		[JsonProperty("custom_difficult_color_hex")]
		public string CustomDifficultColorHex = "";
        [JsonProperty("custom_security_zone_color_hex")]
        public string CustomSecurityZoneColorHex = "";
        [JsonProperty("security_zone_color_alpha")]
        public int SecurityZoneColorAlpha = 64;
        [JsonProperty("background_alpha")] 
        public byte BackgroundAlpha = 255;
		[JsonProperty("top_title_offset")]
		public Vector2 TopTitleOffset = new();
		[JsonProperty("top_title_text_offset")]
		public Vector2 TopTitleTextOffset = new();
        [JsonProperty("security_zone_aspect")]
        public Vector2 SecurityZoneAspect = new(4, 3);

		[JsonProperty("hotkey_config")]
        public HotkeyConfig HotkeyConfig = new();
    }
}
