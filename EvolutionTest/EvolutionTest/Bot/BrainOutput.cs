using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EvolutionTest
{
	public class BrainOutput
	{
		private double direction;
		private double move;
		private double photosynthesis;
		private double attack;
		private double multiply;
		private double energyToGive;

		public int Direction
		{
			get
			{
				int index = Math.Abs((int)(direction % Bot.Directions.Length));
				return (index == 0 ? Bot.Directions.Length : index) - 1;
			}
		}
		public bool Move => move > 0;
		public bool Photosynthesis => photosynthesis > 0;
		public bool Attack => attack > 0;
		public int Multiply => (int)multiply;
		public double EnergyToGive => energyToGive;

		public BrainOutput(double[] outputs)
		{
			FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

			if (outputs.Length != fields.Length)
				throw new Exception($"{typeof(BrainOutput)} fields count is not equal to {outputs} count.");

			for (int i = 0; i < fields.Length; i++)
			{
				fields[i].SetValue(this, outputs[i]);
			}
		}
	}
}
