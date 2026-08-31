using System;
using System.Collections.Generic;
using System.IO;
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
		[JsonProperty("ratingClassAlias")] public int RatingClassAlias;
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

			// Return "" if the string result is null
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

			// Return the string Artist from the difficult class if it's not null and the result is not null or empty
			if (diff != null && !string.IsNullOrEmpty(result = diff.GetArtist(localized)))
				return result;

			// Return the string Artist if it's not null or empty
			if (!string.IsNullOrEmpty(Artist)) return Artist;

			// Try get value from the dictionary ArtistLocalized by the key localized
			ArtistLocalized.TryGetValue(localized, out result);

			// Return "" if the string result is null
			return result ?? "";
		}

		public string GetBackgroundFileName(int difficulty)
		{
			return StringUtility.GetString(AsciiBackground, FindDifficult(difficulty)?.AsciiBackground);
		}

		public string GetJacketFileName(int difficulty)
		{
			var diff = FindDifficult(difficulty);
			return diff is { CoverOverride: true } ? difficulty.ToString() : "base";
		}
		
		[JsonIgnore] private static readonly List<(bool isHQ, bool isSmall)> _fullJacketConditions =
		[
			(true, false),  // 1080_{name}.jpg
			(false, false), // {name}.jpg
			(true, true),   // 1080_{name}_256.jpg
			(false, true),  // {name}_256.jpg
		];

		[JsonIgnore] private static readonly List<(bool isHQ, bool isSmall)> _smallJacketConditions =
		[
			(true, true),  // 1080_{name}_256.jpg
			(false, true), // {name}_256.jpg
		];

		private static string GetJacketPath(string projectFolder, int difficulty, bool isOverride, bool isHighQuality, bool isSmall, string suffix = ".jpg")
		{
			var sb = new StringBuilder(projectFolder);
			sb.Append("/");
			if (isHighQuality) sb.Append("1080_");
			sb.Append(isOverride ? difficulty.ToString() : "base");
			if (isSmall) sb.Append("_256");
			sb.Append(suffix);
			return sb.ToString();
		}
		
		public (string fullJacketPath, string smallJacketPath) GetJacketPath(string projectFolder, int difficulty)
		{
			var isOverrideJacket = FindDifficult(difficulty)?.CoverOverride ?? false;
			
			string fullJacketPath = null;
			string smallJacketPath = null;

			if (isOverrideJacket)
			{
				foreach (var condition in _fullJacketConditions)
				{
					var path = GetJacketPath(projectFolder, difficulty, true, condition.isHQ, condition.isSmall);
					if (!File.Exists(path)) continue;
					fullJacketPath = path;
					break;
				}
			}
			if (fullJacketPath == null)
			{
				foreach (var condition in _fullJacketConditions)
				{
					var path = GetJacketPath(projectFolder, difficulty, false, condition.isHQ, condition.isSmall);
					if (!File.Exists(path)) continue;
					fullJacketPath = path;
					break;
				}
			}
			
			foreach (var condition in _smallJacketConditions)
			{
				var path = GetJacketPath(projectFolder, difficulty, isOverrideJacket, condition.isHQ, condition.isSmall);
				if (!File.Exists(path)) continue;
				smallJacketPath = path;
				break;
			}

			smallJacketPath ??= fullJacketPath;
			
			return (fullJacketPath, smallJacketPath);
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
		/// Find the song class by the serach title and the song id.
		/// </summary>
		/// <param name="title">The serach title</param>
		/// <param name="difficultyIndex">Difficult class ID</param>
		/// <returns></returns>
		public ArcSong? FindSong(string? title, int difficultyIndex)
		{
			if (title == null) return null;
			return Songs.FindLast(s =>
			{
				if (s.AsciiId == title)
				{
					return true;
				}
				else
				{
					var diff = s.FindDifficult(difficultyIndex);
					return s.TitleLocalized.Exists(title) || diff != null && diff.TitleLocalized.Exists(title);
				}
			});
		}

		/// <summary>
		/// Find the song class by the serach title or the song index.
		/// </summary>
		/// <param name="title">The serach title</param>
		/// <param name="index">Song index</param>
		/// <param name="difficultyIndex">Difficult class ID</param>
		/// <returns></returns>
		public ArcSong FindSong(string title, int index, int difficultyIndex)
		{
			return FindSong(title, difficultyIndex) ?? FindSong(index) ?? new();
		}
	}
}
