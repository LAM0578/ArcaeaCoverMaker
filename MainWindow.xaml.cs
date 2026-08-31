using System;
using System.Linq;
using System.Windows;
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
                var args = Environment.GetCommandLineArgs();
                bool noInfoEditor = args.Contains("--no-info-editor");
                if (!noInfoEditor)
                {
                    _infoEditorWindow = new InfoEditor();
                    _infoEditorWindow.Show();
                }
                Instance = this;
                InitializeComponent();
                Reload();
                if (!noInfoEditor)
                {
                    _infoEditorWindow.Init();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

        private static void CreateLogger()
        {
            string logDir = IO.Path.Combine(AppContext.BaseDirectory, "Logs");
            IO.Directory.CreateDirectory(logDir);

            string logPath = IO.Path.Combine(logDir, $"log_{DateTime.Now.Ticks}.log");
            _logger = Logger.CreateLogger(logPath);
        }

        public static MainWindow Instance { get; private set; }

        private static Logger _logger;
        private static InfoEditor _infoEditorWindow;

        private static readonly string _appBaseDirectory = AppContext.BaseDirectory;
        private static readonly Dictionary<string, string> _directories = new()
        {
            ["Background"] = IO.Path.Combine(_appBaseDirectory, "Background"),
            ["Song"] = IO.Path.Combine(_appBaseDirectory, "Songs"),
            ["Font"] = IO.Path.Combine(_appBaseDirectory, "Fonts"),
            ["Capture"] = IO.Path.Combine(_appBaseDirectory, "Captures")
        };

        private static readonly string _configPath = IO.Path.Combine(_appBaseDirectory, "Config.json");
        private static readonly string _songlistPath = IO.Path.Combine(_appBaseDirectory, "Songlist.json");

        private static ArcSonglist _songlist = new();
        private static CoverMakerConfig _config = new();
        private static HotkeyConfig _hotkeyConfig = new();

        private ArcSong _currentSong = new();

        private static List<SKBitmap> s_bulitinBackgrounds = [];

        private static SKBitmap s_currentBackground = new();
        private static SKBitmap s_currentCover = new();

        private bool _isCapture;
        private bool _enableSecurityZone;

        public static CoverMakerConfig Config => _config;

        private static SKColor? s_customDifficultyColor = null;
        private static SKColor? s_customDifficultyTextOutlineColor = null;

        private static readonly SKColor[] s_difficultyColors =
        [
            new SKColor(0x39, 0x6A, 0x77, 0xFF), // Past
            new SKColor(0x55, 0x68, 0x47, 0xFF), // Present
            new SKColor(0x47, 0x2B, 0x53, 0xFF), // Future
            new SKColor(0x7B, 0x1C, 0x2F, 0xFF), // Beyond
            new SKColor(0x42, 0x33, 0x54, 0xFF), // Eternal
        ];
        private static readonly SKColor s_difficultyInscribed = new SKColor(0x1F, 0x2C, 0x65, 0xFF);

        private static readonly SKColor[] s_difficultyTextColors =
        [
            new SKColor(0x16, 0x53, 0x65, 0xFF), // Past
            new SKColor(0x19, 0x4A, 0x08, 0xFF), // Present
            new SKColor(0x52, 0x18, 0x4D, 0xFF), // Future
            new SKColor(0x5A, 0x08, 0x13, 0xFF), // Beyond
            new SKColor(0x5D, 0x41, 0x76, 0xFF), // Eternal
        ];
        private static readonly SKColor s_difficultyTextInscribed = new SKColor(0x05, 0x34, 0x3C, 0xFF);


        private static void CheckDirectories()
        {
            foreach (var item in _directories)
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
                _logger.LogError(ex.Message, ex);
            }
        }

        private static void LoadInternalBackgrounds()
        {
            s_bulitinBackgrounds =
            [
                Helper.SetBulitinBacgroundIfExist(
                    IO.Path.Combine(_directories["Background"], "base_light.jpg"),
                    "Backgrounds.internal_bg_light.png"),

                Helper.SetBulitinBacgroundIfExist(
                    IO.Path.Combine(_directories["Background"], "base_conflict.jpg"),
                    "Backgrounds.internal_bg_conflict.png")
            ];
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

            WriteDataIfNotExists(_songlistPath, null);
            WriteDataIfNotExists(_configPath,
                JsonConvert.SerializeObject(new CoverMakerConfig(), Formatting.Indented));

            //////////////

            Unload();

            // Load songlist
            _songlist = Utility.TryDeserializeObject<ArcSonglist>(IO.File.ReadAllText(_songlistPath));

            // Load config
            _config = Utility.TryDeserializeObject<CoverMakerConfig>(IO.File.ReadAllText(_configPath));
            _hotkeyConfig = _config.HotkeyConfig;

            _config.EnsureDifficulty();
            ColorUtility.TryParseSKColor(_config.CustomDifficultColorHex, out s_customDifficultyColor);
            if (!ColorUtility.TryParseSKColor(_config.CustomDifficultTextOutlineColorHex, out s_customDifficultyTextOutlineColor))
            {
                s_customDifficultyTextOutlineColor = s_customDifficultyColor;
            }

            _currentSong = _songlist.FindSong(
                _config.LastSelectedSongTitle, _config.LastSelectedSongIndex, _config.Difficult);

            // Load background
            var backgroundPath = _currentSong.GetBackgroundFileName(_config.Difficult);
            if (backgroundPath != "" &&
                IO.File.Exists(backgroundPath = IO.Path.Combine(_directories["Background"], backgroundPath + ".jpg")))
            {
                using var fs = IO.File.OpenRead(backgroundPath);
                s_currentBackground = SKBitmap.Decode(fs);
            }
            else
            {
                s_currentBackground = s_bulitinBackgrounds[_currentSong.Side is 1 or 4 ? 1 : 0];
            }

            // Load cover (jacket)
            var songFolder = IO.Path.Combine(_directories["Song"], _currentSong.AsciiId ?? "");
            var (fullJacketPath, _) = _currentSong.GetJacketPath(
                songFolder,
                _config.Difficult
            );
            if (IO.File.Exists(fullJacketPath))
            {
                using var fs = IO.File.OpenRead(fullJacketPath);
                s_currentCover = SKBitmap.Decode(fs);
            }

            SkiaElement.InvalidateVisual();

            _enableSecurityZone = !_enableSecurityZone;
            SecurityZone.InvalidateVisual();
        }

        /// <summary>
        /// Save config to file and reload.
        /// </summary>
        public void SaveAndReload()
        {
            _logger.LogMessage("Save config file and reload.");
            var configJson = JsonConvert.SerializeObject(_config, Formatting.Indented);
            _logger.LogMessage(configJson);
            IO.File.WriteAllText(_configPath, configJson);
            Reload();
            _logger.LogMessage(JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

        /// <summary>
        /// Reload files and re-draw the canvas.
        /// </summary>
        public void Reload()
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
            return !isArtist ?
                IO.Path.Combine(_directories["Font"],
                    StringUtility.GetString(_config.TitleFontFilePath, _config.ArtistFontFilePath)) :
                IO.Path.Combine(_directories["Font"],
                    StringUtility.GetString(_config.ArtistFontFilePath, _config.TitleFontFilePath));
        }


        private void SkiaElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
        #region Capture

            if (_isCapture)
            {
                string filePath = IO.Path.Combine(
                    _directories["Capture"],
                    $"{DateTime.Now.Ticks}.png"
                );

                using var image = e.Surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = IO.File.OpenWrite(filePath);
                data.SaveTo(stream);

                _logger.LogMessage($"Capture!{Environment.NewLine}SaveAt: {filePath}");
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

            _logger.LogMessage("Draw!");

        #region Draw Image

        #region Draw Background

            var bgAvgColor = s_currentBackground.GetAvgColor().WithAlpha(128); // Background average color
            canvas.DrawColor(bgAvgColor, SKBlendMode.SrcOver); // Draw background color

            float scale = 0.9f; // Cover back diamond scale

            float width = size.Width; // Canvas width
            float height = size.Height; // Canvas height

            // Trace.WriteLine($"Canvas: {width} / {height} = {width / height}");

            float halfWidth = width / 2f; // Half Canvas width
            float halfHeight = height / 2f; // Half Canvas width
            canvas.DrawVerticesAspectWithBackground(
                [
                    new(halfWidth, halfHeight + halfHeight * scale), // Top
                    new(halfWidth + halfHeight * scale, halfHeight), // Right
                    new(halfWidth, halfHeight - halfHeight * scale), // Left
                    new(halfWidth - halfHeight * scale, halfHeight) // Bottom
                ],
                size, // Cover back diamond scale
                s_currentBackground,
                backgroundBlurSigma: 2f,
                maskBlurSigma: 5f,
                verticeColor: bgAvgColor,
                backgroundAlpha: _config.BackgroundAlpha
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
                canvas.DrawBitmap(
                    s_currentCover,
                    new SKRect(
                        0,
                        0,
                        s_currentCover.Width,
                        s_currentCover.Height),
                    coverRect,
                    SKSamplingOptions.Default
                );

        #endregion

        #region Draw Info Text

            string title = _currentSong.GetTitle(_config.Localized, _config.Difficult);
            string artist = _currentSong.GetArtist(_config.Localized, _config.Difficult);

            // Get the paint for drawing to the canvas
            var titlePaintGroup = Helper.GetSongInfoPaint(45, GetFontPath(false));
            var artistPaintGroup = Helper.GetSongInfoPaint(35, GetFontPath(true));
            
            using var titleFont = titlePaintGroup.font;
            using var artistFont = artistPaintGroup.font;

            using var titlePaint = titlePaintGroup.paint;
            using var artistPaint = artistPaintGroup.paint;

            // Shadow width
            float textWidth = MathF.Max(
                titleFont.MeasureText(title),
                artistFont.MeasureText(artist)
            );

            // Text width scale

            // Calculate the ratio of the width of the canvas to the actual text width and limit it to 1
            float textXScale = MathF.Min(width / (textWidth * coverScale), 1);
            titleFont.ScaleX = textXScale;
            artistFont.ScaleX = textXScale;
            textWidth *= textXScale;

            // Multiply the text size by the cover scale
            titleFont.Size *= coverScale;
            artistFont.Size *= coverScale;

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
                        [
                            SKColor.Empty,
                            SKColors.Black,
                            SKColor.Empty
                        ],
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
            canvas.DrawText(title, titleTextPos, SKTextAlign.Center, titleFont, titlePaint);
            canvas.DrawText(artist, artistTextPos, SKTextAlign.Center, artistFont, artistPaint);

        #endregion

        #region Draw Difficulty
            
            // Get specified difficulty class from current song by config.difficult
            var diff = _currentSong.FindDifficult(_config.Difficult) ?? new();
            
            var difficulty = _config.Difficult;
            var difficultyAlias = _config.DifficultAlias != 0 ? _config.DifficultAlias : diff.RatingClassAlias;
            var diffColor = s_customDifficultyColor ?? (
                difficulty == 3 && difficultyAlias == 1 ? s_difficultyInscribed : s_difficultyColors[difficulty]
            );
            var diffTextOutlineColor = s_customDifficultyTextOutlineColor ?? (
                difficulty == 3 && difficultyAlias == 1 ? s_difficultyTextInscribed : s_difficultyTextColors[difficulty]
            );
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

            // Get a type face for drawing the difficulty text
            using var diffTextTypeface = Helper.GetFont(
                _directories["Font"],
                "Fonts.Exo-SemiBold.ttf",
                _config.DifficultyFontFilePath);
            using var diffTextFont = new SKFont();
            diffTextFont.Typeface = diffTextTypeface;
            diffTextFont.Size = 55 * coverScale * _config.CustomDifficultyTextScale;
            
            // Offset the position of the center point for drawing the difficulty text
            diffPos.Offset(0, 45 * coverScale / 2f);

            string difficutyString = string.IsNullOrEmpty(_config.CustomDifficultString) ?
                diff.RatingString :
                _config.CustomDifficultString;

            _logger.LogMessage(difficutyString);

            using var diffTextPaint = SkiaSharpUtility.CreatePaint();

            // Drawing the difficulty text
            canvas.DrawTextWithOutline(
                difficutyString,
                diffPos,
                SKTextAlign.Center,
                diffTextFont,
                diffTextOutlineColor.WithAlpha(75).ChangeBrightness(1.25f),
                10 * coverScale * _config.CustomDifficultyTextScale,
                diffTextPaint
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

            using var internalTextFont = new SKFont();
            internalTextFont.Typeface = topTitleTypeFace;
            internalTextFont.Size = 35 * coverScale;
            using var internalTextPaint = SkiaSharpUtility.CreatePaint();

            var topTitleStringWidth = internalTextFont.MeasureText(topTitleString);
            var topTitleOffset = _config.TopTitleOffset;

            // Drawing Top Title Back
            canvas.DrawVerticesAspectWithBackground(
                [
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
                        0)
                ],
                size,
                SKBitmap.FromImage(e.Surface.Snapshot()),
                verticeColor: bgAvgColor,
                maskBlurSigma: 25f,
                backgroundAlpha: _config.BackgroundAlpha
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
                SKTextAlign.Left,
                internalTextFont,
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
                [
                    new(realLeftOffset, size.Height),
                    new(realRightOffset, size.Height),
                    new(realRightOffset, 0),
                    new(realLeftOffset, 0)
                ],
                Enumerable.Repeat(
                    securityZoneColor.WithAlpha(alpha),
                    4
                ).ToArray(),
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
                () => SetSizeWithRatio(16, 9), "Aspect16By9", e);
            _hotkeyConfig.CheckHotkey(
                () => SetSizeWithRatio(4, 3), "Aspect4By3", e);
            _hotkeyConfig.CheckHotkey(
                () => SecurityZone.InvalidateVisual(), "ToggleSecurityZone", e);
        }
    }
}
