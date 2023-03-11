using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EvolutionTest
{
	public struct Cell : IEqualityComparer<Cell>
	{
		public int X;
		public int Y;

		public Cell(int x, int y)
		{
			X = x;
			Y = y;
		}

		public bool IsEmpty() => X < 0 || Y < 0;

		public bool Equals([AllowNull] Cell x, [AllowNull] Cell y)
		{
			return x.X == y.X && x.Y == y.Y;
		}

		public int GetHashCode([DisallowNull] Cell obj)
		{
			return $"{obj.X}-{obj.Y}".GetHashCode();
		}
	}
}
