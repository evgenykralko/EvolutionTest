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

namespace EvolutionTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public const int elementWidth = 8;
		public const int elementHeight = 8;

		public const int worldWidth = 225;
		public const int worldHeight = 125;

		public World MyWorld;
		public ConcurrentQueue<Action> ActionsPerTick = new ConcurrentQueue<Action>();
		Dictionary<Entity, UIElement> UIElementsDict = new Dictionary<Entity, UIElement>();

		private int yearsCount = 0;
		public int YearsCount 
		{
			get 
			{ 
				return yearsCount; 
			}
			set 
			{
				yearsCount = value;
				OnPropertyChanged(nameof(YearsCount)); 
			}
		}

		private int entitiesCount = 0;
		public int EntitiesCount
		{
			get
			{
				return entitiesCount;
			}
			set
			{
				entitiesCount = value;
				OnPropertyChanged(nameof(EntitiesCount));
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			MainForm.DataContext = this;
			canvas.Background = Brushes.AliceBlue;

			MyWorld = new World(worldWidth, worldHeight);

			MyWorld.EntityAdded += MyWorld_EntityAdded;
			MyWorld.EntityMoved += MyWorld_EntityMoved;
			MyWorld.EntityRemoved += MyWorld_EntityRemoved;

			HashSet<Cell> bots = new HashSet<Cell>();

			for (int i = 0; i < 10000; i++)
			{
				Cell position = new Cell(
					MyWorld.RndGenerator.Next(0, MyWorld.Width),
					MyWorld.RndGenerator.Next(0, MyWorld.Height));

				if (bots.Add(position))
				{
					Color color = Color.FromRgb((byte)MyWorld.RndGenerator.Next(256), (byte)MyWorld.RndGenerator.Next(256), (byte)MyWorld.RndGenerator.Next(256));
					Bot bot = new Bot(MyWorld, position, color);
					MyWorld.AddEntity(bot, position);
				}
			}

			DrawWorld();

			Task.Run(() =>
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

		private byte Darker(byte part)
		{
			int color = part;
			color += 50;
			if (color > 255)
			{
				color = 255;
			}

			return (byte)color;
		}

		private void MyWorld_EntityAdded(object sender, EntityAddedEventArgs e)
		{
			Action action = new Action(() =>
			{
				Color border = Color.FromArgb(e.Obj.Background.A, Darker(e.Obj.Background.R), Darker(e.Obj.Background.G), Darker(e.Obj.Background.B));

				Ellipse ellipse = new Ellipse();
				ellipse.Fill = new SolidColorBrush(e.Obj.Background);
				ellipse.Stroke = new SolidColorBrush(border);
				ellipse.Width = elementWidth;
				ellipse.Height = elementHeight;

				Canvas.SetLeft(ellipse, e.Position.X * elementWidth);
				Canvas.SetTop(ellipse, e.Position.Y * elementHeight);
				
				canvas.Children.Add(ellipse);

				lock (UIElementsDict)
				{
					UIElementsDict.Add(e.Obj, ellipse);
				}
			});

			ActionsPerTick.Enqueue(action);
		}

		private void MyWorld_EntityMoved(object sender, EntityMovedEventArgs e)
		{
			Action action = new Action(() =>
			{
				lock (UIElementsDict)
				{
					UIElement element;
					if (UIElementsDict.TryGetValue(e.Obj, out element))
					{
						Canvas.SetLeft(element, Canvas.GetLeft(element) + (e.To.X - e.From.X) * elementWidth);
						Canvas.SetTop(element, Canvas.GetTop(element) + (e.To.Y - e.From.Y) * elementHeight);

						if (element is Rectangle rect)
						{
							rect.Fill = new SolidColorBrush(e.Obj.Background);
						}
					}
				}
			});

			ActionsPerTick.Enqueue(action);
		}

		private void MyWorld_EntityRemoved(object sender, EntityRemovedEventArgs e)
		{
			Action action = new Action(() =>
			{
				lock (UIElementsDict)
				{
					UIElement element;
					if (UIElementsDict.TryGetValue(e.Obj, out element))
					{
						canvas.Children.Remove(element);
						UIElementsDict.Remove(e.Obj);
					}
				}
			});

			ActionsPerTick.Enqueue(action);
		}

		public void DrawWorld()
		{
			EntitiesCount = MyWorld.Bots.Count;

			App.Current.Dispatcher.Invoke(() =>
			{
				Action action;
				while (ActionsPerTick.TryDequeue(out action))
				{
					action.Invoke();
				}

				ActionsPerTick.Clear();
			});
		}
	}
}
