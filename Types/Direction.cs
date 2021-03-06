﻿using System;
using Rhino.Geometry;

namespace Monoceros {
    /// <summary>
    /// Part face direction consisting of <see cref="Monoceros.Axis"/> and
    /// <see cref="Monoceros.Orientation"/>.
    /// </summary>
    [Serializable]
    public struct Direction {
        private readonly Axis axis;
        private readonly Orientation orientation;

        public Axis Axis => axis;

        public Orientation Orientation => orientation;

        public Direction(Axis axis, Orientation orientation) {
            this.axis = axis;
            this.orientation = orientation;
        }

        public static bool FromVector (Vector3d vector, out Direction direction) {
            if (vector.Unitize()) {
                if (vector.X == 1 && vector.Y == 0 && vector.Z == 0) {
                    direction = new Direction(Axis.X, Orientation.Positive);
                    return true;
                }
                if (vector.X == -1 && vector.Y == 0 && vector.Z == 0) {
                    direction = new Direction(Axis.X, Orientation.Negative);
                    return true;
                }
                if (vector.X == 0 && vector.Y == 1 && vector.Z == 0) {
                    direction = new Direction(Axis.Y, Orientation.Positive);
                    return true;
                }
                if (vector.X == 0 && vector.Y == -1 && vector.Z == 0) {
                    direction = new Direction(Axis.Y, Orientation.Negative);
                    return true;
                }
                if (vector.X == 0 && vector.Y == 0 && vector.Z == 1) {
                    direction = new Direction(Axis.Z, Orientation.Positive);
                    return true;
                }
                if (vector.X == 0 && vector.Y == 0 && vector.Z == -1) {
                    direction = new Direction(Axis.Z, Orientation.Negative);
                    return true;
                }
            }
            direction = new Direction();
            return false;
        }

        /// <summary>
        /// Determines whether the other <see cref="Direction"/> is opposite to
        /// the current. A <see cref="Direction"/> is opposite when the
        /// <see cref="Monoceros.Axis"/> equals and
        /// <see cref="Monoceros.Orientation"/> does not.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>True if opposite.</returns>
        public bool IsOpposite(Direction other) {
            return Axis == other.Axis &&
                Orientation != other.Orientation;
        }

        /// <summary>
        /// Returns flipped <see cref="Direction"/> - the
        /// <see cref="Monoceros.Axis"/> remains the same, the
        /// <see cref="Monoceros.Orientation"/> flips.
        /// </summary>
        /// <returns>A flipped Direction.</returns>
        public Direction ToFlipped( ) {
            var flipped = new Direction(Axis,
                                              Orientation == Orientation.Positive ?
                                              Orientation.Negative :
                                              Orientation.Positive);
            return flipped;
        }

        /// <summary>
        /// Converts the <see cref="Direction"/> to a <see cref="Vector3d"/> in
        /// Cartesian coordinate system.
        /// </summary>
        /// <returns>A Vector3d.</returns>
        public Vector3d ToVector( ) {
            if (Axis == Axis.X && Orientation == Orientation.Positive) {
                return Vector3d.XAxis;
            }
            if (Axis == Axis.Y && Orientation == Orientation.Positive) {
                return Vector3d.YAxis;
            }
            if (Axis == Axis.Z && Orientation == Orientation.Positive) {
                return Vector3d.ZAxis;
            }
            if (Axis == Axis.X && Orientation == Orientation.Negative) {
                var directionVector = Vector3d.XAxis;
                directionVector.Reverse();
                return directionVector;
            }
            if (Axis == Axis.Y && Orientation == Orientation.Negative) {
                var directionVector = Vector3d.YAxis;
                directionVector.Reverse();
                return directionVector;
            }
            if (Axis == Axis.Z && Orientation == Orientation.Negative) {
                var directionVector = Vector3d.ZAxis;
                directionVector.Reverse();
                return directionVector;
            }
            return Vector3d.Unset;
        }

        /// <summary>
        /// Converts the <see cref="Direction"/> to a part connector index,
        /// according to the convention: (partIndex * 6) + faceIndex, where
        /// faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5. This method is the
        /// source of truth.
        /// </summary>
        /// <returns>Part connector index.</returns>
        public uint DirectionToSingleModuleConnectorIndex() {
            // Connector numbering convention: 
            // faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
            if (Axis == Axis.X && Orientation == Orientation.Positive) {
                return 0;
            }
            if (Axis == Axis.Y && Orientation == Orientation.Positive) {
                return 1;
            }
            if (Axis == Axis.Z && Orientation == Orientation.Positive) {
                return 2;
            }
            if (Axis == Axis.X && Orientation == Orientation.Negative) {
                return 3;
            }
            if (Axis == Axis.Y && Orientation == Orientation.Negative) {
                return 4;
            }
            if (Axis == Axis.Z && Orientation == Orientation.Negative) {
                return 5;
            }
            // Never
            return uint.MaxValue;
        }

    }

    /// <summary>
    /// Part face axis within a grid: <c>X</c> or <c>Y</c> or <c>Z</c>.
    /// </summary>
    [Serializable]
    public enum Axis : uint {
        // TODO(yan): Bring back the AdjacencyRule type from the Rust solver in
        // the solver component and make conversion functions between this one
        // and that.
        //
        // Note that this type is sent across FFI and its memory layout must be
        // exactly the same as the one defined in C API headers for the Rust
        // solver. It is dangerous to believe we control this from
        // here. Theferore it would be best if the raw FFI type would be defined
        // in Solver.cs, close to the FFI bindings. Conversions can be defined
        // between Axis (which will no longer have to be uint) and
        // AdjacencyKind.
        X = 0,
        Y = 1,
        Z = 2,
    }

    /// <summary>
    /// Part face orientation within a grid: <c>Positive</c> or <c>Negative</c>.
    /// </summary>
    [Serializable]
    public enum Orientation {
        Positive,
        Negative
    }
}
