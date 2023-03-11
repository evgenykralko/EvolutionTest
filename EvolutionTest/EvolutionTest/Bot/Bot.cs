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
		public const int MaxAge = 10;
		public const int MutationChance = 30;

		public Perceptron Brain;
		public Bot Parent;
		public Cell LookAt;

		public BotDirection Direction { get; private set; }

		public Bot(World liveIn, Cell position, Color color, double energy = InitialEnergy, Bot parent = null)
			: base(liveIn, position)
		{
			Energy = energy;
			Background = color;

			if (parent != null)
			{
				Parent = parent;
				Brain = parent.Brain.Copy();

				if (LiveIn.RollDice(MutationChance))
				{
					Mutate();
				}
			}
			else
			{
				int[] definition = new int[] { 5, 6, 6, 5 };
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
			if (!base.Tick()) return false;

			bool ok = true;

			if (!SpendEnergy(BotAction.Tick)) return false;

			if (Age > MaxAge) return false;

			LookAt = GetLookAt(Position, Direction);

			BrainOutput output = Think();

			if (output.Multiply > 0)
			{
				Multiply(output.Multiply);
			}

			if (output.Attack)
			{
				if (!SpendEnergy(BotAction.Attack)) return false;

				Attack();
			}
			else
			{
				Rotate(output.Direction);

				if (output.Move)
				{
					if (!SpendEnergy(BotAction.Move)) return false;

					Move();
				}
				else if (output.Photosynthesis)
				{
					Photosynthesis();
				}
			}

			return ok;
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

		protected virtual void Multiply(int multiply, double energyToPass = 0.5f)
		{
			for (int i = 0; i < multiply; i++)
			{
				Cell freeSpace = FindFreeNeighbourCell(Position, LookAt);
				if (!freeSpace.IsEmpty())
				{
					double toGive = Energy * energyToPass + GetActionCost(BotAction.Multiply);
					if (toGive >= Energy) break;

					Energy -= toGive;
					SpendEnergy(BotAction.Multiply);

					Bot child = new Bot(LiveIn, freeSpace, Background, toGive, this);
					LiveIn.AddEntity(child, freeSpace);
				}
				else break;
			}
		}

		protected virtual void Attack()
		{
			if (!LiveIn.IsInBounds(LookAt)) return;

			Entity obj = LiveIn.GetEntity(LookAt);
			if (obj is Bot bot)
			{
				TakeEnergy(bot.Energy);
				bot.Kill();

				Move();
			}
		}

		protected virtual void Rotate(BotDirection desiredDirection)
		{
			if (desiredDirection != Direction)
			{
				SpendEnergy(BotAction.Rotate);
				Direction = desiredDirection;
			}
		}

		protected virtual void Move()
		{
			if (!LiveIn.IsInBounds(LookAt)) return;

			Entity obj = LiveIn.GetEntity(LookAt);
			if (obj == null)
			{
				Cell from = Position;
				Cell to = LookAt;

				Position = LookAt;
				LiveIn.MoveEntity(this, from, to);
			}
		}

		protected virtual void Photosynthesis()
		{
			TakeEnergy(25.0f);
		}

		#endregion

		#region Utils

		protected void TakeEnergy(double energy)
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
			return Energy > 0;
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

		protected Cell FindFreeNeighbourCell(Cell point, Cell lookAt)
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
			if (LiveIn.IsInBounds(cell))
			{
				Entity obj = LiveIn.GetEntity(cell);
				return obj?.Energy ?? 0.0f;
			}

			return 0.0f;
		}

		protected double IsRelative(Cell cell)
		{
			if (LiveIn.IsInBounds(cell))
			{
				Entity obj = LiveIn.GetEntity(cell);
				if (obj is Bot bot && bot.Background == Background) return 1.0f;
			}

			return 0.0f;
		}

		#endregion
	}
}
