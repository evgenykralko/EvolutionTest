using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace EvolutionTest
{
	public class Entity
	{
		public Color Background { get; set; } = Colors.Green;
		public double Energy { get; set; } = 0.0f;
		public int Age { get; set; } = 0;

		public World LiveIn;
		public Cell Position;

		public Entity(World liveIn, Cell position)
		{
			LiveIn = liveIn;
			Position = position;
		}

		public virtual bool Tick()
		{
			Age++;

			return true;
		}

		public virtual void Kill()
		{
			LiveIn.RemoveEntity(this, Position);
		}
	}
}
