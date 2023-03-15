using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace EvolutionTest
{
	public enum BotDirection { Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft };
	public enum BotAction { Tick, Attack, EatOrganics, Move, Rotate, Multiply };

	public class Bot : Entity
	{
		public const int InitialEnergy = 200;
		public const int MaxEnergy = 500;
		public const int MaxAge = 30;
		public const int MutationChance = 20;

		public Perceptron Brain;
		public Bot Parent;
		public Cell LookAt;

		public BotDirection Direction { get; private set; }

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
				Direction = (BotDirection)LiveIn.RndGenerator.Next(0, Enum.GetNames(typeof(BotDirection)).Length - 1);

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

			//Background = Color.FromRgb((byte)LiveIn.RndGenerator.Next(256), (byte)LiveIn.RndGenerator.Next(256), (byte)LiveIn.RndGenerator.Next(256));
		}

		#region Actions

		public override bool Tick()
		{
			bool isAlive = 
				base.Tick() && 
				Age <= MaxAge && 
				SpendEnergy(BotAction.Tick);
			if (!isAlive) return false;

			BrainOutput output = Think();
			LookAt = GetLookAt(Position, Direction);

			if (output.Multiply > 0)
			{
				isAlive = Multiply(output.Multiply, output.EnergyToGive);
				if (!isAlive) return false;
			}

			if (output.Attack)
			{
				isAlive = Attack();
				if (!isAlive) return false;
			}
			else
			{
				isAlive = Rotate(output.Direction);
				if (!isAlive) return false;

				if (output.Move)
				{
					isAlive = Move(true);
					if (!isAlive) return false;
				}
				else if (output.Photosynthesis)
				{
					isAlive = Photosynthesis();
					if (!isAlive) return false;
				}
			}

			return isAlive;
		}

		public virtual BrainOutput Think()
		{
			BrainInput input = new BrainInput();

			input.Energy = (double)Energy / (double)MaxEnergy;
			input.Age = (double)Age / (double)MaxAge;
			input.Direction = (double)Direction / (Enum.GetNames(typeof(BotDirection)).Length - 1);
			input.Eye = GetCellNutritionValue(LookAt);
			input.IsRelative = IsRelative(LookAt);

			return new BrainOutput(Brain.Activate(input.ToArray()));
		}

		protected virtual bool Multiply(int children, double energyToGive)
		{
			int multiplyCost = GetActionCost(BotAction.Multiply);

			double toGiveTotal = (Energy - multiplyCost * children) * energyToGive;

			if (toGiveTotal < 0)
			{
				Energy = 0;
				return false;
			}

			if (toGiveTotal < multiplyCost) return false;

			double toGive = toGiveTotal / children;

			for (int i = 0; i < children; i++)
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

			return true;
		}

		protected virtual bool Attack()
		{
			bool isAlive = true;

			if (LiveIn.IsInBounds(LookAt))
			{
				Entity entity = LiveIn.GetEntity(LookAt);
				if (entity is Bot bot)
				{
					isAlive = SpendEnergy(BotAction.Attack);
					if (!isAlive) return false;

					GetEnergy(bot.Energy);
					bot.Kill();

					Move(false);
				}
			}

			return isAlive;
		}

		protected virtual bool Rotate(BotDirection desiredDirection)
		{
			bool isAlive = true;

			if (desiredDirection != Direction)
			{
				isAlive = SpendEnergy(BotAction.Rotate);
				if (!isAlive) return false;

				Direction = desiredDirection;
			}

			return isAlive;
		}

		protected virtual bool Move(bool spendEnergy)
		{
			bool isAlive = true;
			
			if (LiveIn.IsInBounds(LookAt))
			{
				Entity obj = LiveIn.GetEntity(LookAt);
				if (obj == null)
				{
					if (spendEnergy)
					{
						isAlive = SpendEnergy(BotAction.Move);
						if (!isAlive) return false;
					}

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

		protected bool SpendEnergy(BotAction action)
		{
			Energy -= GetActionCost(action);
			bool isAlive = Energy > 0;
			return isAlive;
		}

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
			if (LiveIn.IsInBounds(lookAt))
			{
				Entity entity = LiveIn.GetEntity(lookAt);
				if (entity == null) return lookAt;
			}

			int radius = 1;
			for (int i = -radius; i <= radius; i++)
			{
				for (int j = -radius; j <= radius; j++)
				{
					Cell freeCell = new Cell(point.X + i, point.Y + j);
					if (LiveIn.IsInBounds(freeCell))
					{
						Entity entity = LiveIn.GetEntity(freeCell);
						if (entity == null) return freeCell;
					}
				}
			}

			return new Cell(-1, -1);
		}

		protected Cell GetLookAt(Cell cell, BotDirection direction)
		{
			int shiftX = 0;
			int shiftY = 0;

			switch (direction)
			{
				case BotDirection.Left: shiftX = -1; shiftY = 0; break;
				case BotDirection.UpLeft: shiftX = -1; shiftY = -1; break;
				case BotDirection.Up: shiftX = 0; shiftY = -1; break;
				case BotDirection.UpRight: shiftX = 1; shiftY = -1; break;
				case BotDirection.Right: shiftX = 1; shiftY = 0; break;
				case BotDirection.DownRight: shiftX = 1; shiftY = 1; break;
				case BotDirection.Down: shiftX = 0; shiftY = 1; break;
				case BotDirection.DownLeft: shiftX = -1; shiftY = 1; break;
			}

			return new Cell(cell.X + shiftX, cell.Y + shiftY);
		}

		protected double GetCellNutritionValue(Cell cell)
		{
			double energy = 0;
			
			if (LiveIn.IsInBounds(cell))
			{
				Entity obj = LiveIn.GetEntity(cell);
				energy = obj?.Energy ?? 0;
			}

			return energy;
		}

		protected double IsRelative(Cell cell)
		{
			double isRelative = 0;
			
			if (LiveIn.IsInBounds(cell) && 
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
