using System;
using System.Linq;

namespace steg1
{
	public static class Tardos
	{
		private static int CodeLength;
		private static int N;
		private static char[][] Code;
		private static int c;
		private static double[] P;
		private static double k;
		private static Random rnd = new Random();


		private static double GetRND(double a, double b)
		{
			return rnd.NextDouble() * (b - a) + a;
		}

		private static double Sqr(double x)
		{
			return x * x;
		}

		private static char GenOne(double prob)
		{
			return (rnd.NextDouble() < prob) ? '1' : '0';
		}

		private static double CalcScore(int i, char[] y)
		{
			double score = 0;
			double U;

			for (int j = 0; j < CodeLength; j++)
			{
				if (Code[i][j] == '1')
					U = Math.Sqrt((1 - P[j]) / P[j]);
				else
					U = -Math.Sqrt(P[j] / (1 - P[j]));

				score += (y[j] - '0') * U;
			}

			return score;
		}


		public static void BuildCode(int CNT, double c0, double eps)
		{
			N = CNT;
			k = Math.Ceiling(Math.Log(1 / eps));
			c = (int)c0;
			CodeLength = (int)(100 * c * c * k);

			P = new double[CodeLength];
			Code = new char[N][];

			for (int i = 0; i < N; i++)
				Code[i] = new char[CodeLength];

			double t = 1.0 / (300.0 * c);
			double t_ = Math.Asin(Math.Sqrt(t));

			for (int j = 0; j < CodeLength; j++)
			{
				double r = GetRND(t_, Math.PI / 2 - t_);
				P[j] = Sqr(Math.Sin(r));
			}

			for (int i = 0; i < N; i++)
			{
				for (int j = 0; j < CodeLength; j++)
				{
					Code[i][j] = GenOne(P[j]);
				}
			}
		}

		public static char[][] GetCodeTable()
		{
			return Code;
		}

		public static char[] GetCodeWord(int i)
		{
			return Code[i];
		}

		public static (int users, int codeLength, int collisionSize) GetInfo()
		{
			return (N, CodeLength, c);
		}

		public static char[] Collide(int[] attackers)
		{
			char[] y = new char[CodeLength];

			for (int j = 0; j < CodeLength; j++)
			{
				char first = Code[attackers[0]][j];
				bool allEqual = true;

				for (int k = 1; k < attackers.Length; k++)
				{
					if (Code[attackers[k]][j] != first)
					{
						allEqual = false;
						break;
					}
				}

				if (allEqual)
				{
					y[j] = first;
				}
				else
				{
					y[j] = (char)('0' + rnd.Next(2));
				}
			}

			return y;
		}

		public static (int user, double score)[] Detect(char[] y)
		{
			var result = new (int user, double score)[N];

			for (int i = 0; i < N; i++)
			{
				result[i] = (i, CalcScore(i, y));
			}

			return result
				.OrderByDescending(x => x.score)
				.ToArray();
		}
	}
}