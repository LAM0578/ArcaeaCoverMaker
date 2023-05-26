using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Numerics;
using SkiaSharp;
using System.Windows.Documents;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace ArcaeaCoverMaker.Json
{
	[Serializable]
	public struct Vector2 : IEquatable<Vector2>, IFormattable
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2(float value) : this(value, value) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		/// <summary>
		/// The X component of the vector.
		/// </summary>
		[JsonProperty("x")]
		public float X;
		/// <summary>
		/// The Y component of the vector.
		/// </summary>
		[JsonProperty("y")]
		public float Y;

		/// <summary>
		/// Get or set the component value from this vector.
		/// </summary>
		/// <param name="idx">Index</param>
		/// <returns></returns>
		/// <exception cref="IndexOutOfRangeException"></exception>
		[JsonIgnore]
		public float this[float idx]
		{
			get => idx switch
			{
				0 => X,
				1 => Y,
				_ => throw new IndexOutOfRangeException()
			};
			set
			{
				switch (idx)
				{
					case 0:
						X = value;
						break;
					case 1:
						Y = value;
						break;
					default:
						throw new IndexOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Get a new Vector2 from the SKPoint.
		/// </summary>
		/// <param name="point"></param>
		/// <returns>Return a new Vector2 from the SKPoint.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 FromSKPoint(SKPoint point)
		{
			return new Vector2(point.X, point.Y);
		}

		/// <summary>
		/// Get a new SKPoint from the Vector2.
		/// </summary>
		/// <param name="vector"></param>
		/// <returns>Return a new SKPoint from the Vector2.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SKPoint ToSKPoint(Vector2 vector)
		{
			return new SKPoint(vector.X, vector.Y);
		}

		/// <summary>
		/// Get a new SKPoint from this Vector2.
		/// </summary>
		/// <returns>Return a new SKPoint from this Vector2.</returns>
		public SKPoint ToSKPoint()
		{
			return new SKPoint(X, Y);
		}

		#region operators

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator +(Vector2 left, Vector2 right)
		{
			return new Vector2(
				left.X + right.X,
				left.Y + right.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator +(Vector2 left, float right)
		{
			return new Vector2(
				left.X + right,
				left.Y + right
			);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator -(Vector2 left, Vector2 right)
		{
			return new Vector2(
				left.X - right.X,
				left.Y - right.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator -(Vector2 left, float right)
		{
			return new Vector2(
				left.X - right,
				left.Y - right
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator *(Vector2 left, Vector2 right)
		{
			return new Vector2(
				left.X * right.X,
				left.Y * right.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator *(Vector2 left, float right)
		{
			return new Vector2(
				left.X * right,
				left.Y * right
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator /(Vector2 left, Vector2 right)
		{
			return new Vector2(
				left.X / right.X,
				left.Y / right.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator /(Vector2 left, float right)
		{
			return new Vector2(
				left.X / right,
				left.Y / right
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator +(Vector2 vector)
		{
			return vector;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 operator -(Vector2 vector)
		{
			return new Vector2(-vector.X, -vector.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Vector2 left, Vector2 right)
		{
			return left.X.Equals(right.X) && left.Y.Equals(right.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Vector2 left, Vector2 right)
		{
			return !(left == right);
		}

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly string ToString()
		{
			return ToString("G", CultureInfo.CurrentCulture);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly string ToString(string? format)
		{
			return ToString(format, CultureInfo.CurrentCulture);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly string ToString(string? format, IFormatProvider? formatProvider)
		{
			var sb = new StringBuilder();
			string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
			sb.Append('(');
			sb.Append(X.ToString(format, formatProvider));
			sb.Append(separator);
			sb.Append(' ');
			sb.Append(Y.ToString(format, formatProvider));
			sb.Append(')');
			return sb.ToString();
		}

		/// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
		/// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Numerics.Vector3" /> object and their corresponding elements are equal.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly bool Equals([NotNullWhen(true)] object? obj)
		{
			return (obj is Vector2 other) && Equals(other);
		}

		/// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
		/// <param name="other">The other vector.</param>
		/// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
		/// <remarks>Two vectors are equal if their <see cref="X" />, <see cref="Y" /> elements are equal.</remarks>
		public readonly bool Equals(Vector2 other)
		{
			return this == other;
		}

		/// <summary>Returns the hash code for this instance.</summary>
		/// <returns>The hash code.</returns>
		public override readonly int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}
	}
}
