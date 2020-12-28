// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Rhino.Geometry;

namespace WFCToolset
{
    /// <summary>
    /// Submodule face direction within a grid. 
    /// </summary>
    public enum Axis
    {
        X,
        Y,
        Z
    }
    public enum Orientation
    {
        Positive,
        Negative
    }

    public struct Direction
    {
        public Axis _axis;
        public Orientation _orientation;

        public bool IsOpposite(Direction other)
        {
            return _axis == other._axis &&
                _orientation != other._orientation;
        }

        public Direction ToFlipped()
        {
            var flipped = new Direction()
            {
                _axis = _axis,
                _orientation = _orientation == Orientation.Positive ? Orientation.Negative : Orientation.Positive
            };
            return flipped;
        }

        public override string ToString()
        {
            return _orientation.ToString("g") + _axis.ToString("g");
        }

        /// <summary>
        /// Detect orthogonal direction of a vector relative to the base plane. 
        /// </summary>
        /// <param name="vector">
        /// Vector to be checked.
        /// </param>
        /// <param name="basePlane">
        /// Base plane defining XYZ and inverted XYZ directions.
        /// </param>
        public static Direction FromDirectionVector(Vector3d vector, Plane basePlane)
        {
            const double EPSILON = Rhino.RhinoMath.SqrtEpsilon;
            vector.Unitize();
            var x = basePlane.XAxis;
            x.Unitize();
            if (vector.EpsilonEquals(x, EPSILON))
            {
                return new Direction
                {
                    _axis = Axis.X,
                    _orientation = Orientation.Positive
                };
            };
            var y = basePlane.YAxis;
            y.Unitize();
            if (vector.EpsilonEquals(y, EPSILON))
            {
                return new Direction
                {
                    _axis = Axis.Y,
                    _orientation = Orientation.Positive
                };
            }
            var z = basePlane.ZAxis;
            z.Unitize();
            if (vector.EpsilonEquals(z, EPSILON))
            {
                return new Direction
                {
                    _axis = Axis.Z,
                    _orientation = Orientation.Positive
                };
            }
            var ix = basePlane.XAxis;
            ix.Unitize();
            ix.Reverse();
            if (vector.EpsilonEquals(ix, EPSILON))
            {
                return new Direction
                {
                    _axis = Axis.X,
                    _orientation = Orientation.Negative
                };
            }
            var iy = basePlane.YAxis;
            iy.Unitize();
            iy.Reverse();
            if (vector.EpsilonEquals(iy, EPSILON))
            {
                return new Direction
                {
                    _axis = Axis.Y,
                    _orientation = Orientation.Negative
                };
            }
            var iz = basePlane.ZAxis;
            iz.Unitize();
            iz.Reverse();
            if (vector.EpsilonEquals(iz, EPSILON))
            {
                return new Direction
                {
                    _axis = Axis.Z,
                    _orientation = Orientation.Negative
                };

            }
            else
            {
                throw new Exception("The axis cannot be determined from the vector");
            }

        }

        public Vector3d ToVector()
        {
            if (_axis == Axis.X && _orientation == Orientation.Positive)
            {
                return Vector3d.XAxis;
            }
            if (_axis == Axis.Y && _orientation == Orientation.Positive)
            {
                return Vector3d.YAxis;
            }
            if (_axis == Axis.Z && _orientation == Orientation.Positive)
            {
                return Vector3d.ZAxis;
            }
            if (_axis == Axis.X && _orientation == Orientation.Negative)
            {
                var directionVector = Vector3d.XAxis;
                directionVector.Reverse();
                return directionVector;
            }
            if (_axis == Axis.Y && _orientation == Orientation.Negative)
            {
                var directionVector = Vector3d.YAxis;
                directionVector.Reverse();
                return directionVector;
            }
            if (_axis == Axis.Z && _orientation == Orientation.Negative)
            {
                var directionVector = Vector3d.ZAxis;
                directionVector.Reverse();
                return directionVector;
            }
            return Vector3d.Unset;
        }

        public int ToConnectorIndex()
        {
            // Connector numbering convention: (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
            if (_axis == Axis.X && _orientation == Orientation.Positive)
            {
                return 0;
            }
            if (_axis == Axis.Y && _orientation == Orientation.Positive)
            {
                return 1;
            }
            if (_axis == Axis.Z && _orientation == Orientation.Positive)
            {
                return 2;
            }
            if (_axis == Axis.X && _orientation == Orientation.Negative)
            {
                return 3;
            }
            if (_axis == Axis.Y && _orientation == Orientation.Negative)
            {
                return 4;
            }
            if (_axis == Axis.Z && _orientation == Orientation.Negative)
            {
                return 5;
            }
            return -1;
        }

    }
}
