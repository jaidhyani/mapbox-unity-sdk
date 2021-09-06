﻿//-----------------------------------------------------------------------
// <copyright file="CanonicalTileId.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Text;

namespace Mapbox.Map
{
	using System;
	using Mapbox.Utils;

	/// <summary>
	/// Data type to store  <see href="https://en.wikipedia.org/wiki/Web_Mercator"> Web Mercator</see> tile scheme.
	/// <see href="http://www.maptiler.org/google-maps-coordinates-tile-bounds-projection/"> See tile IDs in action. </see>
	/// </summary>
	public struct CanonicalTileId : IEquatable<CanonicalTileId>
	{
		/// <summary> The zoom level. </summary>
		public readonly int Z;

		/// <summary> The X coordinate in the tile grid. </summary>
		public readonly int X;

		/// <summary> The Y coordinate in the tile grid. </summary>
		public readonly int Y;

		private string _stringCache;
		private string _fileNameStringCache;

		/// <summary>
		///     Initializes a new instance of the <see cref="CanonicalTileId"/> struct,
		///     representing a tile coordinate in a slippy map.
		/// </summary>
		/// <param name="z"> The z coordinate or the zoom level. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		public CanonicalTileId(int z, int x, int y)
		{
			this.Z = z;
			this.X = x;
			this.Y = y;
			_stringCache = "";
			_fileNameStringCache = "";
		}

		internal CanonicalTileId(UnwrappedTileId unwrapped)
		{
			var z = unwrapped.Z;
			var x = unwrapped.X;
			var y = unwrapped.Y;

			var wrap = (x < 0 ? x - (1 << z) + 1 : x) / (1 << z);

			this.Z = z;
			this.X = x - wrap * (1 << z);
			this.Y = y < 0 ? 0 : Math.Min(y, (1 << z) - 1);

			_stringCache = "";
			_fileNameStringCache = "";
		}

		public static CanonicalTileId FromUnwrappedValues(int z, int x, int y)
		{
			var wrap = (x < 0 ? x - (1 << z) + 1 : x) / (1 << z);
			return new CanonicalTileId(z, x - wrap * (1 << z), y < 0 ? 0 : Math.Min(y, (1 << z) - 1));
		}

		/// <summary>
		///     Get the cordinate at the top left of corner of the tile.
		/// </summary>
		/// <returns> The coordinate. </returns>
		public Vector2d ToVector2d()
		{
			double n = Math.PI - ((2.0 * Math.PI * this.Y) / Math.Pow(2.0, this.Z));

			double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
			double lng = (this.X / Math.Pow(2.0, this.Z) * 360.0) - 180.0;

			// FIXME: Super hack because of rounding issues.
			return new Vector2d(lat - 0.0001, lng + 0.0001);
		}

		/// <summary>
		///     Returns a <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.Map.CanonicalTileId"/>.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.Map.CanonicalTileId"/>.
		/// </returns>
		public override string ToString()
		{
			if(string.IsNullOrEmpty(_stringCache))
			{
				_stringCache = string.Format("{0}/{1}/{2}", this.Z, this.X, this.Y);
			}

			return _stringCache;
		}

		public string ToFileSafeString()
		{
			if (string.IsNullOrEmpty(_fileNameStringCache))
			{
				_fileNameStringCache = string.Format("{0}_{1}_{2}", this.Z, this.X, this.Y);
			}

			return _fileNameStringCache;
		}

		#region Equality 
		public bool Equals(CanonicalTileId other)
		{
			return this.X == other.X && this.Y == other.Y && this.Z == other.Z;
		}
		
		public override int GetHashCode()
		{
			//old hashcode
			//return X ^ Y ^ Z;

			int hash = X.GetHashCode();
			hash = (hash * 397) ^ Y.GetHashCode();
			hash = (hash * 397) ^ Z.GetHashCode();

			return hash;
		}

		public static bool operator ==(CanonicalTileId a, CanonicalTileId b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		public static bool operator !=(CanonicalTileId a, CanonicalTileId b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj is CanonicalTileId)
			{
				return this.Equals((CanonicalTileId)obj);
			}
			else
			{
				return false;
			}
		}

		#endregion

	}

	public static class TileIdExtensions
	{
		public static int GenerateKey(this CanonicalTileId tileId, string tilesetId)
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + "mapbox".GetHashCode();
				hash = hash * 23 + tilesetId.GetHashCode();
				hash = hash * 23 + tileId.GetHashCode();
				return hash;
			}
		}

		public static int GenerateKey(this CanonicalTileId tileId, string tilesetId, string third)
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + third.GetHashCode();
				hash = hash * 23 + tilesetId.GetHashCode();
				hash = hash * 23 + tileId.GetHashCode();
				return hash;
			}
		}
	}
}
