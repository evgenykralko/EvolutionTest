using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EvolutionTest
{
	public partial class MainWindow : Window
	{
		public const int ElementSize = 4;
		public const int InitElementsCount = 100000;

		public World MyWorld;
		public static Random RndGenerator = new Random();

		public enum ColorModes { Normal, Predators, Energy, Age, Mobility, Family }
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

			MyWorld = new World(RndGenerator, worldWidth, worldHeight, loopX: true, loopY: true);
			GenerateEntities();

			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => 
				edSize.Text = $"{MyWorld.Width}x{MyWorld.Height} ({MyWorld.Width * MyWorld.Height})"));

			_ = Task.Run(() =>
			{
				while (true)
				{
					Utils.LogDebugInfo(() => MyWorld.Tick(), $"Tick {MyWorld.TickCount} for {MyWorld.Population} objects");
					Utils.LogDebugInfo(() => DrawWorldTick(), $"Rendering for {MyWorld.Population} objects");

					// Regenerate World if all entities have died
					if (MyWorld.Population == 0)
					{
						GenerateEntities();
					}
				}
			});
		}

		public void DrawWorldTick()
		{
			App.Current?.Dispatcher.Invoke(() =>
			{
				WriteableBitmap writeableBmp = BitmapFactory.New((int)ActualWidth, (int)ActualHeight);
				Point position = Mouse.GetPosition(this);

				using (writeableBmp.GetBitmapContext())
				{
					int scale = _scale;
					ColorModes mode = ColorMode;

					foreach (Bot bot in MyWorld.Bots)
					{
						int size = ElementSize;

						int x1 = bot.Position.X * size - scale;
						int y1 = bot.Position.Y * size - scale ;
						int x2 = x1 + size - 1 + scale;
						int y2 =y1 + size - 1 + scale;

						Color color = GetBotColorByMode(bot, mode);
						double factor = (Utils.IsDarkColor(color) ? 1 : -1) * 0.2;
						writeableBmp.FillRectangle(x1, y1, x2, y2, color);
						writeableBmp.DrawRectangle(x1, y1, x2, y2, Utils.ChangeColorBrightness(color, factor));
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

				edSteps.Text = MyWorld.TickCount.ToString();
				edPopulation.Text = MyWorld.Population.ToString();
			});

			// Wait until rendering for canvas is done.
			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
		}

		private void GenerateEntities()
		{
			Utils.LogDebugInfo(() => 
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
						Bot bot = new Bot(MyWorld, position, Utils.GetRandomColor());
						MyWorld.AddEntity(bot, position);
						count++;
					}
				}
			}, $"Generate {InitElementsCount} entities");
		}

		private Dictionary<Guid, Color> familyColors = new Dictionary<Guid, Color>();

		private Color GetBotColorByMode(Bot bot, ColorModes mode)
		{
			Color color = bot.Background;

			switch (mode)
			{
				case ColorModes.Normal:
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

				case ColorModes.Family:
					if (!familyColors.TryGetValue(bot.FamilyID, out color))
					{
						color = Utils.GetRandomColor();
						familyColors.Add(bot.FamilyID, color);
					}
					break;
			}

			return color;
		}

		#region Event handlers

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ColorMode = (ColorModes)e.AddedItems[0];
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

		#endregion
	}
}
