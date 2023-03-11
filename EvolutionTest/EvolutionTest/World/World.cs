using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EvolutionTest
{
	public class World
	{
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Random RndGenerator = new Random();
		public Entity[,] Entities;
		public HashSet<Entity> Bots;

		public event EntityAddedEventHandler EntityAdded;
		public event EntityMovedEventHandler EntityMoved;
		public event EntityRemovedEventHandler EntityRemoved;

		public World(int width, int height)
		{
			Width = width;
			Height = height;

			Entities = new Entity[height, width];
			Bots = new HashSet<Entity>();
		}

		#region Actions

		public void Tick()
		{
			foreach (Bot bot in Bots.ToArray())
			{
				if (Bots.Contains(bot))
				{
					if (!bot.Tick())
					{
						bot.Kill();
					}
				}
			}
		}

		public void AddEntity(Entity entity, Cell cell)
		{
			Entities[cell.Y, cell.X] = entity;
			Bots.Add(entity);
			EntityAdded?.Invoke(this, new EntityAddedEventArgs() { Obj = entity, Position = cell });
		}

		public Entity GetEntity(Cell cell)
		{
			return Entities[cell.Y, cell.X];
		}

		public void MoveEntity(Entity entity, Cell from, Cell to)
		{
			Entities[from.Y, from.X] = null;
			Entities[to.Y, to.X] = entity;
			EntityMoved?.Invoke(this, new EntityMovedEventArgs() { Obj = entity, From = from, To = to });
		}

		public void RemoveEntity(Entity entity, Cell cell)
		{
			Entities[cell.Y, cell.X] = null;
			Bots.Remove(entity);
			EntityRemoved?.Invoke(this, new EntityRemovedEventArgs() { Obj = entity, Position = cell });
		}

		#endregion

		#region Utils

		public bool RollDice(int percent)
		{
			int dice = RndGenerator.Next(100);
			return dice < percent;
		}

		public bool IsInBounds(Cell cell)
		{
			return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
		}

		#endregion
	}
}
