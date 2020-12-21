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
        public Axis Axis;
        public Orientation Orientation;

        public bool IsOpposite(Direction other)
        {
            return Axis == other.Axis &&
                Orientation != other.Orientation;
        }

        public Direction ToFlipped()
        {
            var flipped = new Direction()
            {
                Axis = Axis,
                Orientation = Orientation == Orientation.Positive ? Orientation.Negative : Orientation.Positive
            };
            return flipped;
        }

        public override string ToString()
        {
            return Orientation.ToString("g") + Axis.ToString("g");
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
            Vector3d x = basePlane.XAxis;
            x.Unitize();
            if (vector.EpsilonEquals(x, EPSILON))
            {
                return new Direction
                {
                    Axis = Axis.X,
                    Orientation = Orientation.Positive
                };
            };
            Vector3d y = basePlane.YAxis;
            y.Unitize();
            if (vector.EpsilonEquals(y, EPSILON))
            {
                return new Direction
                {
                    Axis = Axis.Y,
                    Orientation = Orientation.Positive
                };
            }
            Vector3d z = basePlane.ZAxis;
            z.Unitize();
            if (vector.EpsilonEquals(z, EPSILON))
            {
                return new Direction
                {
                    Axis = Axis.Z,
                    Orientation = Orientation.Positive
                };
            }
            Vector3d ix = basePlane.XAxis;
            ix.Unitize();
            ix.Reverse();
            if (vector.EpsilonEquals(ix, EPSILON))
            {
                return new Direction
                {
                    Axis = Axis.X,
                    Orientation = Orientation.Negative
                };
            }
            Vector3d iy = basePlane.YAxis;
            iy.Unitize();
            iy.Reverse();
            if (vector.EpsilonEquals(iy, EPSILON))
            {
                return new Direction
                {
                    Axis = Axis.Y,
                    Orientation = Orientation.Negative
                };
            }
            Vector3d iz = basePlane.ZAxis;
            iz.Unitize();
            iz.Reverse();
            if (vector.EpsilonEquals(iz, EPSILON))
            {
                return new Direction
                {
                    Axis = Axis.Z,
                    Orientation = Orientation.Negative
                };

            }
            else
            {
                throw new Exception("The axis cannot be determined from the vector");
            }

        }

        public bool ToVector(out Vector3d directionVector)
        {
            if (Axis == Axis.X && Orientation == Orientation.Positive)
            {
                directionVector = Vector3d.XAxis;
                return true;
            }
            if (Axis == Axis.Y && Orientation == Orientation.Positive)
            {
                directionVector = Vector3d.YAxis;
                return true;
            }
            if (Axis == Axis.Z && Orientation == Orientation.Positive)
            {
                directionVector = Vector3d.ZAxis;
                return true;
            }
            if (Axis == Axis.X && Orientation == Orientation.Negative)
            {
                directionVector = Vector3d.XAxis;
                return directionVector.Reverse();
            }
            if (Axis == Axis.Y && Orientation == Orientation.Negative)
            {
                directionVector = Vector3d.YAxis;
                return directionVector.Reverse();
            }
            if (Axis == Axis.Z && Orientation == Orientation.Negative)
            {
                directionVector = Vector3d.ZAxis;
                return directionVector.Reverse();
            }
            directionVector = Vector3d.Unset;
            return false;
        }

    }
}