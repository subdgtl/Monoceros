﻿using System;
using Rhino.Geometry;

namespace Monoceros {
    /// <summary>
    /// Used as a relative discrete coordinate of a slot or a part center in the
    /// orthogonal 3D voxel-like grid, which describes the Monoceros World.
    /// </summary>
    [Serializable]
    public struct Point3i {
        /// <summary>
        /// The X coordinate of a slot or part center in the orthogonal
        /// voxel-like grid, which describes the Monoceros world.
        /// </summary>
        public int X;
        /// <summary>
        /// The Y coordinate of a slot or part center in the orthogonal
        /// voxel-like grid, which describes the Monoceros world.
        /// </summary>
        public int Y;
        /// <summary>
        /// The Z coordinate of a slot or part center in the orthogonal
        /// voxel-like grid, which describes the Monoceros world.
        /// </summary>
        public int Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3i"/> class.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        public Point3i(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3i"/> class from a
        /// <see cref="Point3d"/> instance.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <remarks>
        /// The double values of the <see cref="Point3d"/> coordinates will be
        /// rounded. 
        /// </remarks>
        public Point3i(Point3d point) {
            X = Convert.ToInt32(point.X);
            Y = Convert.ToInt32(point.Y);
            Z = Convert.ToInt32(point.Z);
        }


        public bool FitsUshort( ) {
            return X >= ushort.MinValue && X <= ushort.MaxValue &&
                Y >= ushort.MinValue && Y <= ushort.MaxValue &&
                Z >= ushort.MinValue && Z <= ushort.MaxValue;
        }

        /// <summary>
        /// Determines if two instances of <see cref="Point3i"/> are equal.
        /// </summary>
        /// <param name="obj">The other obj.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is Point3i other &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            var hashCode = 341329424;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        public static Point3i operator +(Point3i a, Point3i b) {
            return new Point3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Point3i operator -(Point3i a, Point3i b) {
            return new Point3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        /// <summary>
        /// Converts to <see cref="Point3d"/>.
        /// </summary>
        /// <returns>A Point3d.</returns>
        public Point3d ToPoint3d( ) {
            return new Point3d(X, Y, Z);
        }

        /// <summary>
        /// Converts to <see cref="Vector3d"/>.
        /// </summary>
        /// <returns>A Vector3d.</returns>
        public Vector3d ToVector3d( ) {
            return new Vector3d(X, Y, Z);
        }

        /// <summary>
        /// Checks if two discrete points are neighbors in one of the 6
        /// directions in the discrete grid world.
        /// </summary>
        /// <param name="other">The other point.</param>
        /// <returns>True if neighbors</returns>
        public bool IsNeighbor(Point3i other) {
            return (Math.Abs(X - other.X) == 1 && Y == other.Y && Z == other.Z) ||
                (X == other.X && Math.Abs(Y - other.Y) == 1 && Z == other.Z) ||
                (X == other.X && Y == other.Y && (Math.Abs(Z - other.Z) == 1));
        }

        public Point3d ToCartesian(Plane basePlane, Vector3d diagonal) {
            var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            var scalingTransform = Transform.Scale(basePlane,
                                                   diagonal.X,
                                                   diagonal.Y,
                                                   diagonal.Z);

            var partCenter = ToPoint3d();
            partCenter.Transform(baseAlignmentTransform);
            partCenter.Transform(scalingTransform);
            return partCenter;
        }

        public static Point3i FromCartesian(Point3d point, Plane basePlane, Vector3d diagonal) {
            var baseAlignmentTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            var scalingTransform = Transform.Scale(Plane.WorldXY,
                                                   1 / diagonal.X,
                                                   1 / diagonal.Y,
                                                   1 / diagonal.Z);

            point.Transform(baseAlignmentTransform);
            point.Transform(scalingTransform);

            return new Point3i(point);
        }

    }
}
