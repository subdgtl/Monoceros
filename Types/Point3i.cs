// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Rhino.Geometry;

namespace WFCToolset
{
    /// <summary>
    /// Used as a relative discrete coordinate of a slot or a submodule center 
    /// in the orthogonal 3D voxel-like grid, which describes the WFC World.
    /// </summary>
    public struct Point3i
    {
        /// <summary>
        /// The X coordinate of a slot or submodule center 
        /// in the orthogonal voxel-like grid, which describes the WFC world.
        /// </summary>
        public readonly int X;
        /// <summary>
        /// The Y coordinate of a slot or submodule center 
        /// in the orthogonal voxel-like grid, which describes the WFC world.
        /// </summary>
        public readonly int Y;
        /// <summary>
        /// The Z coordinate of a slot or submodule center 
        /// in the orthogonal voxel-like grid, which describes the WFC world.
        /// </summary>
        public readonly int Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3i"/> class.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        public Point3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3i"/> 
        /// class from a <see cref="Point3d"/> instance.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <remarks>
        /// The double values of the <see cref="Point3d"/> coordinates will be rounded. 
        /// </remarks>
        public Point3i(Point3d point)
        {
            X = Convert.ToInt32(point.X);
            Y = Convert.ToInt32(point.Y);
            Z = Convert.ToInt32(point.Z);
        }

        /// <summary>
        /// Determines if two instances of <see cref="Point3i"/> are equal.
        /// </summary>
        /// <param name="obj">The other obj.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is Point3i other &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode()
        {
            var hashCode = 341329424;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        public static Point3i operator +(Point3i a, Point3i b)
        => new Point3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Point3i operator -(Point3i a, Point3i b)
        => new Point3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>
        /// Converts to <see cref="Point3d"/>.
        /// </summary>
        /// <returns>A Point3d.</returns>
        public Point3d ToPoint3d() => new Point3d(X, Y, Z);
    }
}
