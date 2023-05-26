using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ArcaeaCoverMaker.Logging;
using SkiaSharp;

namespace ArcaeaCoverMaker.Util
{
	public static class Helper
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Stream? GetStreamFromExecutingAssembly(string path)
		{
			return Assembly.GetExecutingAssembly()
				.GetManifestResourceStream("ArcaeaCoverMaker.Resources." + path);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKBitmap SetInternalBacgroundIfExist(string path, string sourcePath)
		{
			using var fs = File.Exists(path) ?
				File.OpenRead(path) : GetStreamFromExecutingAssembly(sourcePath);
			return SKBitmap.Decode(fs);
		}

		/// <summary>
		/// Get the blur paint of drawing image.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="blurSigma"></param>
		/// <returns>The blur paint of drawing image.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKPaint GetBlurPaint(SKColor color, float blurSigma)
		{
			return new SKPaint()
			{
				Color = color,
				ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma),
				IsAntialias = true
			};
		}

		/// <summary>
		/// Get the paint of drawing song info text.
		/// </summary>
		/// <param name="textSize"></param>
		/// <param name="fontFilePath"></param>
		/// <returns>The paint of drawing song info text.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKPaint GetSongInfoPaint(float textSize, string fontFilePath)
		{
			return new SKPaint()
			{
				Color = SKColors.White,
				TextSize = textSize,
				Typeface = SKTypeface.FromFile(fontFilePath),
				TextAlign = SKTextAlign.Center,
				IsAntialias = true
			};
		}
	}
}
