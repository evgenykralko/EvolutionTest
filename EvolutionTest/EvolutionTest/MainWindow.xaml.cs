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
		public const int InitElementsCount = 100000;

		public World MyWorld;
		public static Random RndGenerator = new Random();

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

		public EvolutionTestDB DB { get; private set; }

		public MainWindow()
		{
			InitializeComponent();
			MainForm.DataContext = this;
			DB = new EvolutionTestDB("WorldTestDB");
			canvas.MouseWheel += Canvas_MouseWheel;
		}

		private int _scale = 0;

		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			_scale += e.Delta > 0 ? 1 : -1;

			if (_scale < 0)
			{
				_scale = 0;
			}
		}

		private void MainForm_Loaded(object sender, RoutedEventArgs e)
		{
			int worldWidth = (int)(canvas.ActualWidth / ElementSize);
			int worldHeight = (int)(canvas.ActualHeight / ElementSize);

			MyWorld = new World(RndGenerator, worldWidth, worldHeight, loopX: true, loopY: true);
			GenerateEntities();

			_ = Task.Run(() =>
			{
				const int tickCount = 1000;
				int count = -1;

				// Calculate World
				while (++count < tickCount)
				{
					LogDebugInfo(() => MyWorld.Tick(), $"Tick {count} (from {tickCount}) for {MyWorld.Bots.Count} objects");
					LogDebugInfo(() => DrawWorldTick(MyWorld.Bots), "Rendering");

					// Regenerate World if all entities have died
					if (MyWorld.Bots.Count == 0)
					{
						GenerateEntities();
					}
				}
			});
		}

		public void DrawWorldTick(IEnumerable<Entity> bots)
		{
			App.Current?.Dispatcher.Invoke(() =>
			{
				WriteableBitmap writeableBmp = BitmapFactory.New((int)Width, (int)Height);
				Point position = Mouse.GetPosition(this);

				int count = 0;

				using (writeableBmp.GetBitmapContext())
				{
					int scale = _scale;

					foreach (Bot bot in bots)
					{
						int size = ElementSize;

						int x1 = bot.Position.X * size - scale;
						int y1 = bot.Position.Y * size - scale ;
						int x2 = x1 + size - 1 + scale;
						int y2 =y1 + size - 1 + scale;

						Color color = Utils.GetBotColorByMode(bot, ColorMode);
						double factor = (Utils.IsDarkColor(color) ? 1 : -1) * 0.2;
						writeableBmp.FillRectangle(x1, y1, x2, y2, color);
						writeableBmp.DrawRectangle(x1, y1, x2, y2, Utils.ChangeColorBrightness(color, factor));

						count++;
					}
				}

				if (canvas.Children.Count > 0)
				{
					Image image = canvas.Children[0] as Image;
					image.Source = null;
					image.Source = writeableBmp;
				}
				else
				{
					Image image = new Image() { Source = writeableBmp };
					canvas.Children.Add(image);
				}

				edSize.Text = $"{MyWorld.Width}x{MyWorld.Height} ({MyWorld.Width * MyWorld.Height})";
				edSteps.Text = (++YearsCount).ToString();
				edPopulation.Text = count.ToString();
			});

			// Wait until rendering for canvas is done.
			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
		}

		private void GenerateEntities()
		{
			HashSet<Cell> botCells = new HashSet<Cell>();
			int count = 0;

			while (count < InitElementsCount)
			{
				Cell position = new Cell(
					RndGenerator.Next(MyWorld.Width),
					RndGenerator.Next(MyWorld.Height));

				if (botCells.Add(position))
				{
					Color color = Color.FromRgb(
						(byte)RndGenerator.Next(256),
						(byte)RndGenerator.Next(256),
						(byte)RndGenerator.Next(256));
					Bot bot = new Bot(MyWorld, position, color);
					MyWorld.AddEntity(bot, position);
					count++;
				}
			}

			botCells.Clear();
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
