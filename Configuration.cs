﻿namespace WFCToolset
{
    public class Configuration
    {
        public const string EMPTY_MODULE_NAME = "empty";
        public const string OUTER_MODULE_NAME = "out";
        public const string INDIFFERENT_CONNECTOR_TYPE = "indifferent";

        public static readonly string[] RESERVED_NAMES = { EMPTY_MODULE_NAME, OUTER_MODULE_NAME };

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