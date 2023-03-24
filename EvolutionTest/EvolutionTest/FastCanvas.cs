using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static EvolutionTest.MainWindow;

namespace EvolutionTest
{
	public class FastCanvas : Canvas
	{
		private HashSet<Entity> entities;
		private int elementRadius;

		public void Initialize(HashSet<Entity> entities, int worldWidth, int worldHeight, int elementRadius)
		{
			this.entities = entities;
			this.elementRadius = elementRadius;

			Width = worldWidth * elementRadius * 2;
			Height = worldHeight * elementRadius * 2;
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			foreach (Bot bot in entities)
			{
				Point point = new Point(bot.Position.X * elementRadius * 2 + elementRadius, bot.Position.Y * elementRadius * 2 + elementRadius);
				dc.DrawEllipse(new SolidColorBrush(GetBotColorByMode(bot)), null, point, elementRadius, elementRadius);
			}
		}

		private Color GetBotColorByMode(Bot bot)
		{
			Color color;
			MainWindow window = (App.Current.MainWindow as MainWindow);

			switch (window.ColorMode)
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
			}

			return color;
		}

		private Color GetColorByLevel(Color start, Color end, double lvl)
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
