using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using ArcaeaCoverMaker.Util;

namespace ArcaeaCoverMaker
{
	[Serializable]
	public class ArcSongDifficult
	{
		[JsonProperty("ratingClass")] public int RatingClass;
		[JsonProperty("rating")] public int Rating;
		[JsonProperty("ratingPlus")] public bool RatingPlus;
		[JsonProperty("bg")] public string? AsciiBackground;
		[JsonProperty("jacketOverride")] public bool CoverOverride;

		[JsonProperty("title_localized")]
		public Dictionary<string, string> TitleLocalized = new();
		[JsonProperty("artist")] public string? Artist;
		[JsonProperty("artist_localized")]
		public Dictionary<string, string> ArtistLocalized = new();

		public string GetTitle(string localized)
		{
			// Try get value from the dictionary TitleLocalized by the key localized
			TitleLocalized.TryGetValue(localized, out var result);
			return result ?? "";
		}
		public string GetArtist(string localized)
		{
			// Return the string Artist if it's not null or empty
			if (!string.IsNullOrEmpty(Artist)) return Artist;

			// Try get value from the dictionary ArtistLocalized by the key localized
			ArtistLocalized.TryGetValue(localized, out var result);

			// Return "" if the string result is null
			return result ?? "";
		}

		/// <summary>
		/// The string of the rating. (return "?" if the rating is equal to 0)
		/// </summary>
		public string RatingString
			=> new StringBuilder(Rating == 0 ? "?" : Rating.ToString())
				.Append(RatingPlus ? "+" : "").ToString();
	}
	[Serializable]
	public class ArcSong
	{
		[JsonProperty("idx")] public int? Index;
		[JsonProperty("id")] public string? AsciiId;

		[JsonProperty("title_localized")]
		public Dictionary<string, string> TitleLocalized = new();
		[JsonProperty("artist")] public string? Artist;
		[JsonProperty("artist_localized")]
		public Dictionary<string, string> ArtistLocalized = new();

		[JsonProperty("side")] public int Side;
		[JsonProperty("bg")] public string? AsciiBackground;
		[JsonProperty("remote_dl")] public bool NeedDownload;

		[JsonProperty("difficulties")] public List<ArcSongDifficult> Difficulties = new();

		/// <summary>
		/// Find the difficult class by difficult class ID (rating class).
		/// </summary>
		/// <param name="id">Difficult class ID</param>
		/// <returns></returns>
		public ArcSongDifficult? FindDifficult(int id)
		{
			return Difficulties.FindLast(t => t.RatingClass == id);
		}

		/// <summary>
		/// Find the difficult class by the song title.
		/// </summary>
		/// <param name="title">Song title</param>
		/// <returns></returns>
		public ArcSongDifficult? FindDifficult(string title)
		{
			return Difficulties.FindLast(t => t.TitleLocalized.Exists(title));
		}

		public string GetTitle(string localized, int diffId)
		{
			// Find the difficult class by difficult class ID
			ArcSongDifficult? diff = FindDifficult(diffId);
			string? result;

			// Return the title from the difficult class if it's not null and the result is not null or empty
			if (diff != null && !string.IsNullOrEmpty(result = diff.GetTitle(localized))) 
				return result;

			// Try get value from the dictionary TitleLocalized by the key localized
			TitleLocalized.TryGetValue(localized, out result);

			// Return "" if the string result is null
			return result ?? "";
		}
		public string GetArtist(string localized, int diffId)
		{
			// Find the difficult class by difficult class ID
			ArcSongDifficult? diff = FindDifficult(diffId);
			string? result;
			if (diff != null && !string.IsNullOrEmpty(result = diff.GetArtist(localized)))
				return result;

			// Return the string Artist if it's not null or empty
			if (!string.IsNullOrEmpty(Artist)) return Artist;

			// Try get value from the dictionary ArtistLocalized by the key localized
			ArtistLocalized.TryGetValue(localized, out result);

			// Return "" if the string result is null
			return result ?? "";
		}

		public string GetBackgroundName(int diffId)
		{
			return StringUtility.GetString(AsciiBackground, FindDifficult(diffId)?.AsciiBackground);
		}
	}
	[Serializable]
	public class ArcSonglist
	{
		[JsonProperty("songs")] public List<ArcSong> Songs = new();

		/// <summary>
		/// Find the song class by the song index (idx).
		/// </summary>
		/// <param name="index">Song index</param>
		/// <returns></returns>
		public ArcSong? FindSong(int index)
		{
			return Songs.FindLast(s => s.Index == index);
		}

		/// <summary>
		/// Find the song class by the song title.
		/// </summary>
		/// <param name="title">Song title</param>
		/// <param name="diffId">Difficult class ID</param>
		/// <returns></returns>
		public ArcSong? FindSong(string? title, int diffId)
		{
			if (title == null) return null;
			return Songs.FindLast(s =>
			{
				var diff = s.FindDifficult(diffId);
				return s.TitleLocalized.Exists(title) || diff != null && diff.TitleLocalized.Exists(title);
			});
		}

		/// <summary>
		/// Find the song class by the title or the song index.
		/// </summary>
		/// <param name="title">Song title</param>
		/// <param name="index">Song index</param>
		/// <param name="diffId">Difficult class ID</param>
		/// <returns></returns>
		public ArcSong FindSong(string title, int index, int diffId)
		{
			return FindSong(title, diffId) ?? FindSong(index) ?? new();
		}
	}
}
