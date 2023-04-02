
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static EvolutionTest.MainWindow;

namespace EvolutionTest
{
	public class FastCanvas : Canvas
	{
		private bool refreshRequested = false;
		private BotInfo[] entities;

		ScaleTransform transform = new ScaleTransform();

		public FastCanvas() : base()
		{
			RenderTransform = transform;
			MouseWheel += PanAndZoomCanvas_MouseWheel;
		}

		public void Refresh(BotInfo[] entities)
		{
			this.entities = entities;
			refreshRequested = true;
			InvalidateVisual();
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			if (DesignerProperties.GetIsInDesignMode(this) || !refreshRequested) return;

			var main = App.Current.MainWindow as MainWindow;
			
			double penThickness = 1;
			double penAdj = penThickness / 2;

			/*foreach (BotInfo bot in entities)
			{
				double x1 = bot.X * ElementSize + penAdj;
				double y1 = bot.Y * ElementSize + penAdj;
				double x2 = x1 + ElementSize - penAdj;
				double y2 = y1 + ElementSize - penAdj;

				Color color = Utils.GetBotColorByMode(bot, main.ColorMode);
				double factor = (Utils.IsDarkColor(color) ? 1 : -1) * 0.2;
				Brush penBrush = new SolidColorBrush(Utils.ChangeColorBrightness(color, factor));

				dc.DrawRectangle(new SolidColorBrush(color), new Pen(penBrush, penThickness), new Rect(
					new Point(x1 * transform.ScaleX, y1 * transform.ScaleX), 
					new Point(x2 * transform.ScaleX, y2 * transform.ScaleX)));
			}*/

			refreshRequested = false;
		}

		private void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			double factor = 1.2;

			Point position = e.GetPosition(this);

			if (e.Delta > 0)
			{
				transform.ScaleX *= factor;
				transform.ScaleY *= factor;
			}
			else
			{
				transform.ScaleX /= factor;
				transform.ScaleY /= factor;
			}
		}
	}
}
