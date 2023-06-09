﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace EvolutionTest
{
	public class World
	{
		public Guid Id { get; private set; }

		public int Population { get; private set; } = 0;
		public int TickCount { get; private set; } = 0;

		public int Width { get; private set; } = 0;
		public int Height { get; private set; } = 0;

		public Random RndGenerator { get; private set; }
		public Entity[,] Entities { get; private set; }
		public HashSet<Entity> Bots;

		private bool LoopX;
		private bool LoopY;

		public World(Random rndGenerator, int width, int height, bool loopX, bool loopY)
		{
			Id = Guid.NewGuid();
			RndGenerator = rndGenerator;

			Width = width;
			Height = height;

			LoopX = loopX;
			LoopY = loopY;

			Entities = new Entity[height, width];
			Bots = new HashSet<Entity>();
		}

		public void Clear()
		{
			Entities = new Entity[Height, Width];
			Bots = new HashSet<Entity>();

			Population = 0;
			TickCount = 0;
		}

		#region Actions

		public void Tick()
		{
			foreach (Bot bot in Bots.OrderByDescending(row => row.Energy).ThenBy(row => row.SortOrder).ToArray())
			{
				if (Bots.Contains(bot))
				{
					if (!bot.Tick())
					{
						bot.Kill();
					}
				}
			}

			TickCount++;
		}

		public void AddEntity(Entity entity, Cell cell)
		{
			Entities[cell.Y, cell.X] = entity;
			Bots.Add(entity);
			Population++;
		}

		public Entity GetEntity(Cell cell)
		{
			return Entities[cell.Y, cell.X];
		}

		public void MoveEntity(Entity entity, Cell from, Cell to)
		{
			Entities[from.Y, from.X] = null;
			Entities[to.Y, to.X] = entity;
		}

		public void RemoveEntity(Entity entity, Cell cell)
		{
			Entities[cell.Y, cell.X] = null;
			Bots.Remove(entity);
			Population--;
		}

		#endregion

		#region Utils

		public bool RollDice(int percent)
		{
			int dice = RndGenerator.Next(100);
			return dice < percent;
		}

		public bool IsInBounds(ref Cell cell)
		{
			int x = LoopX ? LoopCoordinate(cell.X, true) : cell.X;
			int y = LoopY ? LoopCoordinate(cell.Y, false) : cell.Y;
			cell = new Cell(x, y);

			return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
		}

		private int LoopCoordinate(int coordinate, bool isX)
		{
			int max = isX ? Width : Height;
			return coordinate < 0
				? max - 1
				: coordinate >= max
					? 0
					: coordinate;
		}

		#endregion
	}
}
