using System;
using System.Collections.Generic;
using System.Text;

namespace EvolutionTest
{
	public delegate void WorldTickEventHandler(object sender, WorldTickEventArgs e);
	public class WorldTickEventArgs
	{
	};

	public delegate void EntityAddedEventHandler(object sender, EntityAddedEventArgs e);
	public class EntityAddedEventArgs
	{
		public Entity Obj;
		public Cell Position;
	};

	public delegate void EntityMovedEventHandler(object sender, EntityMovedEventArgs e);
	public class EntityMovedEventArgs
	{
		public Entity Obj;
		public Cell From;
		public Cell To;
	};

	public delegate void EntityRemovedEventHandler(object sender, EntityRemovedEventArgs e);
	public class EntityRemovedEventArgs
	{
		public Entity Obj;
		public Cell Position;
	};
}
