using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;

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
			TitleLocalized.TryGetValue(localized, out var result);
			return result ?? "";
		}
		public string GetArtist(string localized)
		{
			if (!string.IsNullOrEmpty(Artist)) return Artist;
			ArtistLocalized.TryGetValue(localized, out var result);
			return result ?? "";
		}

		public string RatingStr
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
		/// Find difficult object by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ArcSongDifficult? FindDifficult(int id)
		{
			return Difficulties.FindLast(t => t.RatingClass == id);
		}

		/// <summary>
		/// Find difficult object by song title.
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		public ArcSongDifficult? FindDifficult(string title)
		{
			return Difficulties.FindLast(t => t.TitleLocalized.Exists(title));
		}

		public string GetTitle(string localized, int diffId)
		{
			//
			ArcSongDifficult? diff = FindDifficult(diffId);
			string? result;
			if (diff != null && !string.IsNullOrEmpty(result = diff.GetTitle(localized))) 
				return result;
			//
			TitleLocalized.TryGetValue(localized, out result);
			return result ?? "";
		}
		public string GetArtist(string localized, int diffId)
		{
			//
			ArcSongDifficult? diff = FindDifficult(diffId);
			string? result;
			if (diff != null && !string.IsNullOrEmpty(result = diff.GetArtist(localized)))
				return result;
			//
			if (!string.IsNullOrEmpty(Artist)) return Artist;
			ArtistLocalized.TryGetValue(localized, out result);
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
		/// Find song by index (idx).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ArcSong? FindSong(int index)
		{
			return Songs.FindLast(s => s.Index == index);
		}

		/// <summary>
		/// Find song by song title.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="diffId"></param>
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
		/// 
		/// </summary>
		/// <param name="title"></param>
		/// <param name="index"></param>
		/// <param name="diffId"></param>
		/// <returns></returns>
		public ArcSong FindSong(string title, int index, int diffId)
		{
			return FindSong(title, diffId) ?? FindSong(index) ?? new();
		}
	}
}
