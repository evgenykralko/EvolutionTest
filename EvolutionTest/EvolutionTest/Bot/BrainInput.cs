using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EvolutionTest
{
	public class BrainInput
	{
		public double Energy;
		public double Age;
		public double Direction;
		public double Eye;
		public double Attacked;
		public double AttackedDirection;

		private static FieldInfo[] fields = typeof(BrainInput).GetFields();

		public double[] ToArray()
		{
			double[] inputs = new double[fields.Length];

			for (int i = 0; i < fields.Length; i++)
			{
				inputs[i] = (double)fields[i].GetValue(this);
			}

			return inputs;
		}
	}
}
