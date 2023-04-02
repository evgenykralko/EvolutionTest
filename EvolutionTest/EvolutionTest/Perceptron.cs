using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace EvolutionTest
{
	[Serializable]
	public class Perceptron
	{
		public Layer[] layers;

		public Perceptron() { }

		public Perceptron(int[] neuronsPerLayer, Random random)
		{
			layers = new Layer[neuronsPerLayer.Length];

			for (int i = 0; i < layers.Length; i++)
			{
				int numberOfInpits = i == 0 ? neuronsPerLayer[i] : neuronsPerLayer[i - 1];
				layers[i] = new Layer(neuronsPerLayer[i], numberOfInpits, random);
			}
		}

		public Perceptron Copy()
		{
			Perceptron copy = new Perceptron();
			copy.layers = new Layer[layers.Length];

			for (int i = 0; i < layers.Length; i++)
			{
				copy.layers[i] = layers[i].Copy();
			}

			return copy;
		}

		public double[] Activate(double[] inputs)
		{
			double[] outputs = new double[0];
			for (int i = 1; i < layers.Length; i++)
			{
				outputs = layers[i].Activate(inputs);
				inputs = outputs;
			}

			return outputs;
		}

		double IndividualError(double[] realOutput, double[] desiredOutput)
		{
			double err = 0;
			for (int i = 0; i < realOutput.Length; i++)
			{
				err += Math.Pow(realOutput[i] - desiredOutput[i], 2);
			}
			return err;
		}

		double GeneralError(List<double[]> input, List<double[]> desiredOutput)
		{
			double error = 0;
			for (int i = 0; i < input.Count; i++)
			{
				error += IndividualError(Activate(input[i]), desiredOutput[i]);
			}

			return error;
		}

		List<string> log;
		public bool Learn(List<double[]> input, List<double[]> desiredOutput, double alpha, double maxError, int maxIterations, string net_path = null, int iter_save = 1)
		{
			double error = 99999;
			log = new List<string>();
			int it = maxIterations;
			while (error > maxError)
			{
				ApplyBackPropagation(input, desiredOutput, alpha);
				error = GeneralError(input, desiredOutput);

				if ((it - maxIterations) % 1000 == 0)
				{
					Console.WriteLine(error + " iterations: " + (it - maxIterations));
				}


				if (net_path != null)
				{
					if ((it - maxIterations) % iter_save == 0)
					{
						Save(net_path);
						Console.WriteLine("Save net to " + net_path);
					}
				}

				log.Add(error.ToString());
				maxIterations--;

				if (Console.KeyAvailable)
				{
					File.WriteAllLines(@"LogTail.txt", log.ToArray());
					return true;
				}

				if (maxIterations <= 0)
				{
					Console.WriteLine("MINIMO LOCAL");
					File.WriteAllLines(@"LogTail.txt", log.ToArray());
					return false;
				}

			}

			File.WriteAllLines(@"LogTail.txt", log.ToArray());
			return true;
		}

		List<double[]> sigmas;
		List<double[,]> deltas;

		void SetSigmas(double[] desiredOutput)
		{
			sigmas = new List<double[]>();
			for (int i = 0; i < layers.Length; i++)
			{
				sigmas.Add(new double[layers[i].numberOfNeurons]);
			}
			for (int i = layers.Length - 1; i >= 0; i--)
			{
				for (int j = 0; j < layers[i].numberOfNeurons; j++)
				{
					if (i == layers.Length - 1)
					{
						double y = layers[i].neurons[j].lastActivation;
						sigmas[i][j] = (Neuron.Sigmoid(y) - desiredOutput[j]) * Neuron.SigmoidDerivated(y);
					}
					else
					{
						double sum = 0;
						for (int k = 0; k < layers[i + 1].numberOfNeurons; k++)
						{
							sum += layers[i + 1].neurons[k].weights[j] * sigmas[i + 1][k];
						}
						sigmas[i][j] = Neuron.SigmoidDerivated(layers[i].neurons[j].lastActivation) * sum;
					}
				}
			}
		}

		void SetDeltas()
		{
			deltas = new List<double[,]>();
			for (int i = 0; i < layers.Length; i++)
			{
				deltas.Add(new double[layers[i].numberOfNeurons, layers[i].neurons[0].weights.Length]);
			}
		}

		void AddDelta()
		{
			for (int i = 1; i < layers.Length; i++)
			{
				for (int j = 0; j < layers[i].numberOfNeurons; j++)
				{
					for (int k = 0; k < layers[i].neurons[j].weights.Length; k++)
					{
						deltas[i][j, k] += sigmas[i][j] * Neuron.Sigmoid(layers[i - 1].neurons[k].lastActivation);
					}
				}
			}
		}

		void UpdateBias(double alpha)
		{
			for (int i = 0; i < layers.Length; i++)
			{
				for (int j = 0; j < layers[i].numberOfNeurons; j++)
				{
					layers[i].neurons[j].bias -= alpha * sigmas[i][j];
				}
			}
		}

		void UpdateWeights(double alpha)
		{
			for (int i = 0; i < layers.Length; i++)
			{
				for (int j = 0; j < layers[i].numberOfNeurons; j++)
				{
					for (int k = 0; k < layers[i].neurons[j].weights.Length; k++)
					{
						layers[i].neurons[j].weights[k] -= alpha * deltas[i][j, k];
					}
				}
			}
		}

		void ApplyBackPropagation(List<double[]> input, List<double[]> desiredOutput, double alpha)
		{
			SetDeltas();
			
			for (int i = 0; i < input.Count; i++)
			{
				Activate(input[i]);
				SetSigmas(desiredOutput[i]);
				UpdateBias(alpha);
				AddDelta();
			}

			UpdateWeights(alpha);
		}

		public void Save(String neuralNetworkPath)
		{
			FileStream fs = new FileStream(neuralNetworkPath, FileMode.Create);
			BinaryFormatter formatter = new BinaryFormatter();
			try
			{
				formatter.Serialize(fs, this);
			}
			catch (SerializationException e)
			{
				Console.WriteLine("Failed to serialize. Reason: " + e.Message);
				throw;
			}
			finally
			{
				fs.Close();
			}
		}

		public static Perceptron Load(String neuralNetworkPath)
		{
			FileStream fs = new FileStream(neuralNetworkPath, FileMode.Open);
			Perceptron p = null;
			try
			{
				BinaryFormatter formatter = new BinaryFormatter();

				// Deserialize the hashtable from the file and 
				// assign the reference to the local variable.
				p = (Perceptron)formatter.Deserialize(fs);
			}
			catch (SerializationException e)
			{
				Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
				throw;
			}
			finally
			{
				fs.Close();
			}

			return p;
		}
	}

	[Serializable]
	public class Layer
	{
		public Neuron[] neurons;
		public int numberOfNeurons;
		public double[] output;

		public Layer() { }

		public Layer(int numberOfNeurons, int numberOfInputs, Random random)
		{
			this.numberOfNeurons = numberOfNeurons;
			neurons = new Neuron[numberOfNeurons];
			for (int i = 0; i < numberOfNeurons; i++)
			{
				neurons[i] = new Neuron(numberOfInputs, random);
			}
		}

		public Layer Copy()
		{
			Layer copy = new Layer();
			
			copy.numberOfNeurons = numberOfNeurons;
			copy.neurons = new Neuron[numberOfNeurons];

			for (int i = 0; i < numberOfNeurons; i++)
			{
				copy.neurons[i] = neurons[i].Copy();
			}

			return copy;
		}

		public double[] Activate(double[] inputs)
		{
			double[] outputs = new double[numberOfNeurons];
			for (int i = 0; i < numberOfNeurons; i++)
			{
				outputs[i] = neurons[i].Activate(inputs);
			}

			return outputs;
		}
	}

	[Serializable]
	public class Neuron
	{
		public double[] weights;
		public double lastActivation;
		public double bias;

		private Random random;

		public Neuron() { }

		public Neuron(int numberOfInputs, Random rnd)
		{
			random = rnd;
			weights = new double[numberOfInputs];

			SetRandomBias();
			SetRandomWeights();
		}

		public Neuron Copy()
		{
			Neuron copy = new Neuron();
			
			copy.weights = new double[weights.Length];
			copy.random = random;
			copy.bias = bias;

			for (int i = 0; i < weights.Length; i++)
			{
				copy.weights[i] = weights[i];
			}

			return copy;
		}

		public void SetRandomBias()
		{
			bias = 10 * random.NextDouble() - 5;
		}

		public void SetRandomWeights()
		{
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = 10 * random.NextDouble() - 5;
			}
		}

		public double Activate(double[] inputs)
		{
			double activation = bias;

			for (int i = 0; i < weights.Length; i++)
			{
				activation += weights[i] * inputs[i];
			}

			lastActivation = activation;
			//return Sigmoid(activation);
			return activation;
		}

		public static double Sigmoid(double input)
		{
			return 1 / (1 + Math.Exp(-input));
		}

		public static double SigmoidDerivated(double input)
		{
			double y = Sigmoid(input);
			return y * (1 - y);
		}
	}
}
