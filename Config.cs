using System.Drawing;
using System.Linq;

namespace Monoceros {
    /// <summary>
    /// The Monoceros configuration and constants.
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
        /// Maximum number of parts supported by the current solver.
        /// </summary>
        public static readonly int MAX_PARTS = 248;

        public static readonly string FONT_FACE = "Mark Pro";
        public static readonly int MODULE_NAME_FONT_HEIGHT = 25;

        /// <summary>
        /// Preview and baking color a <see cref="Module"/> cage and
        /// <see cref="ModuleConnector.Face"/>.
        /// </summary>
        public static Color CAGE_COLOR = Color.FromArgb(192, 255, 255, 255);

        /// <summary>
        /// Preview and baking color an erroneous <see cref="Module"/> cage and
        /// <see cref="ModuleConnector.Face"/>.
        /// </summary>
        public static Color CAGE_ERROR_COLOR = Color.FromArgb(192, 238, 33, 67);

        /// <summary>
        /// Preview and baking color for <see cref="Slot"/>s with unknown
        /// entropy.
        /// </summary>
        public static Color CAGE_UNKNOWN_COLOR = Color.FromArgb(192, 28, 141, 157);

        /// <summary>
        /// Preview and baking color for <see cref="Slot"/>s with full entropy.
        /// </summary>
        public static Color CAGE_EVERYTHING_COLOR = Color.FromArgb(192, 255, 255, 255);

        /// <summary>
        /// Preview and baking color for <see cref="Slot"/>s with entropy = 2.
        /// Used for arbitrary entropy gradient.
        /// </summary>
        public static Color CAGE_TWO_COLOR = Color.FromArgb(192, 0, 0, 0);

        /// <summary>
        /// Preview and baking color for deterministic <see cref="Slot"/>s with
        /// entropy = 1. 
        /// </summary>
        public static Color CAGE_ONE_COLOR = Color.FromArgb(192, 28, 157, 104);

        /// <summary>
        /// Preview and baking color for contradictory <see cref="Slot"/>s with
        /// entropy = 0. 
        /// </summary>
        public static Color CAGE_NONE_COLOR = Color.FromArgb(192, 238, 33, 67);

        /// <summary>
        /// Preview and baking color of a text dot marking an index and a
        /// position of a connector in X <see cref="Axis"/>.
        /// </summary>
        public static Color X_DOT_COLOR = Color.FromArgb(128, 238, 33, 67);

        /// <summary>
        /// Preview and baking color of a text dot marking an index and a
        /// position of a connector in Y <see cref="Axis"/>.
        /// </summary>
        public static Color Y_DOT_COLOR = Color.FromArgb(128, 28, 157, 104);

        /// <summary>
        /// Preview and baking color of a text dot marking an index and a
        /// position of a connector in Z <see cref="Axis"/>.
        /// </summary>
        public static Color Z_DOT_COLOR = Color.FromArgb(128, 28, 101, 157);


        /// <summary>
        /// Preview and baking color of geometry marking a connector in X
        /// <see cref="Axis"/>.
        /// </summary>
        public static Color X_COLOR = Color.FromArgb(192, 238, 33, 67);

        /// <summary>
        /// Preview and baking color of geometry marking a connector in Y
        /// <see cref="Axis"/>.
        /// </summary>
        public static Color Y_COLOR = Color.FromArgb(192, 28, 157, 104);

        /// <summary>
        /// Preview and baking color of geometry marking a connector in Z
        /// <see cref="Axis"/>.
        /// </summary>
        public static Color Z_COLOR = Color.FromArgb(192, 28, 101, 157);

        /// <summary>
        /// Preview color of a dot marking a connector in positive
        /// <see cref="Orientation"/>.
        /// </summary>
        public static Color POSITIVE_COLOR = Color.FromArgb(192, 255, 255, 255);

        /// <summary>
        /// Preview color of a dot marking a connector in negative
        /// <see cref="Orientation"/>.
        /// </summary>
        public static Color NEGATIVE_COLOR = Color.FromArgb(192, 0, 0, 0);

        /// <summary>
        /// <para>
        /// <see cref="Slot"/> preview cage shrink factor.
        /// </para>
        /// <para>
        /// A <see cref="Slot"/> is displayed as a wire frame cage. To make
        /// individual cages visually distinct, they are slightly shrunk, so
        /// that each cage's edges are visible. <see cref="SLOT_SHRINK_FACTOR"/>
        /// is the amount of uniform size reduction to make the visual
        /// distinction possible.
        /// </para>
        /// </summary>
        public static readonly double SLOT_SHRINK_FACTOR = 0.025;
        internal static readonly int RULE_PREVIEW_THICKNESS = 3;

        /// <summary>
        /// Converts <see cref="Direction"/> to a dot background color.
        /// </summary>
        /// <param name="dir">The <see cref="Direction"/> to convert.</param>
        /// <returns>Dot background <see cref="Color"/>.</returns>
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
        /// <returns>Dot foreground <see cref="Color"/>.</returns>
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
