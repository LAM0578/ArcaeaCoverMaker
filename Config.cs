using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcaeaCoverMaker.Hotkey;
using Newtonsoft.Json;

#pragma warning disable 0649
namespace ArcaeaCoverMaker
{
	[Serializable]
	internal class Config
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
		[JsonProperty("custom_difficult")]
		public string CustomDifficultString = "";

		[JsonProperty("hotkey_config")]
		public HotkeyConfig HotkeyConfig = new();
	}
}
