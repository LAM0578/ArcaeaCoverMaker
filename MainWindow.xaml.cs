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
using System.Threading.Tasks;

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
				s_logger.LogError(ex.Message, ex);
			}
		}

		private static void CreateLogger()
		{
			string logDir = IO.Path.Combine(AppContext.BaseDirectory, "Logs");
			IO.Directory.CreateDirectory(logDir);

			string logPath = IO.Path.Combine(logDir, $"log_{DateTime.Now.Ticks}.log");
			s_logger = Logger.CreateLogger(logPath);
		}

		private static Logger s_logger;

		private static readonly string s_appBaseDirectory = AppContext.BaseDirectory;
		private static readonly Dictionary<string, string> s_directories = new()
		{
			["Background"] = IO.Path.Combine(s_appBaseDirectory, "Background"),
			["Song"] = IO.Path.Combine(s_appBaseDirectory, "Songs"),
			["Font"] = IO.Path.Combine(s_appBaseDirectory, "Fonts"),
			["Capture"] = IO.Path.Combine(s_appBaseDirectory, "Captures")
		};

		private static readonly string s_configPath = IO.Path.Combine(s_appBaseDirectory, "Config.json");
		private static readonly string s_songlistPath = IO.Path.Combine(s_appBaseDirectory, "Songlist.json");

		private static ArcSonglist _songlist = new();
		private static CoverMakerConfig _config = new();
		private static HotkeyConfig _hotkeyConfig = new();

		private ArcSong _currentSong = new();

		private static List<SKBitmap> s_bulitinBackgrounds = new();

		private static SKBitmap s_currentBackground = new();
		private static SKBitmap s_currentCover = new();

		private bool _isCapture;
		private bool _enableSecurityZone;

		private static SKColor? s_customDifficultColor = null;
		private static readonly SKColor[] s_difficultColors = new SKColor[]
		{
			new SKColor(0x0a,0x82,0xbe,255), // Past
			new SKColor(0x64,0x8c,0x3c,255), // Present
			new SKColor(0x50,0x19,0x4b,255), // Future
			new SKColor(0x82,0x23,0x28,255), // Beyond
			new SKColor(0x5d,0x4e,0x76,255)  // Eternal
		};

		private static void CheckDirectories()
		{
			foreach (var item in s_directories)
			{
				IO.Directory.CreateDirectory(item.Value);
			}
		}

		private static void WriteDataIfNotExists(string path, string? data)
		{
			try
			{
				if (!IO.File.Exists(path) || string.IsNullOrEmpty(IO.File.ReadAllText(path)))
					IO.File.WriteAllText(path, data);
			}
			catch (Exception ex)
			{
				s_logger.LogError(ex.Message, ex);
			}
		}
		
		private static void LoadInternalBackgrounds()
		{
			s_bulitinBackgrounds = new()
			{
				Helper.SetBulitinBacgroundIfExist(
					 IO.Path.Combine(s_directories["Background"], "base_light.jpg"),
					 "Backgrounds.internal_bg_light.png"),
				Helper.SetBulitinBacgroundIfExist(
					 IO.Path.Combine(s_directories["Background"], "base_conflict.jpg"),
					 "Backgrounds.internal_bg_conflict.png")
			};
		}

		/// <summary>
		/// Unload all skia bitmap resources.
		/// </summary>
		private void Unload()
		{
			if (s_currentCover.Width != 0 && s_currentCover.Height != 0)
			{
				s_currentCover.Dispose();
				s_currentCover = new();
			}
			if (s_currentBackground.Width != 0 && s_currentBackground.Height != 0)
			{
				s_currentBackground.Dispose();
				s_currentBackground = new();
			}
		}

		/// <summary>
		/// Load config from file and load images from files.
		/// </summary>
		private void Load()
		{
			//// File ////
			
			WriteDataIfNotExists(s_songlistPath, null);
			WriteDataIfNotExists(s_configPath, 
				  JsonConvert.SerializeObject(new CoverMakerConfig(), Formatting.Indented));

			//////////////

			Unload();

			// Load songlist
			_songlist = Utility.TryDeserializeObject<ArcSonglist>(IO.File.ReadAllText(s_songlistPath));

			// Load config
			_config = Utility.TryDeserializeObject<CoverMakerConfig>(IO.File.ReadAllText(s_configPath));
			_hotkeyConfig = _config.HotkeyConfig;

			_config.Difficult = (_config.Difficult < 0 || _config.Difficult > 4) ? 0 : _config.Difficult;
			ColorUtility.TryParseSKColor(_config.CustomDifficultColorHex, out s_customDifficultColor);
			
			_currentSong = _songlist.FindSong(
				_config.LastSelectedSongTitle, _config.LastSelectedSongIndex, _config.Difficult);
			
			// Load background
			var backgroundPath = _currentSong.GetBackgroundFileName(_config.Difficult);
			if (backgroundPath != "" &&
			    IO.File.Exists(backgroundPath = IO.Path.Combine(s_directories["Background"], backgroundPath + ".jpg")))
			{
				using var fs = IO.File.OpenRead(backgroundPath);
				s_currentBackground = SKBitmap.Decode(fs);
			}
			else
			{
				s_currentBackground = s_bulitinBackgrounds[_currentSong.Side == 1 ? 1 : 0];
			}
			
			// Load cover (jacket)
			var jacketName = _currentSong.GetJacketFileName(_config.Difficult);
			string jacketFilePath;
			// Songs/{CurrentSongId}/1080_{jacketName}.jpg
			if (!IO.File.Exists(jacketFilePath = combineJacketPath(jacketName, true, false)))
			{
				// Songs/{CurrentSongId}/1080_{jacketName}_256.jpg
				if (!IO.File.Exists(jacketFilePath = combineJacketPath(jacketName, true, true)))
				{
					// Songs/{CurrentSongId}/{jacketName}.jpg
					if (!IO.File.Exists(jacketFilePath = combineJacketPath(jacketName, false, false)))
					{
						// Songs/{CurrentSongId}/{jacketName}_256.jpg
						jacketFilePath = combineJacketPath(jacketName, false, true);
					}
				}
			}
			
			if (IO.File.Exists(jacketFilePath))
			{
				using var fs = IO.File.OpenRead(jacketFilePath);
				s_currentCover = SKBitmap.Decode(fs);
			}

			SkiaElement.InvalidateVisual();

			_enableSecurityZone = !_enableSecurityZone;
			SecurityZone.InvalidateVisual();
			
			return;
			
			string combineJacketPath(string jacketFileName, bool isHighQuality, bool is256)
			{
				string result = jacketFileName;
				if (isHighQuality) result = result.Insert(0, "1080_");
				if (is256) result += "_256";
				return IO.Path.Combine(s_directories["Song"], _currentSong.AsciiId, result + ".jpg");
			}
		}

		/// <summary>
		/// Reload files and re-draw the canvas.
		/// </summary>
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
		private void Capture()
		{
			_isCapture = true;
			SkiaElement.InvalidateVisual();
		}

		private Vector2 GetWindowSize(Vector2 aspect)
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
		private static string GetFontPath(bool isArtist)
		{
			return isArtist ?

				IO.Path.Combine(s_directories["Font"],
					StringUtility.GetString(_config.TitleFontFilePath, _config.ArtistFontFilePath)) :

				IO.Path.Combine(s_directories["Font"],
					StringUtility.GetString(_config.ArtistFontFilePath, _config.TitleFontFilePath));
		}
		

		private void SkiaElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			#region Capture

			if (_isCapture)
			{
				string filePath = IO.Path.Combine(
					s_directories["Capture"],
					$"{DateTime.Now.Ticks}.png"
				);

				e.Surface.Snapshot().ToBitmap().Save(
					filePath,
					System.Drawing.Imaging.ImageFormat.Png
				);

				s_logger.LogMessage($"Capture!{Environment.NewLine}SaveAt: {filePath}");
				_isCapture = false;
				return;
			}

			#endregion

			#region Initialize

			var canvas = e.Surface.Canvas;
			var size = e.Info.Size;

			#endregion

			#region Preload

			canvas.Clear(); // Clear the Canvas

			#endregion

			s_logger.LogMessage("Draw!");

			#region Draw Image

			#region Draw Background

			var bgAvgColor = s_currentBackground.GetAvgColor().WithAlpha(128); // Background average color
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
				s_currentBackground,
				backgroundBlurSigma: 2f,
				maskBlurSigma: 5f,
				verticeColor: bgAvgColor
			);

			#endregion

			#region Draw Cover
			
			var avgColor = s_currentCover.GetAvgColor(); // Cover average color
			float coverScale = height / 512f;

			// float coverOffset = 256 * coverScale * scale * 0.65f;
			float coverOffsetY = -35 * coverScale * scale;
			var coverRect = new SKRect() // Create a new rect
				.GetOffseted(halfWidth, halfHeight + coverOffsetY) // Offset the rect
				.EnwidenRect(150 * coverScale); // Enwiden the rect

			// Draw blurred rect to the canvas
			canvas.DrawRect(coverRect, Helper.GetBlurPaint(avgColor, 15));
			// Draw cover
			if (s_currentCover.Width != 0 && s_currentCover.Height != 0)
				canvas.DrawBitmap(s_currentCover, coverRect);

			#endregion

			#region Draw Info Text

			string title = _currentSong.GetTitle(_config.Localized, _config.Difficult);
			string artist = _currentSong.GetArtist(_config.Localized, _config.Difficult);

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
			var diffColor = s_customDifficultColor ?? s_difficultColors[_config.Difficult];
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
			var diff = _currentSong.FindDifficult(_config.Difficult) ?? new();
			// Get a type face for drawing the difficulty text
			using var diffTextTypeface = Helper.GetFont(
				s_directories["Font"], 
				"Fonts.Exo-SemiBold.ttf",
				_config.DifficultyFontFilePath);
			// Offset the position of the center point for drawing the difficulty text
			diffPos.Offset(0, 45 * coverScale / 2f);

			string difficutyString = string.IsNullOrEmpty(_config.CustomDifficultString) ?
					diff.RatingString :
					_config.CustomDifficultString;

            s_logger.LogMessage(difficutyString);

            // Drawing the difficulty text
            canvas.DrawTextWithOutline(
				difficutyString,
				diffPos, 
				diffColor.WithAlpha(64).ChangeBrightness(1.25f),
				10 * coverScale * _config.CustomDifficultyTextScale,
				new()
				{
					Typeface = diffTextTypeface,
					TextSize = 55 * coverScale * _config.CustomDifficultyTextScale,
					IsAntialias = true,
					TextAlign = SKTextAlign.Center,
					Color = SKColors.White
				}
			);

			#endregion

			#region Draw Top Title

			string topTitleString = _config.TopTitle ?? "";

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
			var topTitleOffset = _config.TopTitleOffset;

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
					_config.TopTitleTextOffset) * coverScale
				).ToSKPoint(),
				bgAvgColor.WithAlpha(64).ChangeBrightness(1.25f),
				5 * coverScale,
				internalTextPaint
			);

			#endregion

			#endregion

			// End of SkiaElement_PaintSurface
		}

		private void SecurityZone_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			var size = e.Info.Size;

			canvas.Clear();

			_enableSecurityZone = !_enableSecurityZone;

			if (!_enableSecurityZone)
			{
				return;
			}

			var securityZoneSize = GetWindowSize(_config.SecurityZoneAspect);
			float halfWidth = securityZoneSize.X / 2;
			float halfCanvasWidth = size.Width / 2f;

			float ratio = size.Height / securityZoneSize.Y;

			float realLeftOffset = halfCanvasWidth - halfWidth * ratio;
			float realRightOffset = halfCanvasWidth + halfWidth * ratio;

			ColorUtility.TryParseSKColor(_config.CustomSecurityZoneColorHex, out var customColor);
			var securityZoneColor = customColor ?? s_currentBackground.GetAvgColor().Reverse();
			byte alpha = (byte)Math.Clamp(_config.SecurityZoneColorAlpha, 0, 255);

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

		private void HotkeyTarget(object sender, KeyEventArgs e)
		{
			_hotkeyConfig.CheckHotkey(
				  () => Reload(), "Reload", e);
			_hotkeyConfig.CheckHotkey(
				  () => Capture(), "Capture", e);
			_hotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(16, 10), "RatioBili", e);
			_hotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(16, 9), "RatioYtb", e);
			_hotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(4, 3), "Ratio4:3", e);
			_hotkeyConfig.CheckHotkey(
				  () => SecurityZone.InvalidateVisual(), "SwitchSecurityZone", e);
		}
	}
}
