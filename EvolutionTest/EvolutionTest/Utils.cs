using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace EvolutionTest
{
	public static class Utils
	{
		public static Color ChangeColorBrightness(Color color, double correctionFactor)
		{
			double red = color.R;
			double green = color.G;
			double blue = color.B;

			if (correctionFactor < 0)
			{
				correctionFactor = 1 + correctionFactor;
				red *= correctionFactor;
				green *= correctionFactor;
				blue *= correctionFactor;
			}
			else
			{
				red = (255 - red) * correctionFactor + red;
				green = (255 - green) * correctionFactor + green;
				blue = (255 - blue) * correctionFactor + blue;
			}

			return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
		}

		public static bool IsDarkColor(Color color) => color.R * 0.2126 + color.G * 0.7152 + color.B * 0.0722 < 255 / 2;

		public static Color GetColorByLevel(Color start, Color end, double lvl)
		{
			double rStep = end.R - start.R;
			double gStep = end.G - start.G;
			double bStep = end.B - start.B;

			int r = start.R + (int)(rStep * lvl);
			int g = start.G + (int)(gStep * lvl);
			int b = start.B + (int)(bStep * lvl);

			return Color.FromRgb((byte)r, (byte)g, (byte)b);
		}
	}
}
