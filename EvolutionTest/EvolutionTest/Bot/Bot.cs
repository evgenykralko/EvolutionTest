using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace EvolutionTest
{
	public enum BotAction { Tick, Attack, EatOrganics, Move, Rotate, Multiply };

	public class Bot : Entity
	{
		public const int InitialEnergy = 200;
		public const int MaxEnergy = 500;
		public const int MaxAge = 100;
		public const int MutationChance = 10;

		public Perceptron Brain;
		public Bot Parent;
		public Cell LookAt;

		public bool IsPredator { get; private set; }
		public bool IsMobile { get; private set; }

		public int Direction { get; private set; }

		public static readonly Cell[] Directions =
		{
			new Cell(0, -1),
			new Cell(1, -1),
			new Cell(1, 0),
			new Cell(1, 1),
			new Cell(0, 1),
			new Cell(-1, 1),
			new Cell(-1, 0),
			new Cell(-1, -1)
		};

		public Bot(World liveIn, Cell position, Color color, Bot parent = null, double energy = InitialEnergy)
			: base(liveIn, position)
		{
			Energy = energy;
			Background = color;

			if (parent != null)
			{
				Parent = parent;
				Brain = parent.Brain.Copy();
				Direction = parent.Direction;

				if (LiveIn.RollDice(MutationChance))
				{
					Mutate();
				}
			}
			else
			{
				Direction = LiveIn.RndGenerator.Next(Directions.Length);

				int[] definition = new int[] { 5, 6, 6, 6 };
				Brain = new Perceptron(definition);
			}
		}

		protected virtual void Mutate()
		{
			for (int i = 0; i < Brain.layers.Count; i++)
			{
				int randomNeuronIndex = LiveIn.RndGenerator.Next(0, Brain.layers[i].numberOfNeurons);

				Brain.layers[i].neurons[randomNeuronIndex].SetRandomBias();
				Brain.layers[i].neurons[randomNeuronIndex].SetRandomWeights();
			}

			double factor = LiveIn.RndGenerator.Next(-30, 30) / 100.0;
			Background = Utils.ChangeColorBrightness(Background, factor);
		}

		#region Actions

		public override bool Tick()
		{
			bool isAlive;

			do
			{
				if ((isAlive = base.Tick() && Age <= MaxAge && SpendEnergy(BotAction.Tick)) != true) 
					break;

				BrainOutput output = Think();
				LookAt = GetLookAt(Position, Direction);

				if (output.Multiply > 0 && 
					output.EnergyToGive > 0 && 
					(isAlive = Multiply(output.Multiply, output.EnergyToGive)) != true) break;

				IsPredator = false;
				IsMobile = false;

				if (output.Attack)
				{
					if ((isAlive = Attack()) != true)
						break;
				}
				else
				{
					if ((isAlive = Rotate(output.Direction)) != true)
						break;

					if (output.Move)
					{
						if ((isAlive = Move(spendEnergy: true)) != true)
						break;
					}
					else if (output.Photosynthesis)
					{
						if ((isAlive = Photosynthesis()) != true)
							break;
					}
				}
			}
			while (false);

			return isAlive;
		}

		public virtual BrainOutput Think()
		{
			BrainInput input = new BrainInput();

			input.Energy = (double)Energy / (double)MaxEnergy;
			input.Age = (double)Age / (double)MaxAge;
			input.Direction = Direction;
			input.Eye = GetCellNutritionValue(LookAt);
			input.IsRelative = IsRelative(LookAt);

			return new BrainOutput(Brain.Activate(input.ToArray()));
		}

		protected virtual bool Multiply(int childrenCount, double energyToGive)
		{
			bool isAlive;

			do
			{
				int multiplyCost = GetActionCost(BotAction.Multiply);
				double toGive = energyToGive / childrenCount;

				if ((isAlive = Energy - multiplyCost * childrenCount - energyToGive > 0 && toGive > 1) != true)
					break;

				for (int i = 0; i < childrenCount; i++)
				{
					Cell freeSpace = FindFreeCellAround(Position, LookAt);
					if (!freeSpace.IsEmpty())
					{
						Energy -= toGive;
						SpendEnergy(BotAction.Multiply);

						Bot child = new Bot(LiveIn, freeSpace, Background, this, toGive);
						LiveIn.AddEntity(child, freeSpace);
					}
					else break;
				}
			}
			while (false);

			return isAlive;
		}

		protected virtual bool Attack()
		{
			bool isAlive = true;
			IsPredator = true;

			if (LiveIn.IsInBounds(ref LookAt))
			{
				Entity entity = LiveIn.GetEntity(LookAt);
				if (entity is Bot bot)
				{
					if (isAlive = SpendEnergy(BotAction.Attack))
					{
						GetEnergy(bot.Energy);
						bot.Kill();
						Move(spendEnergy: false);
					}
				}
				else
				{
					isAlive = Move(spendEnergy: true);
				}
			}

			return isAlive;
		}

		protected virtual bool Rotate(int desiredDirection)
		{
			bool isAlive = true;

			if (desiredDirection != Direction && (isAlive = SpendEnergy(BotAction.Rotate)))
			{
				Direction = desiredDirection;
			}

			return isAlive;
		}

		protected virtual bool Move(bool spendEnergy)
		{
			bool isAlive = true;
			IsMobile = true;

			if (LiveIn.IsInBounds(ref LookAt))
			{
				Entity obj = LiveIn.GetEntity(LookAt);
				if (obj == null && (isAlive = spendEnergy ? SpendEnergy(BotAction.Move) : true))
				{
					Cell from = Position;
					Cell to = LookAt;

					Position = LookAt;
					LiveIn.MoveEntity(this, from, to);
				}
			}

			return isAlive;
		}

		protected virtual bool Photosynthesis()
		{
			GetEnergy(25.0f);
			return true;
		}

		#endregion

		#region Utils

		protected void GetEnergy(double energy)
		{
			Energy += energy;

			if (Energy > MaxEnergy)
			{
				Energy = MaxEnergy;
			}
		}

		protected bool SpendEnergy(BotAction action) => (Energy -= GetActionCost(action)) > 0;

		protected int GetActionCost(BotAction action)
		{
			int cost;

			switch (action)
			{
				case BotAction.Tick:
					cost = 0; break;
				case BotAction.Attack:
					cost = 10; break;
				case BotAction.EatOrganics:
					cost = 8; break;
				case BotAction.Move:
					cost = 1; break;
				case BotAction.Rotate:
					cost = 0; break;
				case BotAction.Multiply:
					cost = 10; break;
				default:
					cost = 0; break;
			}

			return cost;
		}

		protected Cell FindFreeCellAround(Cell point, Cell lookAt)
		{
			Cell freeCell = new Cell(-1, -1);
			List<Cell> freeCells = new List<Cell>();

			int radius = 1;
			for (int i = -radius; i <= radius; i++)
			{
				for (int j = -radius; j <= radius; j++)
				{
					Cell nextCell = new Cell(point.X + i, point.Y + j);
					if (LiveIn.IsInBounds(ref nextCell) && LiveIn.GetEntity(nextCell) == null)
					{
						freeCells.Add(nextCell);
					}
				}
			}

			if (freeCells.Count > 0)
			{
				freeCell = freeCells[LiveIn.RndGenerator.Next(freeCells.Count)];
			}

			return freeCell;
		}

		protected Cell GetLookAt(Cell cell, int direction)
		{
			Cell shift = Directions[direction];
			int x = cell.X + shift.X;
			int y = cell.Y + shift.Y;

			return new Cell(x, y);
		}

		protected double GetCellNutritionValue(Cell cell)
		{
			double energy = 0;
			
			if (LiveIn.IsInBounds(ref cell))
			{
				Entity obj = LiveIn.GetEntity(cell);
				energy = obj?.Energy ?? 0;
			}

			return energy;
		}

		protected double IsRelative(Cell cell)
		{
			double isRelative = 0;
			
			if (LiveIn.IsInBounds(ref cell) && 
				LiveIn.GetEntity(cell) is Bot bot && 
				bot.Background == Background)
			{
				isRelative = 1.0f;
			}

			return isRelative;
		}

		#endregion
	}
}
