using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace EvolutionTest
{
	public partial class MainWindow : Window
	{
		public const int ElementSize = 4;
		public const int InitElementsCount = 10000;

		public World MyWorld;

		public int YearsCount { get; set; }
		public int EntitiesCount { get; set; }

		public enum ColorModes { Normal, Predators, Energy, Age, Mobility, Direction }
		public ColorModes ColorMode { get; set; } = ColorModes.Normal;

		private List<ColorModes> colorModesList;
		public List<ColorModes> ColorModesList
		{
			get
			{
				if (colorModesList == null)
				{
					colorModesList = new List<ColorModes>();
					foreach (ColorModes mode in Enum.GetValues(typeof(ColorModes)))
					{
						colorModesList.Add(mode);
					}
				}

				return colorModesList;
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			MainForm.DataContext = this;
		}

		private void MainForm_Loaded(object sender, RoutedEventArgs e)
		{
			int worldWidth = (int)(canvas.ActualWidth / ElementSize);
			int worldHeight = (int)(canvas.ActualHeight / ElementSize);

			MyWorld = new World(worldWidth, worldHeight, loopX: true, loopY: true);

			HashSet<Cell> botCells = new HashSet<Cell>();
			int count = 0;

			while (count < InitElementsCount)
			{
				Cell position = new Cell(
					MyWorld.RndGenerator.Next(MyWorld.Width),
					MyWorld.RndGenerator.Next(MyWorld.Height));

				if (botCells.Add(position))
				{
					Color color = Color.FromRgb(
						(byte)MyWorld.RndGenerator.Next(256),
						(byte)MyWorld.RndGenerator.Next(256),
						(byte)MyWorld.RndGenerator.Next(256));
					Bot bot = new Bot(MyWorld, position, color);
					MyWorld.AddEntity(bot, position);
					count++;
				}
			}

			botCells.Clear();

			_ = Task.Run(() =>
			{
				while (true)
				{
					LogDebugInfo(() => MyWorld.Tick(), "Tick");
					LogDebugInfo(() =>
					{
						App.Current.Dispatcher.Invoke(() =>
						{
							WriteableBitmap writeableBmp = BitmapFactory.New((int)Width, (int)Height);

							using (writeableBmp.GetBitmapContext())
							{
								foreach (Bot bot in MyWorld.Bots)
								{
									int x1 = bot.Position.X * ElementSize;
									int y1 = bot.Position.Y * ElementSize;
									int x2 = x1 + ElementSize - 1;
									int y2 = y1 + ElementSize - 1;

									Color color = GetBotColorByMode(bot, ColorMode);
									double factor = (Utils.IsDarkColor(color) ? 1 : -1) * 0.2;
									writeableBmp.FillRectangle(x1, y1, x2, y2, color);
									writeableBmp.DrawRectangle(x1, y1, x2, y2, Utils.ChangeColorBrightness(color, factor));
								}

								Image image = new Image() { Source = writeableBmp };
								canvas.Children.Clear();
								canvas.Children.Add(image);
							}

							edSize.Text = $"{worldWidth}x{worldHeight} ({worldWidth * worldHeight})";
							edSteps.Text = (++YearsCount).ToString();
							edPopulation.Text = MyWorld.Bots.Count.ToString();
						});

						// Wait until rendering for canvas is done.
						App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
					}, "Rendering");
				}
			});
		}

		private Color GetBotColorByMode(Bot bot, ColorModes mode)
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
					color = Utils.GetColorByLevel(Colors.Gold, Colors.Firebrick, bot.Energy / Bot.MaxEnergy);
					break;

				case ColorModes.Age:
					color = Utils.GetColorByLevel(Colors.LightGreen, Colors.SteelBlue, (double)bot.Age / (double)Bot.MaxAge);
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
					break;
			}

			return color;
		}

		public static void LogDebugInfo(Action action, string message)
		{
			DateTime startTime = DateTime.Now;
			action.Invoke();
			TimeSpan completedTime = DateTime.Now - startTime;
			Debug.WriteLine($"{message} action is completed in {completedTime.TotalSeconds} sec.");
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ColorMode = (ColorModes)e.AddedItems[0];
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			string showText = "▲";
			string hideText = "▼";
			
			Button button = sender as Button;
			bool hide = panel.Visibility == Visibility.Visible;

			button.Content = hide ? showText : hideText;
			panel.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
		}
	}
}
