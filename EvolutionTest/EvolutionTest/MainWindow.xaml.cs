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

namespace EvolutionTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public const int ElementWidth = 6;
		public const int ElementHeight = 6;
		public const int ElementCount = 18000;

		public const int WorldWidth = 180;
		public const int WorldHeight = 100;

		public World MyWorld;
		public Queue<Action> TickActions = new Queue<Action>();
		Dictionary<Entity, UIElement> UIElementsDict = new Dictionary<Entity, UIElement>();

		public int YearsCount { get; set; }
		public int EntitiesCount { get; set; }

		public enum ColorModes { Normal, Predators, Energy }
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
			canvas.Width = WorldWidth * ElementWidth;
			canvas.Height = WorldHeight * ElementHeight;

			MyWorld = new World(WorldWidth, WorldHeight, loopX: true, loopY: true);

			MyWorld.EntityAdded += MyWorld_EntityAdded;
			MyWorld.EntityMoved += MyWorld_EntityMoved;
			MyWorld.EntityRemoved += MyWorld_EntityRemoved;

			HashSet<Cell> botCells = new HashSet<Cell>();

			int count = 0;
			while (count < ElementCount)
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

			DrawWorld();

			_ = Task.Run(() =>
			{
				while (true)
				{
					DateTime startTime = DateTime.Now;

					MyWorld.Tick();
					YearsCount++;

					TimeSpan completedTime = DateTime.Now - startTime;
					Debug.WriteLine($"Tick {YearsCount} completed in {completedTime.TotalSeconds} sec.");

					startTime = DateTime.Now;
					DrawWorld();
					completedTime = DateTime.Now - startTime;

					Debug.WriteLine($"{nameof(DrawWorld)} completed in {completedTime.TotalSeconds} sec.");

					Thread.Sleep(100);
				}
			});
		}

		#region World Actions

		private void MyWorld_EntityAdded(object sender, EntityAddedEventArgs e)
		{
			Action action = new Action(() =>
			{
				BotControl control = new BotControl(e.Obj)
				{
					DataContext = this,
					Width = ElementWidth,
					Height = ElementHeight
				};

				Canvas.SetLeft(control, e.Position.X * ElementWidth);
				Canvas.SetTop(control, e.Position.Y * ElementHeight);

				canvas.Children.Add(control);
				UIElementsDict.Add(e.Obj, control);
			});

			TickActions.Enqueue(action);
		}

		private void MyWorld_EntityMoved(object sender, EntityMovedEventArgs e)
		{
			Action action = new Action(() =>
			{
				UIElement element;
				if (UIElementsDict.TryGetValue(e.Obj, out element))
				{
					Canvas.SetLeft(element, Canvas.GetLeft(element) + (e.To.X - e.From.X) * ElementWidth);
					Canvas.SetTop(element, Canvas.GetTop(element) + (e.To.Y - e.From.Y) * ElementHeight);
				}
			});

			TickActions.Enqueue(action);
		}

		private void MyWorld_EntityRemoved(object sender, EntityRemovedEventArgs e)
		{
			Action action = new Action(() =>
			{
				UIElement element;
				if (UIElementsDict.TryGetValue(e.Obj, out element))
				{
					canvas.Children.Remove(element);
					UIElementsDict.Remove(e.Obj);
				}
			});

			TickActions.Enqueue(action);
		}

		#endregion

		public void DrawWorld()
		{
			EntitiesCount = MyWorld.Bots.Count;

			App.Current.Dispatcher.Invoke(() =>
			{
				edSteps.Text = YearsCount.ToString();
				edPopulation.Text = EntitiesCount.ToString();

				Action action;
				while (TickActions.TryDequeue(out action))
				{
					action.Invoke();
				}

				TickActions.Clear();
			});

			List<Action> colorModeActions = new List<Action>();

			foreach (var pair in UIElementsDict)
			{
				Bot bot = (Bot)pair.Key;
				BotControl control = (BotControl)pair.Value;

				Color color;

				switch (ColorMode)
				{
					case ColorModes.Normal:
						color = bot.Background;
						break;

					case ColorModes.Predators:
						color = bot.IsPredator ? Colors.Red : Colors.Green;
						break;

					case ColorModes.Energy:
						Color start = Colors.Gold;
						Color end = Colors.Firebrick;
						double lvl = bot.Energy / Bot.MaxEnergy;

						double aStep = end.A - start.A;
						double rStep = end.R - start.R;
						double gStep = end.G - start.G;
						double bStep = end.B - start.B;

						int a = start.A + (int)(aStep * lvl);
						int r = start.R + (int)(rStep * lvl);
						int g = start.G + (int)(gStep * lvl);
						int b = start.B + (int)(bStep * lvl);

						color = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
						break;
				}

				if (control.PrevColor != color)
				{
					control.PrevColor = color;
					colorModeActions.Add(new Action(() => { SetControlColor(control, color); }));
				}
			}

			App.Current.Dispatcher.Invoke(() =>
			{
				foreach (Action colorModeAction in colorModeActions)
				{
					colorModeAction.Invoke();
				}
			});
		}

		private void SetControlColor(BotControl control, Color color)
		{
			control.ellipse.Fill = new SolidColorBrush(color);
			//control.eye.Fill = Brushes.Black;

			if (control.Obj is Bot bot)
			{
				//int angle = 360 / 8 * bot.Direction;
				//control.RenderTransformOrigin = new Point(0.5, 0.5);
				//control.RenderTransform = new RotateTransform() { Angle = angle };
			}
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ColorMode = (ColorModes)e.AddedItems[0];
		}
	}
}
