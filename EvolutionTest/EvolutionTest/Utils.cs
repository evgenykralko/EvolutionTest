using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using static EvolutionTest.MainWindow;

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

		public static Color GetBotColorByMode(BotInfo bot, ColorModes mode)
		{
			Color color = bot.Background;

			switch (mode)
			{
				case ColorModes.Normal:
					color = bot.Background;
					break;

				case ColorModes.Predators:
					color = bot.IsPredator ? Colors.Red : Colors.Green;
					break;

				case ColorModes.Energy:
					color = GetColorByLevel(Colors.Gold, Colors.Firebrick, bot.Energy / Bot.MaxEnergy);
					break;

				case ColorModes.Age:
					color = GetColorByLevel(Colors.LightGreen, Colors.SteelBlue, (double)bot.Age / (double)Bot.MaxAge);
					break;

				case ColorModes.Mobility:
					color = bot.IsMobile ? Colors.Coral : Colors.Gray;
					break;

				case ColorModes.Direction:
					Color[] colors = new[]
					{
						Colors.Aquamarine,
						Colors.Aqua,
						Colors.Cyan,
						Colors.Turquoise,
						Colors.SpringGreen,
						Colors.MediumTurquoise,
						Colors.Gray,
						Colors.MediumSpringGreen,
					};

					color = colors[RndGenerator.Next(bot.Direction)];
					break;
			}

			return color;
		}
	}
}
