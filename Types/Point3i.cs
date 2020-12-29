// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Rhino.Geometry;

namespace WFCToolset
{
    public struct Point3i
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Point3i(int item1, int item2, int item3)
        {
            X = item1;
            Y = item2;
            Z = item3;
        }

        public Point3i(Point3d point)
        {
            X = Convert.ToInt32(point.X);
            Y = Convert.ToInt32(point.Y);
            Z = Convert.ToInt32(point.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is Point3i other &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            var hashCode = 341329424;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        public Point3d ToPoint3d() => new Point3d(X, Y, Z);
    }
}
