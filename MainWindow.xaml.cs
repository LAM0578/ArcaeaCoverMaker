using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using IO = System.IO;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;
using ArcaeaCoverMaker.Hotkey;
using SkiaSharp.Views.WPF;

#pragma warning disable 8600, 8601, 8604
namespace ArcaeaCoverMaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Reload();
		}

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
		private static Config Config = new();
		private static HotkeyConfig HotkeyConfig = new();

		private ArcSong CurrentSong = new();

		private static List<SKBitmap> InternalBackgrounds = new();

		private static SKBitmap CurrentBackground = new();
		private static SKBitmap CurrentCover = new();

		private bool IsCapture;

		private static readonly SKColor[] DifficultColors = new SKColor[]
		{
			new SKColor(0x0a,0x82,0xbe,255),
			new SKColor(0x64,0x8c,0x3c,255),
			new SKColor(0x50,0x19,0x4b,255),
			new SKColor(0x82,0x23,0x28,255),
		};

		private static SKBitmap CheckBacgroundExist(string path, string sourcePath)
		{
			using var fs = IO.File.Exists(path) ?
				IO.File.OpenRead(path) : GetStreamFromExecutingAssembly(sourcePath);
			return SKBitmap.Decode(fs);
		}

		private static void LoadInternal()
		{
			InternalBackgrounds = new()
			{
				CheckBacgroundExist(
					 IO.Path.Combine(Directories["Background"], "base_light.jpg"),
					 "Backgrounds.internal_bg_light.png"),
				CheckBacgroundExist(
					 IO.Path.Combine(Directories["Background"], "base_conflict.jpg"),
					 "Backgrounds.internal_bg_conflict.png")
			};
		}

		private void CheckDirectories()
		{
			foreach (var item in Directories)
			{
				IO.Directory.CreateDirectory(item.Value);
			}
		}

		/// <summary>
		/// Load files.
		/// </summary>
		private void Load()
		{
			//// File ////
			if (!IO.File.Exists(SonglistPath))
				IO.File.WriteAllText(SonglistPath, null);

			if (!IO.File.Exists(ConfigPath) ||
				string.IsNullOrEmpty(IO.File.ReadAllText(ConfigPath)))
				IO.File.WriteAllText(ConfigPath,
					  JsonConvert.SerializeObject(new Config(), Formatting.Indented));
			//////////////

			// Load songlist
			Songlist = Utility.TryDeserializeObject<ArcSonglist>(IO.File.ReadAllText(SonglistPath));

			// Load config
			Config = Utility.TryDeserializeObject<Config>(IO.File.ReadAllText(ConfigPath));
			HotkeyConfig = Config.HotkeyConfig;

			CoverBitmaps.Clear();
			BackgroundBitmaps.Clear();

			Config.Difficult = (Config.Difficult < 0 || Config.Difficult > 3) ? 0 : Config.Difficult;

			// Load cover images from song folder
			if (Songlist.Songs != null)
			{
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
			}

			// Load background images
			foreach (var path in IO.Directory.GetFiles(Directories["Background"])
				.Where(t => t.ToLower().EndsWith(".jpg")))
			{
				using var fs = IO.File.OpenRead(path);
				BackgroundBitmaps.Add(
					   IO.Path.GetFileNameWithoutExtension(path), SKBitmap.Decode(fs));
			}

			string backgroundName = CurrentSong.GetBackgroundName(Config.Difficult);
			BackgroundBitmaps.TryGetValue(backgroundName, out CurrentBackground);
			if (string.IsNullOrEmpty(backgroundName))
			{
				CurrentBackground = InternalBackgrounds[CurrentSong.Side == 1 ? 1 : 0];
			}

			CoverBitmaps.TryGetValue(CurrentSong.AsciiId ?? "", out CurrentCover);

			// Print load info
			Trace.WriteLine($"CoverBitmaps.Count: {CoverBitmaps.Count}\n" +
				$"BackgroundBitmaps.Count: {BackgroundBitmaps.Count}");

			SkiaElement.InvalidateVisual();
		}

		/// <summary>
		/// Reload and re-draw canvas.
		/// </summary>
		private void Reload()
		{
			CheckDirectories();
			LoadInternal();
			Load();
			GC.Collect();
		}

		/// <summary>
		/// Capture surface content and save.
		/// </summary>
		private void Capture()
		{
			IsCapture = true;
			SkiaElement.InvalidateVisual();
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

		private void SkiaElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			if (IsCapture)
			{
				e.Surface.Snapshot().ToBitmap().Save(
					IO.Path.Combine(
						Directories["Capture"],
						$"{DateTime.Now.Ticks}.png"
					),
					System.Drawing.Imaging.ImageFormat.Png
				);
				IsCapture = false;
				return;
			}

			// SHIT WARNING //
			//  GGGGG,Lf,   .ift:GCCCG
			// 8@@@8G           .80000C
			// 8@00..::,     .,.: ;000G
			//  ;8: C0C:1, .:t;C01 10.
			//  .,   .:       ,,.   i
			//  f       .            ,
			// i0t    . ,.  ;:       80
			// C08     f.,. :.L     ,00
			// C0000,.  .illi     ;0000
			// 8000@0008:    .i00000000
			//           .    ,
			//          iG8  ttL
			//          .t,, it,

			// Initialize //
			var canvas = e.Surface.Canvas;
			var size = e.Info.Size;
			////////////////

			canvas.Clear();

			if (CurrentCover == null)
			{
				CurrentCover = new(512, 512);
				CurrentCover.Erase(SKColors.White);
			}
			CurrentSong ??= new();

			// Draw image! //
			Trace.WriteLine("Draw!");
			var bgAvgColor = CurrentBackground.GetAvgColor().WithAlpha(128);
			canvas.DrawColor(bgAvgColor);

			float scale = 0.9f;

			float width = size.Width;
			float height = size.Height;

			// Trace.WriteLine($"Canvas: {width} / {height} = {width / height}");

			float halfWidth = width / 2f;
			float halfHeight = height / 2f;
			canvas.DrawVerticesAspectWithBackground(
				new SKPoint[]
				{
					new(halfWidth, halfHeight + halfHeight * scale),
					new(halfWidth + halfHeight * scale, halfHeight),
					new(halfWidth, halfHeight - halfHeight * scale),
					new(halfWidth - halfHeight * scale, halfHeight),
				},
				size,
				CurrentBackground,
				backgroundBlurSigma: 2f,
				maskBlurSigma: 5f,
				verticeColor: bgAvgColor
			);

			var avgColor = CurrentCover.GetAvgColor();
			float coverScale = height / 512f;

			// float coverOffset = 256 * coverScale * scale * 0.65f;
			float coverOffsetY = -35 * coverScale * scale;
			var coverRect = new SKRect()
				.GetOffseted(halfWidth, halfHeight + coverOffsetY)
				.EnwidenRect(150 * coverScale);

			canvas.DrawRect(coverRect, new()
			{
				Color = avgColor,
				ImageFilter = SKImageFilter.CreateBlur(15, 15),
				IsAntialias = true
			});
			canvas.DrawBitmap(CurrentCover, coverRect);

			// Draw Text

			string title = CurrentSong.GetTitle(Config.Localized, Config.Difficult);
			string artist = CurrentSong.GetArtist(Config.Localized, Config.Difficult);

			using var titlePaint = new SKPaint()
			{
				Color = SKColors.White,
				TextSize = 45,
				Typeface = SKTypeface.FromFile(
					IO.Path.Combine(Directories["Font"],
					StringUtility.GetString(Config.TitleFontFilePath, Config.ArtistFontFilePath))),
				TextAlign = SKTextAlign.Center,
				IsAntialias = true
			};
			using var artistPaint = titlePaint.Clone();
			artistPaint.Typeface = SKTypeface.FromFile(
				IO.Path.Combine(Directories["Font"],
				StringUtility.GetString(Config.ArtistFontFilePath, Config.TitleFontFilePath)));
			artistPaint.TextSize = 35;
			float textWidth = MathF.Max(
				titlePaint.MeasureText(title),
				artistPaint.MeasureText(artist)
			);

			float textXScale = MathF.Min(width / (textWidth * coverScale), 1);
			titlePaint.TextScaleX = textXScale;
			artistPaint.TextScaleX = textXScale;
			textWidth *= textXScale;

			titlePaint.TextSize *= coverScale;
			artistPaint.TextSize *= coverScale;

			float textWidthOffset = (textWidth / 2 + 20) * coverScale;
			float textShadowOffsetTop = 119f * coverScale;
			float textShadowOffsetBottom = 220 * coverScale;

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

			SKPoint titleTextPos = new(halfWidth, halfHeight + 160 * coverScale);
			SKPoint artistTextPos = new(halfWidth, halfHeight + 205 * coverScale);

			float textOffset = 3 * coverScale;
			using var textImgFilter = SKImageFilter.CreateDropShadow(
				 textOffset, textOffset, 0, 0, SKColors.Black.WithAlpha(64));

			titlePaint.ImageFilter = textImgFilter;
			artistPaint.ImageFilter = textImgFilter;

			canvas.DrawText(title, titleTextPos, titlePaint);
			canvas.DrawText(artist, artistTextPos, artistPaint);

			float diffSize = 65 * coverScale;
			SKPoint diffPos = new(halfWidth, halfHeight);
			diffPos.Offset(150 * coverScale, -(35 + 135) * coverScale);
			canvas.DrawDiamond(
				diffSize,
				diffPos,
				DifficultColors[Config.Difficult].WithAlpha(175),
				DifficultColors[Config.Difficult].ChangeBrightness(0.5f).WithAlpha(175),
				SKBitmap.FromImage(e.Surface.Snapshot()),
				size,
				5f
			);
			var diff = CurrentSong.FindDifficult(Config.Difficult) ?? new();
			using var diffTextTypeface = SKTypeface.FromStream(
				GetStreamFromExecutingAssembly("Fonts.Exo-SemiBold.ttf")
			);
			diffPos.Offset(0, 45 * coverScale / 2f);

			canvas.DrawTextWithOutline(
				string.IsNullOrEmpty(Config.CustomDifficultString) ? 
					diff == null ? "?" : diff.RatingStr : Config.CustomDifficultString, 
				diffPos, 
				DifficultColors[Config.Difficult].WithAlpha(64).ChangeBrightness(1.25f),
				10 * coverScale,
				new()
				{
					Typeface = diffTextTypeface,
					TextSize = 55 * coverScale,
					IsAntialias = true,
					TextAlign = SKTextAlign.Center,
					Color = SKColors.White
				}
			);

			// Draw Title

			string topTitle = Config.TopTitle ?? "";
			// Trace.WriteLine(Config.TopTitle);

			var internalTextPaint = new SKPaint()
			{
				Typeface = diffTextTypeface,
				TextAlign = SKTextAlign.Left,
				IsAntialias = true,
				Color = SKColors.White,
				TextSize = 35 * coverScale
			};

			var topTitleSize = internalTextPaint.MeasureText(topTitle);

			canvas.DrawVerticesAspectWithBackground(
				new SKPoint[]
				{
					new(0,0),
					new(0,55 * coverScale),
					new(topTitleSize,55 * coverScale),
					new(topTitleSize + 55 * coverScale,0),
				},
				size,
				SKBitmap.FromImage(e.Surface.Snapshot()),
				verticeColor: bgAvgColor,
				maskBlurSigma: 5f
			);

			canvas.DrawTextWithOutline(
				topTitle,
				new SKPoint(8 * coverScale, 38 * coverScale),
				bgAvgColor.WithAlpha(64).ChangeBrightness(1.25f),
				5 * coverScale,
				internalTextPaint
			);
			/////////////////

			// End of SkiaElement_PaintSurface
		}

		private void HotkeyTarget(object sender, KeyEventArgs e)
		{
			HotkeyConfig.CheckHotkey(
				  () => Reload(), "Reload", e);
			HotkeyConfig.CheckHotkey(
				  () => Capture(), "Capture", e);
			HotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(16, 10), "RatioBili", e);
			HotkeyConfig.CheckHotkey(
				  () => SetSizeWithRatio(16,9), "RatioYtb", e);
		}

		private static IO.Stream? GetStreamFromExecutingAssembly(string path)
		{
			return Assembly.GetExecutingAssembly()
				.GetManifestResourceStream("ArcaeaCoverMaker.Resources." + path);
		}
	}
}
