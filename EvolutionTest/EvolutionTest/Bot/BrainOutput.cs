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

		public BotDirection Direction => (BotDirection)Math.Truncate(Neuron.Sigmoid(direction) / (1.0f / (double)Enum.GetValues(typeof(BotDirection)).Length));
		public bool Move => move > 0;
		public bool Photosynthesis => photosynthesis > 0;
		public bool Attack => attack > 0;
		public int Multiply => (int)multiply; //(int)Math.Truncate(multiply / (1.0f / 10.0f));

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
