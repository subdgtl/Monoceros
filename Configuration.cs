// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace WFCToolset
{
    public class Configuration
    {
        public const string EMPTY_TAG = "empty";
        public const string OUTER_TAG = "out";
        public const string INDIFFERENT_TAG = "indifferent";

        public static readonly string[] RESERVED_NAMES = { EMPTY_TAG, OUTER_TAG };

        public static System.Drawing.Color CAGE_COLOR = System.Drawing.Color.FromArgb(128, 255, 255, 255);

        public static System.Drawing.Color X_COLOR_BACKGROUND = System.Drawing.Color.FromArgb(32, 255, 0, 0);
        public static System.Drawing.Color Y_COLOR_BACKGROUND = System.Drawing.Color.FromArgb(32, 0, 255, 0);
        public static System.Drawing.Color Z_COLOR_BACKGROUND = System.Drawing.Color.FromArgb(32, 0, 0, 255);

        public static System.Drawing.Color POSITIVE_COLOR_FOREGROUND = System.Drawing.Color.White;
        public static System.Drawing.Color NEGATIVE_COLOR_FOREGROUND = System.Drawing.Color.Black;

        public static System.Drawing.Color ColorBackgroundFromDirection(Direction dir)
        {
            if (dir.Axis == Axis.X)
            {
                return X_COLOR_BACKGROUND;
            }
            if (dir.Axis == Axis.Y)
            {
                return Y_COLOR_BACKGROUND;
            }
            if (dir.Axis == Axis.Z)
            {
                return Z_COLOR_BACKGROUND;
            }
            return System.Drawing.Color.Red;
        }

        public static System.Drawing.Color ColorForegroundFromDirection(Direction dir)
        {
            if (dir.Orientation == Orientation.Positive)
            {
                return POSITIVE_COLOR_FOREGROUND;
            }
            if (dir.Orientation == Orientation.Negative)
            {
                return NEGATIVE_COLOR_FOREGROUND;
            }
            return System.Drawing.Color.Blue;
        }

    }
}