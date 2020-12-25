// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace WFCToolset
{
    public class Configuration
    {
        public const string EMPTY_TAG = "empty";
        public const string OUTER_TAG = "out";
        public const string INDIFFERENT_TAG = "indifferent";

        public static readonly string[] RESERVED_NAMES = { EMPTY_TAG, OUTER_TAG };
        public static string RESERVED_TO_STRING => RESERVED_NAMES.Aggregate("", (accum, name) => accum + ", " + name);

        public static System.Drawing.Color CAGE_COLOR = System.Drawing.Color.FromArgb(128, 255, 255, 255);

        public static System.Drawing.Color X_DOT_COLOR = System.Drawing.Color.FromArgb(32, 255, 0, 0);
        public static System.Drawing.Color Y_DOT_COLOR = System.Drawing.Color.FromArgb(32, 0, 255, 0);
        public static System.Drawing.Color Z_DOT_COLOR = System.Drawing.Color.FromArgb(32, 0, 0, 255);

        public static System.Drawing.Color X_COLOR = System.Drawing.Color.FromArgb(128, 255, 0, 0);
        public static System.Drawing.Color Y_COLOR = System.Drawing.Color.FromArgb(128, 0, 255, 0);
        public static System.Drawing.Color Z_COLOR = System.Drawing.Color.FromArgb(128, 0, 0, 255);

        public static System.Drawing.Color POSITIVE_COLOR = System.Drawing.Color.FromArgb(128, 255, 255, 255);
        public static System.Drawing.Color NEGATIVE_COLOR = System.Drawing.Color.FromArgb(128, 0, 0, 0);


        public static System.Drawing.Color ColorBackgroundFromDirection(Direction dir)
        {
            if (dir._axis == Axis.X)
            {
                return X_DOT_COLOR;
            }
            if (dir._axis == Axis.Y)
            {
                return Y_DOT_COLOR;
            }
            if (dir._axis == Axis.Z)
            {
                return Z_DOT_COLOR;
            }
            return System.Drawing.Color.Red;
        }

        public static System.Drawing.Color ColorForegroundFromDirection(Direction dir)
        {
            if (dir._orientation == Orientation.Positive)
            {
                return POSITIVE_COLOR;
            }
            if (dir._orientation == Orientation.Negative)
            {
                return NEGATIVE_COLOR;
            }
            return System.Drawing.Color.Blue;
        }

    }
}
