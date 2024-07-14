using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using SkiaSharp;

#pragma warning disable 8625, 8602
namespace ArcaeaCoverMaker.Util
{
	internal static class Utility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BitmapSource ToBitmapSource(this Image image)
		{
			return (BitmapSource)image.Source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? ReadSource<T>(object key)
		{
			var obj = (T)Application.Current.TryFindResource(key);
			return obj ?? default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T TryDeserializeObject<T>(string json) where T : new()
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(json) ?? new();
			}
			catch (Exception)
			{
				return new();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Exists<TKey, TValue>(this Dictionary<TKey, TValue>? dict, TValue item) where TKey : notnull
		{
			return dict != null && dict.ContainsValue(item);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DrawVerticesAspectWithBackground(
			this SKCanvas canvas,
			SKPoint[] vertices,
			SKSizeI baseSize,
			SKBitmap background,
			SKColor? verticeColor = null,
			float backgroundBlurSigma = 0,
			float maskBlurSigma = 0
		)
		{
			if (vertices?.Length == 0 || background == null) return;

			//// Calculate aspect size of background ////

			float scale = SkiaSharpUtility.GetAspectRatio(baseSize, new SKSizeI(background.Width, background.Height), true);
			SKPoint aspSize = new(background.Width * scale, background.Height * scale);

			// Calculate aspect size of background end //

			backgroundBlurSigma *= scale;
			maskBlurSigma *= scale;

			//// Calculate rect of background ////

			float rLeft = (baseSize.Width - aspSize.X) / 2f;
			float rTop = (baseSize.Height - aspSize.Y) / 2f;
			// float dBgSigma = backgroundBlurSigma * 2;
			var bgRect = SkiaSharpUtility.GetRect(
				new(rLeft, rTop),
				new(rLeft + aspSize.X, rTop + aspSize.Y)
			);

			// Calculate rect of background end //

			//// Draw vertices and mask ////

			SKBitmap maskBitmap = new(new(baseSize.Width, baseSize.Height));
			using var maskCanvas = new SKCanvas(maskBitmap);
			using var maskPaint = new SKPaint()
			{
				ImageFilter = SKImageFilter.CreateBlur(
					backgroundBlurSigma + maskBlurSigma,
					backgroundBlurSigma + maskBlurSigma
				),
				IsAntialias = true
			};

			SKRect maskRect = bgRect;

			// Clip mask canvas

			using SKPath maskPath = new();
			foreach (var vert in vertices)
			{
				maskPath.LineTo(vert);
			}
			maskPath.LineTo(vertices[0]);

			maskCanvas.ClipPath(maskPath);

			// Clip mask canvas end

			maskCanvas.DrawBitmap(background, maskRect, maskPaint);

			if (verticeColor != null)
			{
				var vertColors = Enumerable.Repeat(verticeColor.Value, vertices.Length).ToArray();
				maskCanvas.DrawVertices(
					 SKVertexMode.TriangleFan, vertices, vertColors.ToArray(), new SKPaint()
					 {
						 IsAntialias = true
					 }
				);
			}

			// Draw vertices and mask end //
			
			canvas.DrawBitmap(background, bgRect, new()
			{
				ImageFilter = SKImageFilter.CreateBlur(backgroundBlurSigma, backgroundBlurSigma)
			});
			canvas.DrawBitmap(maskBitmap, 0, 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DrawDiamond(
			this SKCanvas canvas,
			float size,
			SKPoint pos,
			SKColor top,
			SKColor bottom,
			SKBitmap background = null,
			SKSizeI? baseSize = null,
			float? blurSigma = null
		)
		{
			SKPoint[] topVerts = new SKPoint[]
			{
				new(pos.X, pos.Y - size),
				new(pos.X - size, pos.Y),
				new(pos.X + size, pos.Y),
			};
			SKPoint[] bottomVerts = new SKPoint[]
			{
				new(pos.X, pos.Y + size),
				new(pos.X - size, pos.Y),
				new(pos.X + size, pos.Y),
			};

			var topVertColors = Enumerable.Repeat(top, 3).ToArray();
			var bottomVertColors = Enumerable.Repeat(bottom, 3).ToArray();

			if (background != null && baseSize != null)
			{
				using var path = new SKPath();
				path.LineTo(pos.X, pos.Y - size);
				path.LineTo(pos.X - size, pos.Y);
				path.LineTo(pos.X, pos.Y + size);
				path.LineTo(pos.X + size, pos.Y);
				path.LineTo(pos.X, pos.Y - size);

				float scale = SkiaSharpUtility.GetAspectRatio(
					  baseSize.Value, new SKSize(background.Width, background.Height), true);

				blurSigma ??= 3.5f;
				blurSigma *= scale;

				canvas.Save();
				canvas.ClipPath(path);
				canvas.DrawBitmap(
					background,
					background.GetRect(),
					new()
					{
						ImageFilter = SKImageFilter.CreateBlur(blurSigma.Value, blurSigma.Value)
					}
				);
				canvas.Restore();
			}

			canvas.DrawVertices(
				 SKVertexMode.Triangles, topVerts, topVertColors, new());
			canvas.DrawVertices(
				 SKVertexMode.Triangles, bottomVerts, bottomVertColors, new());
		}
	}
	public static class SkiaSharpUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetAspectRatio(SKSize a, SKSize b, bool useMax)
		{
			float aspx = a.Width / b.Width;
			float aspy = a.Height / b.Height;
			return useMax ? MathF.Max(aspx, aspy) : MathF.Min(aspx, aspy);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetAspectRatio(SKSizeI a, SKSizeI b, bool useMax)
		{
			float aspx = a.Width / (float)b.Width;
			float aspy = a.Height / (float)b.Height;
			return useMax ? MathF.Max(aspx, aspy) : MathF.Min(aspx, aspy);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKRect GetRect(SKPoint leftTop, SKPoint rightBottom, SKPoint offset = new())
		{
			return new SKRect(
				leftTop.X + offset.X,
				leftTop.Y + offset.Y,
				rightBottom.X + offset.X,
				rightBottom.Y + offset.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKRect EnwidenRect(this SKRect rect, float x)
		{
			return new SKRect(
				rect.Left - x,
				rect.Top - x,
				rect.Right + x,
				rect.Bottom + x
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKRect GetOffseted(this SKRect rect, float x, float y)
		{
			return new SKRect(
				rect.Left + x,
				rect.Top + y,
				rect.Right + x,
				rect.Bottom + y
			);
		}

		/// <summary>
		/// Get the size of the <see cref="SKBitmap">current bitmap</see> and
		/// return it as a <see cref="SKRect"/>.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKRect GetRect(this SKBitmap bitmap)
		{
			return new SKRect(0, 0, bitmap.Width, bitmap.Height);
		}

		/// <summary>
		/// Get the <see cref="SKColor">average color</see> from the <see cref="SKBitmap">current bitmap</see>.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKColor GetAvgColor(this SKBitmap bitmap)
		{
			if (bitmap.Width == 0 || bitmap.Height == 0) return SKColors.Empty;
			using var newBitmap = bitmap.Resize(new SKImageInfo(1, 1), SKFilterQuality.High);
			return newBitmap.Pixels[0];
		}

		/// <summary>
		/// Change the <see cref="SKColor">current color</see> brightness and 
		/// return the <see cref="SKColor">changed color</see>.
		/// </summary>
		/// <param name="color"></param>
		/// <param name="scale"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKColor ChangeBrightness(this SKColor color, float scale)
		{
			color.ToHsl(out var h, out var s, out var l);
			return SKColor.FromHsl(h, s, l * scale, color.Alpha);
		}

		/// <summary>
		/// Read the image from a file and return it as a <see cref="SKBitmap"/>.
		/// (Return null if the file does not exist.)
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKBitmap? ReadBitmapFrom(string path)
		{
			if (!File.Exists(path)) return null;
			using var fs = File.OpenRead(path);
			return SKBitmap.Decode(fs);
		}

		/// <summary>
		/// Draw text with outline on the canvas at the specified coordinates.
		/// </summary>
		/// <param name="canvas"></param>
		/// <param name="text"></param>
		/// <param name="point"></param>
		/// <param name="outlineColor"></param>
		/// <param name="outlineWidth"></param>
		/// <param name="paint"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DrawTextWithOutline(
			this SKCanvas canvas, string text, SKPoint point, SKColor outlineColor, float outlineWidth = float.NaN, SKPaint paint = null)
		{
			// Use a new paint if the paint is null
			paint ??= new();
			SKColor textColor = paint.Color;

			// Draw outline if outlineWidth is not NaN
			if (!float.IsNaN(outlineWidth))
			{
				paint.StrokeWidth = outlineWidth;
				paint.Style = SKPaintStyle.Stroke;
				paint.Color = outlineColor;
				canvas.DrawText(text, point.X, point.Y, paint);
			}

			// Draw text
			paint.Style = SKPaintStyle.Fill;
			paint.Color = textColor;
			canvas.DrawText(text, point.X, point.Y, paint);
		}
	}
	
	public static class ColorUtility
	{
		private static readonly char[] HexChars = "0123456789abcdefABCDEF".ToCharArray();
		private static readonly NumberStyles HexNumberStyle = NumberStyles.HexNumber;
		private static readonly CultureInfo HexProvider = CultureInfo.CurrentCulture;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryParseSKColor(string? hex, out SKColor? outColor)
		{
			hex = hex?.GetIntersect(HexChars);
			if (string.IsNullOrEmpty(hex) || hex.Length < 6)
			{
				outColor = null;
				return false;
			}

			if (!byte.TryParse(
				hex[0..2], HexNumberStyle, HexProvider, out var red))
			{
				outColor = null;
				return false;
			}
			if (!byte.TryParse(
				hex[2..4], HexNumberStyle, HexProvider, out var blue))
			{
				outColor = null;
				return false;
			}
			if (!byte.TryParse(
				hex[4..6], HexNumberStyle, HexProvider, out var green))
			{
				outColor = null;
				return false;
			}

			outColor = new SKColor(red, green, blue);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKColor Reverse(this SKColor color)
		{
			return new SKColor(
				(byte)(byte.MaxValue - color.Red),
				(byte)(byte.MaxValue - color.Green),
				(byte)(byte.MaxValue - color.Blue),
				color.Alpha
			);
		}
	}

	public static class StringUtility
	{
		/// <summary>
		/// Return <see cref="string"/> "b" if <see cref="string"/> "a" <see cref="string.IsNullOrEmpty">is null or empty</see>.
		/// (Comparison result will return <see cref="string.Empty"/> if both are null.)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static string GetString(string? a, string? b)
		{
			return (string.IsNullOrEmpty(a) ? b : a) ?? "";
		}

		/// <summary>
		/// Return all eligible characters from the string.
		/// </summary>
		/// <param name="base"></param>
		/// <param name="compare"></param>
		/// <returns>The eligible characters from the string.</returns>
		public static string GetIntersect(this string @base, char[] compare)
			=> new string(@base?.ToList().FindAll(compare.Contains).ToArray());
	}
}
