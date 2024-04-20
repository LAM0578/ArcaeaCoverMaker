using System;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Input;
using System.Collections.Generic;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using ArcaeaCoverMaker.Logging;
using ArcaeaCoverMaker.Config;
using ArcaeaCoverMaker.Json;
using ArcaeaCoverMaker.Util;
using IO = System.IO;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

#pragma warning disable 8600, 8601, 8604, 8618
namespace ArcaeaCoverMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public MainWindow()
		{
			CreateLogger();
			try
			{
				InitializeComponent();
				Reload();
			}
			catch (Exception ex)
			{
				Logger.LogError(ex.Message, ex);
			}
		}

		private static void CreateLogger()
		{
			string logDir = IO.Path.Combine(AppContext.BaseDirectory, "Logs");
			IO.Directory.CreateDirectory(logDir);

			string logPath = IO.Path.Combine(logDir, $"log_{DateTime.Now.Ticks}.log");
			Logger = Logger.CreateLogger(logPath);
		}

		private static Logger Logger;

		private static readonly string AppBaseDirectory = AppContext.BaseDirectory;
		private static readonly Dictionary<string, string> Directories = new()
		{
			["Background"] = IO.Path.Combine(AppBaseDirectory, "Background"),
			["Song"] = IO.Path.Combine(AppBaseDirectory, "Songs"),
			["Font"] = IO.Path.Combine(AppBaseDirectory, "Fonts"),
			["Capture"] = IO.Path.Combine(AppBaseDirectory, "Captures")
		};

		private static readonly string ConfigPath = IO.Path.Combine(AppBaseDirectory, "Config.json");
		private static readonly string SonglistPath = IO.Path.Combine(AppBaseDirectory, "Songlist.json");

		private Dictionary<string, SKBitmap?> BackgroundBitmaps = new();
		private Dictionary<string, SKBitmap?> CoverBitmaps = new();

		private static ArcSonglist Songlist = new();
		private static CoverMakerConfig Config = new();
		private static HotkeyConfig HotkeyConfig = new();

		private ArcSong CurrentSong = new();

		private static List<SKBitmap> InternalBackgrounds = new();

		private static SKBitmap CurrentBackground = new();
		private static SKBitmap CurrentCover = new();

		private bool IsCapture;
		private bool EnableSecurityZone;

		private static SKColor? CustomDifficultColor = null;
		private static readonly SKColor[] DifficultColors = new SKColor[]
		{
			new SKColor(0x0a,0x82,0xbe,255), // Past
			new SKColor(0x64,0x8c,0x3c,255), // Present
			new SKColor(0x50,0x19,0x4b,255), // Future
			new SKColor(0x82,0x23,0x28,255), // Beyond
			new SKColor(0x5d,0x4e,0x76,255)  // Eternal
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CheckDirectories()
		{
			foreach (var item in Directories)
			{
				IO.Directory.CreateDirectory(item.Value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void WriteDataIfNotExists(string path, string? data)
		{
			try
			{
				if (!IO.File.Exists(path) || string.IsNullOrEmpty(IO.File.ReadAllText(path)))
					IO.File.WriteAllText(path, data);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex.Message, ex);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LoadInternalBackgrounds()
		{
			InternalBackgrounds = new()
			{
				Helper.SetInternalBacgroundIfExist(
					 IO.Path.Combine(Directories["Background"], "base_light.jpg"),
					 "Backgrounds.internal_bg_light.png"),
				Helper.SetInternalBacgroundIfExist(
					 IO.Path.Combine(Directories["Background"], "base_conflict.jpg"),
					 "Backgrounds.internal_bg_conflict.png")
			};
		}

		/// <summary>
		/// Unload all skia bitmap resources.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Unload()
		{
			foreach (var item in CoverBitmaps)
			{
				item.Value?.Dispose();
			}
			CoverBitmaps.Clear();

			foreach (var item in BackgroundBitmaps)
			{
				item.Value?.Dispose();
			}
			BackgroundBitmaps.Clear();
		}

		/// <summary>
		/// Load config from file and load images from files.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Load()
		{
			//// File ////
			
			WriteDataIfNotExists(SonglistPath, null);
			WriteDataIfNotExists(ConfigPath, 
				  JsonConvert.SerializeObject(new CoverMakerConfig(), Formatting.Indented));

			//////////////

			Unload();

			// Load songlist
			Songlist = Utility.TryDeserializeObject<ArcSonglist>(IO.File.ReadAllText(SonglistPath));

			// Load config
			Config = Utility.TryDeserializeObject<CoverMakerConfig>(IO.File.ReadAllText(ConfigPath));
			HotkeyConfig = Config.HotkeyConfig;

			Config.Difficult = (Config.Difficult < 0 || Config.Difficult > 4) ? 0 : Config.Difficult;
			ColorUtility.TryParseSKColor(Config.CustomDifficultColorHex, out CustomDifficultColor);

			// Load cover images from song folder
			foreach (var song in Songlist.Songs)
			{
				string songPath = IO.Path.Combine(Directories["Song"],
					(Config.ReadNeedDownloadSongWithHead && song.NeedDownload ? "dl_" : "") + song.AsciiId);

				if (IO.Directory.Exists(songPath))
				{
					string coverPath = IO.Path.Combine(songPath, $"base.jpg");
					CoverBitmaps.Add(song.AsciiId, SkiaSharpUtility.ReadBitmapFrom(coverPath));
				}
			}
			CurrentSong = Songlist.FindSong(
				Config.LastSelectedSongTitle, Config.LastSelectedSongIndex, Config.Difficult);

			// Load background images
			foreach (var path in IO.Directory.GetFiles(Directories["Background"])
				.Where(t => t.ToLower().EndsWith(".jpg")))
			{
				using var fs = IO.File.OpenRead(path);
				BackgroundBitmaps.Add(
					   IO.Path.GetFileNameWithoutExtension(path), SKBitmap.Decode(fs));
			}

			string backgroundName = CurrentSong.GetBackgroundName(Config.Difficult);
			if (string.IsNullOrEmpty(backgroundName))
			{
				CurrentBackground = InternalBackgrounds[CurrentSong.Side == 1 ? 1 : 0];
			}
			else
			{
				BackgroundBitmaps.TryGetValue(backgroundName, out CurrentBackground);
			}

			CoverBitmaps.TryGetValue(CurrentSong.AsciiId ?? "", out CurrentCover);

			// // Print load info
			// Logger.LogMessage($"CoverBitmaps.Count: {CoverBitmaps.Count}{Environment.NewLine}" +
			// 	$"BackgroundBitmaps.Count: {BackgroundBitmaps.Count}");

			SkiaElement.InvalidateVisual();

			EnableSecurityZone = !EnableSecurityZone;
			SecurityZone.InvalidateVisual();
		}

		/// <summary>
		/// Reload files and re-draw the canvas.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Reload()
		{
			CheckDirectories();
			LoadInternalBackgrounds();
			Load();
			GC.Collect();
		}

		/// <summary>
		/// Capture surface content and save.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Capture()
		{
			IsCapture = true;
			SkiaElement.InvalidateVisual();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 GetWindowSizeInternal(Vector2 aspect)
		{
			double segment = SkiaElement.RenderSize.Width / aspect.X;
			double ratio = Height / SkiaElement.RenderSize.Height;
			return new Vector2((float)Width, (float)(segment * aspect.Y * ratio));
		}

		/// <summary>
		/// Sets the height of the window proportionally to the given aspect ratio.
		/// </summary>
		/// <param name="aspectWidth">The width of aspect ratio.</param>
		/// <param name="aspectHeight">The height of aspect ratio.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SetSizeWithRatio(double aspectWidth, double aspectHeight)
		{
			double segment = SkiaElement.RenderSize.Width / aspectWidth;
			double ratio = Height / SkiaElement.RenderSize.Height;
			Height = segment * aspectHeight * ratio;
		}

		/// <summary>
		/// Get font file path.
		/// </summary>
		/// <param name="isArtist">Is artist path</param>
		/// <returns>The font file path</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetFontPath(bool isArtist)
		{
			return isArtist ?

				IO.Path.Combine(Directories["Font"],
					StringUtility.GetString(Config.TitleFontFilePath, Config.ArtistFontFilePath)) :

				IO.Path.Combine(Directories["Font"],
					StringUtility.GetString(Config.ArtistFontFilePath, Config.TitleFontFilePath));
		}
		
		

		/// <summary>
		/// Get font file path.
		/// </summary>
		/// <returns>The font file path</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetFontPath(string name)
		{
			return IO.Path.Combine(Directories["Font"], name);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SkiaElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			#region Capture

			if (IsCapture)
			{
				string filePath = IO.Path.Combine(
					Directories["Capture"],
					$"{DateTime.Now.Ticks}.png"
				);

				e.Surface.Snapshot().ToBitmap().Save(
					filePath,
					System.Drawing.Imaging.ImageFormat.Png
				);

				Logger.LogMessage($"Capture!{Environment.NewLine}SaveAt: {filePath}");
				IsCapture = false;
				return;
			}

			#endregion

			//// SHIT WARNING ////

			#region Initialize

			var canvas = e.Surface.Canvas;
			var size = e.Info.Size;

			#endregion

			#region Preload

			canvas.Clear(); // Clear the Canvas

			#endregion

			Logger.LogMessage("Draw!");

			#region Draw Image

			#region Draw Background

			var bgAvgColor = CurrentBackground.GetAvgColor().WithAlpha(128); // Background average color
			canvas.DrawColor(bgAvgColor); // Draw background color

			float scale = 0.9f; // Cover back diamond scale

			float width = size.Width; // Canvas width
			float height = size.Height; // Canvas height

			// Trace.WriteLine($"Canvas: {width} / {height} = {width / height}");

			float halfWidth = width / 2f; // Half Canvas width
			float halfHeight = height / 2f; // Half Canvas width
			canvas.DrawVerticesAspectWithBackground(
				new SKPoint[]
				{
					new(halfWidth, halfHeight + halfHeight * scale), // Top
					new(halfWidth + halfHeight * scale, halfHeight), // Right
					new(halfWidth, halfHeight - halfHeight * scale), // Left
					new(halfWidth - halfHeight * scale, halfHeight), // Bottom
				},
				size, // Cover back diamond scale
				CurrentBackground,
				backgroundBlurSigma: 2f,
				maskBlurSigma: 5f,
				verticeColor: bgAvgColor
			);

			#endregion

			#region Draw Cover

			var avgColor = CurrentCover.GetAvgColor(); // Cover average color
			float coverScale = height / 512f;

			// float coverOffset = 256 * coverScale * scale * 0.65f;
			float coverOffsetY = -35 * coverScale * scale;
			var coverRect = new SKRect() // Create a new rect
				.GetOffseted(halfWidth, halfHeight + coverOffsetY) // Offset the rect
				.EnwidenRect(150 * coverScale); // Enwiden the rect

			// Draw blurred rect to the canvas
			canvas.DrawRect(coverRect, Helper.GetBlurPaint(avgColor, 15));
			// Draw cover
			canvas.DrawBitmap(CurrentCover, coverRect);

			#endregion

			#region Draw Info Text

			string title = CurrentSong.GetTitle(Config.Localized, Config.Difficult);
			string artist = CurrentSong.GetArtist(Config.Localized, Config.Difficult);

			// Get the paint for drawing to the canvas
			using var titlePaint = Helper.GetSongInfoPaint(45, GetFontPath(false));
			using var artistPaint = Helper.GetSongInfoPaint(35, GetFontPath(true));

			// Shadow width
			float textWidth = MathF.Max(
				titlePaint.MeasureText(title),
				artistPaint.MeasureText(artist)
			);

			// Text width scale

			// Calculate the ratio of the width of the canvas to the actual text width and limit it to 1
			float textXScale = MathF.Min(width / (textWidth * coverScale), 1);
			titlePaint.TextScaleX = textXScale;
			artistPaint.TextScaleX = textXScale;
			textWidth *= textXScale;

			// Multiply the text size by the cover scale
			titlePaint.TextSize *= coverScale;
			artistPaint.TextSize *= coverScale;

			// Calculate the offset of text and text shadow
			float textWidthOffset = (textWidth / 2 + 20) * coverScale;
			float textShadowOffsetTop = 119f * coverScale;
			float textShadowOffsetBottom = 220f * coverScale;

			// Draw text back shadow to the canvas
			canvas.DrawRect(
				new(
					halfWidth - textWidthOffset,
					halfHeight + textShadowOffsetTop,
					halfWidth + textWidthOffset,
					halfHeight + textShadowOffsetBottom
				),
				new()
				{
					Shader = SKShader.CreateLinearGradient(
						new(
							halfWidth - textWidthOffset,
							halfHeight + textShadowOffsetTop
						), 
						new(
							halfWidth + textWidthOffset,
							halfHeight + textShadowOffsetTop
						),
						new SKColor[]
						{
							SKColor.Empty,
							SKColors.Black,
							SKColor.Empty
						},
						SKShaderTileMode.Clamp
					)
				}
			);

			// Calculate the text positions for drawing the info texts to the canvas
			SKPoint titleTextPos = new(halfWidth, halfHeight + 160 * coverScale);
			SKPoint artistTextPos = new(halfWidth, halfHeight + 205 * coverScale);

			float textOffset = 3 * coverScale;
			using var textImgFilter = SKImageFilter.CreateDropShadow(
				 textOffset, textOffset, 0, 0, SKColors.Black.WithAlpha(64));

			titlePaint.ImageFilter = textImgFilter;
			artistPaint.ImageFilter = textImgFilter;

			// Draw song info texts to the canvas
			canvas.DrawText(title, titleTextPos, titlePaint);
			canvas.DrawText(artist, artistTextPos, artistPaint);

			#endregion

			#region Draw Difficulty

			// Get difficulty color
			var diffColor = CustomDifficultColor ?? DifficultColors[Config.Difficult];
			float diffSize = 65 * coverScale;

			// Set difficulty center position
			SKPoint diffPos = new(halfWidth, halfHeight);
			diffPos.Offset(150 * coverScale, -(35 + 135) * coverScale);

			// Draw difficulty diamond
			canvas.DrawDiamond(
				diffSize,
				diffPos,
				diffColor.WithAlpha(175),
				diffColor.ChangeBrightness(0.5f).WithAlpha(175),
				SKBitmap.FromImage(e.Surface.Snapshot()),
				size,
				5f
			);

			// Get specified difficulty class from current song by config.difficult
			var diff = CurrentSong.FindDifficult(Config.Difficult) ?? new();
			// Get a type face for drawing the difficulty text
			using var diffTextTypeface = Helper.GetFont(
				Directories["Font"], 
				"Fonts.Exo-SemiBold.ttf",
				Config.DifficultyFontFilePath);
			// Offset the position of the center point for drawing the difficulty text
			diffPos.Offset(0, 45 * coverScale / 2f);

			string difficutyString = string.IsNullOrEmpty(Config.CustomDifficultString) ?
					diff.RatingString :
					Config.CustomDifficultString;

            Logger.LogMessage(difficutyString);

            // Drawing the difficulty text
            canvas.DrawTextWithOutline(
				difficutyString,
				diffPos, 
				diffColor.WithAlpha(64).ChangeBrightness(1.25f),
				10 * coverScale * Config.CustomDifficultyTextScale,
				new()
				{
					Typeface = diffTextTypeface,
					TextSize = 55 * coverScale * Config.CustomDifficultyTextScale,
					IsAntialias = true,
					TextAlign = SKTextAlign.Center,
					Color = SKColors.White
				}
			);

			#endregion

			#region Draw Top Title

			string topTitleString = Config.TopTitle ?? "";

			if (string.IsNullOrWhiteSpace(topTitleString))
			{
				return;
			}

			using var topTitleTypeFace = SKTypeface.FromStream(
				Helper.GetStreamFromExecutingAssembly("Fonts.Exo-SemiBold.ttf"));

			var internalTextPaint = new SKPaint()
			{
				Typeface = topTitleTypeFace,
				TextAlign = SKTextAlign.Left,
				IsAntialias = true,
				Color = SKColors.White,
				TextSize = 35 * coverScale
			};

			var topTitleStringWidth = internalTextPaint.MeasureText(topTitleString);
			var topTitleOffset = Config.TopTitleOffset;

			// Drawing Top Title Back
			canvas.DrawVerticesAspectWithBackground(
				new SKPoint[]
				{
					// Left Top
					new(0,
						0),
					// Left Bottom
					new(0,
						(55 + topTitleOffset.Y) * coverScale),
					// Right Bottom
					new(topTitleStringWidth + (topTitleOffset.X * coverScale),
						(55 + topTitleOffset.Y) * coverScale),
					// Right Top
					new(topTitleStringWidth + (55 + topTitleOffset.X + topTitleOffset.Y) * coverScale,
						0),
				},
				size,
				SKBitmap.FromImage(e.Surface.Snapshot()),
				verticeColor: bgAvgColor,
				maskBlurSigma: 25f
			);

			// Drawing Top Title Text
			canvas.DrawTextWithOutline(
				topTitleString,
				( 
				// Calculate the position of the top title text (string)
					(new Vector2(8, 38) + 
					topTitleOffset + 
					Config.TopTitleTextOffset) * coverScale
				).ToSKPoint(),
				bgAvgColor.WithAlpha(64).ChangeBrightness(1.25f),
				5 * coverScale,
				internalTextPaint
			);

			#endregion

			#endregion

			// End of SkiaElement_PaintSurface
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SecurityZone_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			var size = e.Info.Size;

			canvas.Clear();

			EnableSecurityZone = !EnableSecurityZone;

			if (!EnableSecurityZone)
			{
				return;
			}

			var securityZoneSize = GetWindowSizeInternal(Config.SecurityZoneAspect);
			float halfWidth = securityZoneSize.X / 2;
			float halfCanvasWidth = size.Width / 2;

			float ratio = size.Height / securityZoneSize.Y;

			float realLeftOffset = halfCanvasWidth - halfWidth * ratio;
			float realRightOffset = halfCanvasWidth + halfWidth * ratio;

			ColorUtility.TryParseSKColor(Config.CustomSecurityZoneColorHex, out var customColor);
			var securityZoneColor = customColor ?? CurrentBackground.GetAvgColor().Reverse();
			byte alpha = (byte)Math.Clamp(Config.SecurityZoneColorAlpha, 0, 255);

			canvas.DrawVertices(
				SKVertexMode.TriangleFan, 
				new SKPoint[]
				{
					new(realLeftOffset, size.Height),
					new(realRightOffset, size.Height), 
					new(realRightOffset, 0),
					new(realLeftOffset, 0),
				}, 
				Enumerable.Repeat(
					securityZoneColor.WithAlpha(alpha),
					4
				)
				.ToArray(), 
				new SKPaint()
				{
					IsAntialias = true
				}
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void HotkeyTarget(object sender, KeyEventArgs e)
		{
			HotkeyConfig.CheckHotkey(
				  () => Reload(), "Reload", e);
			HotkeyConfig.CheckHotkey(
				  () => Capture(), "Capture", e);
			HotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(16, 10), "RatioBili", e);
			HotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(16, 9), "RatioYtb", e);
			HotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(4, 3), "Ratio4:3", e);
			HotkeyConfig.CheckHotkey(
				  () => SecurityZone.InvalidateVisual(), "SwitchSecurityZone", e);
		}
	}
}
