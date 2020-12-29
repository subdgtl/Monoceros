// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Rhino.Geometry;

namespace WFCToolset
{
    public struct Point3i
    {
        public int _x;
        public int _y;
        public int _z;

        public Point3i(int item1, int item2, int item3)
        {
            _x = item1;
            _y = item2;
            _z = item3;
        }

        public override bool Equals(object obj)
        {
            return obj is Point3i other &&
                   _x == other._x &&
                   _y == other._y &&
                   _z == other._z;
        }

        public override int GetHashCode()
        {
            var hashCode = 341329424;
            hashCode = hashCode * -1521134295 + _x.GetHashCode();
            hashCode = hashCode * -1521134295 + _y.GetHashCode();
            hashCode = hashCode * -1521134295 + _z.GetHashCode();
            return hashCode;
        }

        public Point3d ToPoint3d() => new Point3d(_x, _y, _z);
    }
}
