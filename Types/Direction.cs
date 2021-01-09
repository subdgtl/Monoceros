using Rhino.Geometry;

namespace WFCPlugin {
    /// <summary>
    /// Submodule face direction consisting of <see cref="WFCPlugin.Axis"/> and
    /// <see cref="WFCPlugin.Orientation"/>.
    /// </summary>
    public struct Direction {
        private readonly Axis axis;
        private readonly Orientation orientation;

        public Axis Axis => axis;

        public Orientation Orientation => orientation;

        public Direction(Axis axis, Orientation orientation) {
            this.axis = axis;
            this.orientation = orientation;
        }

        /// <summary>
        /// Determines whether the other <see cref="Direction"/> is opposite to
        /// the current. A <see cref="Direction"/> is opposite when the
        /// <see cref="WFCPlugin.Axis"/> equals and <see cref="WFCPlugin.Orientation"/> does not.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>True if opposite.</returns>
        public bool IsOpposite(Direction other) {
            return Axis == other.Axis &&
                Orientation != other.Orientation;
        }

        /// <summary>
        /// Returns flipped <see cref="Direction"/> - the <see cref="WFCPlugin.Axis"/>
        /// remains the same, the <see cref="WFCPlugin.Orientation"/> flips.
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

    }

    /// <summary>
    /// Submodule face axis within a grid: <c>X</c> or <c>Y</c> or <c>Z</c>.
    /// </summary>
    public enum Axis : uint {
        X = 0,
        Y = 1,
        Z = 2,
    }

    /// <summary>
    /// Submodule face orientation within a grid: <c>Positive</c> or
    /// <c>Negative</c>.
    /// </summary>
    public enum Orientation {
        Positive,
        Negative
    }
}
