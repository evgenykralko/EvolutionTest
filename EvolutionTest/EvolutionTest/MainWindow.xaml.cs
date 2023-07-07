using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EvolutionTest
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		public bool SaveScreenshot = false;
		public const string SaveScreenshotFolder = "E:/EvolutionTest";
		public const int SaveScreenshotStep = 100;

		public const int ElementSize = 4;
		public const int InitElementsPercent = 50;

		public World MyWorld;
		public static Random RndGenerator = new Random();

		public enum ColorModes { Normal, Predators, Energy, Age, Mobility, Family }
		private ColorModes colorMode = ColorModes.Normal;
		public ColorModes ColorMode
		{
			get => colorMode;
			set
			{
				colorMode = value;
				OnPropertyChanged(nameof(ColorMode));
			}
		}

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

			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() =>
			{
				edSize.Text = $"{MyWorld.Width}x{MyWorld.Height} ({MyWorld.Width * MyWorld.Height} cells)";
				edWorld.Text = MyWorld.Id.ToString();
				edSteps.Text = MyWorld.TickCount.ToString();
				edPopulation.Text = MyWorld.Population.ToString();
			}));

			_ = Task.Run(() =>
			{
				while (true)
				{
					if (MyWorld.Population == 0 || resetRequested)
					{
						MyWorld.Clear();
						DrawWorldTick();
						GenerateEntities();
						resetRequested = false;
					}

					Utils.LogDebugInfo(() => MyWorld.Tick(), $"Tick {MyWorld.TickCount} for {MyWorld.Population} objects");
					Utils.LogDebugInfo(() => DrawWorldTick(), $"Rendering for {MyWorld.Population} objects");

					if (SaveScreenshot && MyWorld.TickCount > 0 && MyWorld.TickCount % SaveScreenshotStep == 0)
					{
						CaptureScreenshoots();
					}
				}
			});
		}

		public void CaptureScreenshoots()
		{
			string folder = Path.Combine(SaveScreenshotFolder, $"World{MyWorld.Id}");
			Directory.CreateDirectory(folder);

			ColorModes origMode = ColorMode;
			foreach (ColorModes mode in Enum.GetValues(typeof(ColorModes)))
			{
				if (mode == ColorModes.Family || mode == ColorModes.Mobility) continue;

				string fileName = $"{mode}{MyWorld.TickCount}";
				string filePath = Path.Combine(folder, fileName);

				if (mode != origMode)
				{
					ColorMode = mode;
					DrawWorldTick();
				}

				App.Current?.Dispatcher.Invoke(() => Utils.SaveScreenshoot(filePath));
			}

			ColorMode = origMode;
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
						//Draw Bot figure
						int size = ElementSize;

						int x1 = bot.Position.X * size - scale;
						int y1 = bot.Position.Y * size - scale ;
						int x2 = x1 + size - 1 + scale;
						int y2 =y1 + size - 1 + scale;

						double factor = -0.2; // (Utils.IsDarkColor(color) ? 1 : -1) * 0.2;
						Color color = GetBotColorByMode(bot, mode);
						Color borderColor = Utils.ChangeColorBrightness(color, factor);

						writeableBmp.FillRectangle(x1, y1, x2, y2, color);
						writeableBmp.DrawRectangle(x1, y1, x2, y2, borderColor);

						//Draw Bot Direction
						int width = x2 - x1;
						int height = y2 - y1;

						if (width >= 10 && height >= 10)
						{
							int xr = width / 2;
							int yr = height / 2;
							int xc = x1 + xr;
							int yc = y1 + yr;

							Cell direction = Bot.Directions[bot.Direction];
							writeableBmp.DrawLine(xc, yc, xc + direction.X * xr, yc + direction.Y * yr, borderColor);
						}
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

				edWorld.Text = MyWorld.Id.ToString();
				edSteps.Text = MyWorld.TickCount.ToString();
				edPopulation.Text = MyWorld.Population.ToString();
			});

			// Wait until rendering for canvas is done.
			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));
		}

		private void GenerateEntities()
		{
			int elementsCount = MyWorld.Height * MyWorld.Width / 100 * InitElementsPercent;

			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() =>
			{
				edLoading.Visibility = Visibility.Visible;
			}));

			Utils.LogDebugInfo(() => 
			{
				HashSet<Cell> botCells = new HashSet<Cell>();
				int count = 0;

				while (count < elementsCount)
				{
					Cell position = new Cell(
						RndGenerator.Next(MyWorld.Width),
						RndGenerator.Next(MyWorld.Height));

					if (botCells.Add(position))
					{
						Bot bot = new Bot(MyWorld, position);
						MyWorld.AddEntity(bot, position);
						count++;
					}
				}
			}, $"Generate {elementsCount} entities");

			App.Current.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() =>
			{
				edLoading.Visibility = Visibility.Hidden;
			}));
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
					color = bot.IsPredator ? Color.FromRgb(251, 67, 67) : Color.FromRgb(38, 164, 38);
					break;

				case ColorModes.Energy:
					color = Utils.GetColorByLevel(Colors.Gold, Colors.Firebrick, bot.Energy / Bot.MaxEnergy);
					break;

				case ColorModes.Age:
					color = Utils.GetColorByLevel(Colors.MediumSpringGreen, Colors.DarkCyan, (double)bot.Age / (double)Bot.MaxAge);
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

		private int _scale = 2;
		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			_scale += e.Delta > 0 ? 1 : -1;

			if (_scale < 0)
			{
				_scale = 0;
			}
		}

		#endregion

		private bool resetRequested = false;

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			resetRequested = true;
		}
	}
}
