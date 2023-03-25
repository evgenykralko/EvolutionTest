using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private double elementRadius;
		private Brush origBackground;

		private bool refreshRequested = false;

		public FastCanvas() : base() 
		{
		}

		public void Initialize(double elementRadius, out World myWorld, out int worldWidth, out int worldHeight)
		{
			origBackground = Background;
			this.elementRadius = elementRadius;

			worldWidth = (int)(ActualWidth / elementRadius / 2);
			worldHeight = (int)(ActualHeight / elementRadius / 2); ;
			myWorld = new World(worldWidth, worldHeight, loopX: true, loopY: true);

			this.entities = myWorld.Bots;
		}

		public void Refresh()
		{
			refreshRequested = true;
			InvalidateVisual();
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			if (DesignerProperties.GetIsInDesignMode(this) || !refreshRequested) return;

			foreach (Bot bot in entities)
			{
				Point point = new Point(bot.Position.X * elementRadius * 2 + elementRadius, bot.Position.Y * elementRadius * 2 + elementRadius);
				dc.DrawEllipse(new SolidColorBrush(GetBotColorByMode(bot)), null, point, elementRadius, elementRadius);
			}

			refreshRequested = false;
		}

		private Color GetBotColorByMode(Bot bot)
		{
			Color color;
			Brush background = origBackground;
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

					color = colors[bot.LiveIn.RndGenerator.Next(bot.Direction)];
					background = Brushes.DodgerBlue;
					break;
			}

			Background = background;
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
