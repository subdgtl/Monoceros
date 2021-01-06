using System.Drawing;
using System.Linq;

namespace WFCPlugin {
    /// <summary>
    /// The WFC configuration and constants.
    /// </summary>
    public class Config {
        /// <summary>
        /// Reserved name for the "Empty" module.
        /// </summary>
        public const string EMPTY_MODULE_NAME = "empty";
        /// <summary>
        /// Reserved name for the "Out" module.
        /// </summary>
        public const string OUTER_MODULE_NAME = "out";
        /// <summary>
        /// Reserved name for the "Indifferent" connector type. Used in
        /// <see cref="RuleTyped"/>.
        /// </summary>
        public const string INDIFFERENT_TAG = "indifferent";

        /// <summary>
        /// Collected reserved module names.
        /// </summary>
        public static readonly string[] RESERVED_NAMES = { EMPTY_MODULE_NAME, OUTER_MODULE_NAME };

        /// <summary>
        /// User-friendly listing of the reserved module names.
        /// </summary>
        public static string RESERVED_TO_STRING => RESERVED_NAMES
            .Aggregate("", (accum, name) => accum + ", " + name);


        /// <summary>
        /// Preview and baking color a <see cref="Module"/> cage and
        /// <see cref="ModuleConnector.Face"/>.
        /// </summary>
        public static Color CAGE_COLOR = Color.FromArgb(128, 255, 255, 255);

        /// <summary>
        /// Preview and baking color an erroneous <see cref="Module"/> cage and
        /// <see cref="ModuleConnector.Face"/>.
        /// </summary>
        public static Color CAGE_ERROR_COLOR = Color.FromArgb(128, 255, 0, 0);

        /// <summary>
        /// Preview and baking color for <see cref="Slot"/>s with unknown
        /// entropy.
        /// </summary>
        public static Color CAGE_UNKNOWN_COLOR = Color.FromArgb(128, 0, 128, 255);

        /// <summary>
        /// Preview and baking color for <see cref="Slot"/>s with full entropy.
        /// </summary>
        public static Color CAGE_EVERYTHING_COLOR = Color.FromArgb(128, 255, 255, 255);

        /// <summary>
        /// Preview and baking color for <see cref="Slot"/>s with entropy = 2.
        /// Used for arbitrary entropy gradient.
        /// </summary>
        public static Color CAGE_TWO_COLOR = Color.FromArgb(128, 0, 0, 0);

        /// <summary>
        /// Preview and baking color for deterministic <see cref="Slot"/>s with
        /// entropy = 1. 
        /// </summary>
        public static Color CAGE_ONE_COLOR = Color.FromArgb(128, 0, 255, 0);

        /// <summary>
        /// Preview and baking color for contradictory <see cref="Slot"/>s with
        /// entropy = 0. 
        /// </summary>
        public static Color CAGE_NONE_COLOR = Color.FromArgb(128, 255, 0, 0);

        /// <summary>
        /// Preview and baking color of a text dot marking an index and a
        /// position of a connector in X <see cref="Axis"/>.
        /// </summary>
        public static Color X_DOT_COLOR = Color.FromArgb(32, 255, 0, 0);

        /// <summary>
        /// Preview and baking color of a text dot marking an index and a
        /// position of a connector in Y <see cref="Axis"/>.
        /// </summary>
        public static Color Y_DOT_COLOR = Color.FromArgb(32, 0, 255, 0);

        /// <summary>
        /// Preview and baking color of a text dot marking an index and a
        /// position of a connector in Z <see cref="Axis"/>.
        /// </summary>
        public static Color Z_DOT_COLOR = Color.FromArgb(32, 0, 0, 255);


        /// <summary>
        /// Preview and baking color of geometry marking a connector in X
        /// <see cref="Axis"/>.
        /// </summary>
        public static Color X_COLOR = Color.FromArgb(128, 255, 0, 0);

        /// <summary>
        /// Preview and baking color of geometry marking a connector in Y
        /// <see cref="Axis"/>.
        /// </summary>
        public static Color Y_COLOR = Color.FromArgb(128, 0, 255, 0);

        /// <summary>
        /// Preview and baking color of geometry marking a connector in Z
        /// <see cref="Axis"/>.
        /// </summary>
        public static Color Z_COLOR = Color.FromArgb(128, 0, 0, 255);

        /// <summary>
        /// Preview color of a dot marking a connector in positive
        /// <see cref="Orientation"/>.
        /// </summary>
        public static Color POSITIVE_COLOR = Color.FromArgb(128, 255, 255, 255);

        /// <summary>
        /// Preview color of a dot marking a connector in negative
        /// <see cref="Orientation"/>.
        /// </summary>
        public static Color NEGATIVE_COLOR = Color.FromArgb(128, 0, 0, 0);


        /// <summary>
        /// Converts <see cref="Direction"/> to a dot background color.
        /// </summary>
        /// <param name="dir">The <see cref="Direction"/> to convert.</param>
        /// <returns>Dot background <see cref="Color"/>.
        ///     </returns>
        public static Color ColorFromAxis(Axis axis) {
            if (axis == Axis.X) {
                return X_DOT_COLOR;
            }
            if (axis == Axis.Y) {
                return Y_DOT_COLOR;
            }
            if (axis == Axis.Z) {
                return Z_DOT_COLOR;
            }
            return Color.Red;
        }

        /// <summary>
        /// Converts <see cref="Direction"/> to a dot foreground color.
        /// </summary>
        /// <param name="dir">The <see cref="Direction"/> to convert.</param>
        /// <returns>Dot foreground <see cref="Color"/>.
        ///     </returns>
        public static Color ColorFromOrientation(Orientation orientation) {
            if (orientation == Orientation.Positive) {
                return POSITIVE_COLOR;
            }
            if (orientation == Orientation.Negative) {
                return NEGATIVE_COLOR;
            }
            return Color.Blue;
        }

    }
}
