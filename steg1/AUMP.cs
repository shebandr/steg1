using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steg1
{
	internal class AUMP
	{
		public static double Aump(byte[][] X, int m = 16, int d = 2)
		{
			double sig_th = 1.0; // Порог для числовой стабильности
			int rows = X.Length;
			int cols = X[0].Length;

			// Конвертация в double
			double[][] Xdouble = new double[rows][];
			for (int i = 0; i < rows; i++)
			{
				Xdouble[i] = new double[cols];
				for (int j = 0; j < cols; j++)
					Xdouble[i][j] = X[i][j];
			}

			// Предсказание и веса
			var (Xpred, S, w) = PredAump(Xdouble, m, d, sig_th);

			// Остаток
			double[][] r = new double[rows][];
			for (int i = 0; i < rows; i++)
			{
				r[i] = new double[cols];
				for (int j = 0; j < cols; j++)
					r[i][j] = Xdouble[i][j] - Xpred[i][j];
			}

			// Инвертирование LSB
			double[][] Xbar = new double[rows][];
			for (int i = 0; i < rows; i++)
			{
				Xbar[i] = new double[cols];
				for (int j = 0; j < cols; j++)
					Xbar[i][j] = Xdouble[i][j] + 1 - 2 * (X[i][j] % 2);
			}

			// Статистика обнаружения
			double beta = 0;
			for (int i = 0; i < rows; i++)
				for (int j = 0; j < cols; j++)
					beta += w[i][j] * (Xdouble[i][j] - Xbar[i][j]) * r[i][j];

			return beta;
		}

		/// <summary>
		/// Sample Pairs Analysis (Dumitrescu, 2002)
		/// </summary>
		/// <param name="X">2D массив яркостей пикселей</param>
		/// <returns>Оценка длины сообщения beta_hat</returns>
		public static double SamplePairs(byte[][] X)
		{
			int M = X.Length;
			int N = X[0].Length;

			int Xc = 0, Zc = 0, Wc = 0;

			for (int i = 0; i < M; i++)
			{
				for (int j = 0; j < N - 1; j++) // Только горизонтальные пары
				{
					int u = X[i][j];
					int v = X[i][j + 1];

					// X = { (u,v) | v четное и u<v или v нечетное и u>v}
					if ((v % 2 == 0 && u < v) || (v % 2 == 1 && u > v))
						Xc++;

					// Z = { (u,v) | u = v}
					if (u == v)
						Zc++;

					// W = { (u,v) | (u = 2k and v = 2k+1) or (u = 2k+1 and v = 2k)}
					if ((u / 2) == (v / 2) && u != v)
						Wc++;
				}
			}

			int Vc = M * (N - 1) - (Xc + Zc + Wc);

			double a = (Wc + Zc) / 2.0;
			double b = 2 * Xc - M * (N - 1);
			double c = Vc + Wc - Xc;

			double D = b * b - 4 * a * c;
			double beta_hat = -1;

			if (a > 0 && D >= 0)
			{
				double p1 = (-b + Math.Sqrt(D)) / (2 * a);
				double p2 = (-b - Math.Sqrt(D)) / (2 * a);
				beta_hat = Math.Min(p1, p2);
			}

			return Math.Max(0, Math.Min(1, beta_hat)); // Ограничиваем [0,1]
		}

		/// <summary>
		/// Triples message length estimator (Ker, 2005)
		/// </summary>
		/// <param name="X">2D массив яркостей пикселей</param>
		/// <returns>Оценка длины сообщения beta_hat</returns>
		public static double Triples(byte[][] X)
		{
			int M = X.Length;
			int N = X[0].Length;

			// Двумерные массивы 25x25 (индексы от -12 до 12, смещение +12)
			double[,] e = new double[25, 25];
			double[,] o = new double[25, 25];

			for (int i = 0; i < M; i++)
			{
				for (int j = 0; j < N - 2; j++) // Только горизонтальные тройки
				{
					int L = X[i][j];
					int C = X[i][j + 1];
					int R = X[i][j + 2];

					int T1 = C - L; // s2 - s1
					int T2 = R - C; // s3 - s2

					int m1 = T1 + 12; // Смещение, чтобы индекс был от 0 до 24
					int m2 = T2 + 12;

					if (m1 >= 0 && m1 < 25 && m2 >= 0 && m2 < 25)
					{
						if (L % 2 == 0) // L четное
							e[m1, m2] += 1;
						else
							o[m1, m2] += 1;
					}
				}
			}

			// Инициализация массивов для m,n от -5 до 5
			double[,] c0 = new double[11, 11];
			double[,] c1 = new double[11, 11];
			double[,] c2 = new double[11, 11];
			double[,] c3 = new double[11, 11];

			for (int mi = -5; mi <= 5; mi++)
			{
				for (int ni = -5; ni <= 5; ni++)
				{
					int m = mi + 5; // Индекс в массивах c0-c3 (0-10)
					int n = ni + 5;

					// Вычисление d0, d1, d2, d3 согласно формуле из статьи
					// Для каждого сочетания mi, ni берем значения из e и o с соответствующими индексами
					// Индексы в e и o: 2m±1, 2n±1 и т.д. со смещением +12

					// d0: e(2m+1,2n+1) - o(2m+1,2n+1)
					int idx_d0_1 = 2 * mi + 1 + 12;
					int idx_d0_2 = 2 * ni + 1 + 12;
					double d0 = SafeGet(e, idx_d0_1, idx_d0_2) - SafeGet(o, idx_d0_1, idx_d0_2);

					// d1: e(2m+1,2n+2)+e(2m,2n+2)+o(2m,2n+1)-o(2m+1,2n)-o(2m+2,2n)-e(2m+2,2n+1)
					double d1 = SafeGet(e, 2 * mi + 1 + 12, 2 * ni + 2 + 12) +
								SafeGet(e, 2 * mi + 12, 2 * ni + 2 + 12) +
								SafeGet(o, 2 * mi + 12, 2 * ni + 1 + 12) -
								SafeGet(o, 2 * mi + 1 + 12, 2 * ni + 12) -
								SafeGet(o, 2 * mi + 2 + 12, 2 * ni + 12) -
								SafeGet(e, 2 * mi + 2 + 12, 2 * ni + 1 + 12);

					// d2: e(2m,2n+3)+o(2m-1,2n+2)+o(2m,2n+2)-o(2m+2,2n-1)-e(2m+2,2n)-e(2m+3,2n)
					double d2 = SafeGet(e, 2 * mi + 12, 2 * ni + 3 + 12) +
								SafeGet(o, 2 * mi - 1 + 12, 2 * ni + 2 + 12) +
								SafeGet(o, 2 * mi + 12, 2 * ni + 2 + 12) -
								SafeGet(o, 2 * mi + 2 + 12, 2 * ni - 1 + 12) -
								SafeGet(e, 2 * mi + 2 + 12, 2 * ni + 12) -
								SafeGet(e, 2 * mi + 3 + 12, 2 * ni + 12);

					// d3: o(2m-1,2n+3)-e(2m+3,2n-1)
					double d3 = SafeGet(o, 2 * mi - 1 + 12, 2 * ni + 3 + 12) -
								SafeGet(e, 2 * mi + 3 + 12, 2 * ni - 1 + 12);

					// Вычисление коэффициентов c0-c3
					c0[m, n] = d0 + d1 + d2 + d3;
					c1[m, n] = 3 * d0 + d1 - d2 - 3 * d3;
					c2[m, n] = 3 * d0 - d1 - d2 + 3 * d3;
					c3[m, n] = d0 - d1 + d2 - d3;
				}
			}

			// Поиск корня квинтики бинарным поиском
			double epsilon = 1e-9;
			double left = 0.6;
			double right = 7.0;
			double FL = Quintic(left, c0, c1, c2, c3);
			double FR = Quintic(right, c0, c1, c2, c3);

			double q_hat = -1;
			if (FL * FR <= 0)
			{
				while (right - left > epsilon)
				{
					double mid = (left + right) / 2;
					double Fmid = Quintic(mid, c0, c1, c2, c3);

					if (Fmid * FL <= 0)
					{
						right = mid;
						FR = Fmid;
					}
					else
					{
						left = mid;
						FL = Fmid;
					}
				}
				q_hat = (left + right) / 2;
			}

			// Возвращаем оценку длины сообщения
			return q_hat > 0 ? 0.5 * (1 - 1 / q_hat) : -1;
		}

		// Вспомогательный метод для безопасного доступа к элементам двумерного массива
		private static double SafeGet(double[,] array, int i, int j)
		{
			if (i >= 0 && i < array.GetLength(0) && j >= 0 && j < array.GetLength(1))
				return array[i, j];
			return 0;
		}

		// Квинтика для поиска корня
		private static double Quintic(double q, double[,] c0, double[,] c1, double[,] c2, double[,] c3)
		{
			int rows = c0.GetLength(0);
			int cols = c0.GetLength(1);
			double y = 0;

			for (int m = 0; m < rows; m++)
			{
				for (int n = 0; n < cols; n++)
				{
					y += 2 * c0[m, n] * c1[m, n] +
						 q * (4 * c0[m, n] * c2[m, n] + 2 * c1[m, n] * c1[m, n]) +
						 q * q * (6 * c0[m, n] * c3[m, n] + 6 * c1[m, n] * c2[m, n]) +
						 q * q * q * (4 * c2[m, n] * c2[m, n] + 8 * c1[m, n] * c3[m, n]) +
						 q * q * q * q * 10 * c2[m, n] * c3[m, n] +
						 q * q * q * q * q * 6 * c3[m, n] * c3[m, n];
				}
			}
			return y;
		}

		/// <summary>
		/// Weighted-Stego LSB change-rate estimator (Ker & Bohme, 2008)
		/// </summary>
		/// <param name="S">2D массив яркостей пикселей</param>
		/// <param name="applyBiasCorrection">Применять ли коррекцию смещения</param>
		/// <returns>Оценка скорости изменений beta_hat</returns>
		public static double WeightedStego(byte[][] S, bool applyBiasCorrection = false)
		{
			int M = S.Length;
			int N = S[0].Length;

			double[][] Sdouble = new double[M][];
			for (int i = 0; i < M; i++)
			{
				Sdouble[i] = new double[N];
				for (int j = 0; j < N; j++)
					Sdouble[i][j] = S[i][j];
			}

			// Стего с инвертированными LSB
			double[][] Sbar = new double[M][];
			for (int i = 0; i < M; i++)
			{
				Sbar[i] = new double[N];
				for (int j = 0; j < N; j++)
					Sbar[i][j] = Sdouble[i][j] + 1 - 2 * (S[i][j] % 2);
			}

			// Локальная дисперсия
			double[][] localVar = LocalVariance(Sdouble, 3);

			// Веса (исключая границы)
			int Istart = 1, Iend = M - 1;
			int Jstart = 1, Jend = N - 1;
			double sumW = 0;
			double[][] w = new double[M][];
			for (int i = 0; i < M; i++)
			{
				w[i] = new double[N];
				for (int j = 0; j < N; j++)
				{
					if (i >= Istart && i < Iend && j >= Jstart && j < Jend)
					{
						w[i][j] = 1.0 / (5.0 + localVar[i][j]);
						sumW += w[i][j];
					}
					else
						w[i][j] = 0;
				}
			}

			// Нормализация весов
			for (int i = 0; i < M; i++)
				for (int j = 0; j < N; j++)
					w[i][j] /= sumW;

			// Оценка cover изображения (KB kernel)
			double[][] Xhat = new double[M][];
			for (int i = 0; i < M; i++)
			{
				Xhat[i] = new double[N];
				for (int j = 0; j < N; j++)
					Xhat[i][j] = 0;
			}

			for (int i = Istart; i < Iend; i++)
			{
				for (int j = Jstart; j < Jend; j++)
				{
					Xhat[i][j] = 0.25 * (
						-Sdouble[i - 1][j - 1] - Sdouble[i + 1][j - 1] -
						Sdouble[i + 1][j + 1] - Sdouble[i - 1][j + 1] +
						2 * (Sdouble[i][j - 1] + Sdouble[i][j + 1] +
							 Sdouble[i - 1][j] + Sdouble[i + 1][j])
					);
				}
			}

			// Оценка скорости изменений
			double beta = 0;
			for (int i = Istart; i < Iend; i++)
				for (int j = Jstart; j < Jend; j++)
					beta += w[i][j] * (Sdouble[i][j] - Xhat[i][j]) * (Sdouble[i][j] - Sbar[i][j]);

			// Коррекция смещения
			if (applyBiasCorrection)
			{
				double[][] D = new double[M][];
				for (int i = 0; i < M; i++)
				{
					D[i] = new double[N];
					for (int j = 0; j < N; j++)
						D[i][j] = Sbar[i][j] - Sdouble[i][j];
				}

				double b = 0;
				for (int i = Istart; i < Iend; i++)
				{
					for (int j = Jstart; j < Jend; j++)
					{
						double FD = 0.25 * (
							-D[i - 1][j - 1] - D[i + 1][j - 1] -
							D[i + 1][j + 1] - D[i - 1][j + 1] +
							2 * (D[i][j - 1] + D[i][j + 1] +
								 D[i - 1][j] + D[i + 1][j])
						);
						b += w[i][j] * FD * (Sdouble[i][j] - Sbar[i][j]);
					}
				}
				beta += beta * b;
			}

			return beta;
		}

		#region Вспомогательные методы

		private static (double[][], double[][], double[][]) PredAump(double[][] X, int m, int d, double sig_th)
		{
			int rows = X.Length;
			int cols = X[0].Length;
			int q = d + 1;
			int Kn = (rows * cols) / m; // Количество блоков

			// Формирование матрицы Вандермонда H
			double[][] H = new double[m][];
			for (int i = 0; i < m; i++)
			{
				H[i] = new double[q];
				double x = (i + 1) / (double)m;
				for (int j = 0; j < q; j++)
					H[i][j] = Math.Pow(x, j);
			}

			// Формирование блоков Y (каждый блок - столбец)
			double[][] Y = new double[m][];
			for (int i = 0; i < m; i++)
				Y[i] = new double[Kn];

			int blockIdx = 0;
			for (int j = 0; j < cols; j += m)
			{
				for (int i = 0; i < rows; i++)
				{
					for (int k = 0; k < m && j + k < cols; k++)
					{
						Y[k][blockIdx] = X[i][j + k];
					}
					blockIdx++;
					if (blockIdx >= Kn) break;
				}
				if (blockIdx >= Kn) break;
			}

			// Решение H * p = Y методом наименьших квадратов
			double[][] p = SolveLeastSquares(H, Y);

			// Предсказанные значения
			double[][] Ypred = MultiplyMatrix(H, p);

			// Дисперсия в блоках
			double[] sig2 = new double[Kn];
			for (int k = 0; k < Kn; k++)
			{
				double sum = 0;
				for (int i = 0; i < m; i++)
				{
					double diff = Y[i][k] - Ypred[i][k];
					sum += diff * diff;
				}
				sig2[k] = sum / (m - q);
				if (sig2[k] < sig_th * sig_th)
					sig2[k] = sig_th * sig_th;
			}

			// Глобальная дисперсия
			double s_n2 = 0;
			for (int k = 0; k < Kn; k++)
				s_n2 += 1.0 / sig2[k];
			s_n2 = Kn / s_n2;

			// Веса
			double[][] w = new double[rows][];
			double[][] S = new double[rows][];
			double[][] Xpred = new double[rows][];

			for (int i = 0; i < rows; i++)
			{
				w[i] = new double[cols];
				S[i] = new double[cols];
				Xpred[i] = new double[cols];
			}

			blockIdx = 0;
			for (int j = 0; j < cols; j += m)
			{
				for (int i = 0; i < rows; i++)
				{
					for (int k = 0; k < m && j + k < cols; k++)
					{
						int col = j + k;
						Xpred[i][col] = Ypred[k][blockIdx];
						S[i][col] = sig2[blockIdx];
						w[i][col] = Math.Sqrt(s_n2 / (Kn * (m - q))) / sig2[blockIdx];
					}
					blockIdx++;
					if (blockIdx >= Kn) break;
				}
				if (blockIdx >= Kn) break;
			}

			return (Xpred, S, w);
		}

		private static double[][] SolveLeastSquares(double[][] H, double[][] Y)
		{
			// Упрощенная реализация - в реальном коде нужно использовать матричные операции
			// Здесь используется псевдо-обратная матрица через метод наименьших квадратов
			int m = H.Length;
			int q = H[0].Length;
			int Kn = Y[0].Length;

			double[][] Ht = Transpose(H);
			double[][] HtH = MultiplyMatrix(Ht, H);
			double[][] HtHinv = InvertMatrix(HtH);
			double[][] HtY = MultiplyMatrix(Ht, Y);

			return MultiplyMatrix(HtHinv, HtY);
		}

		private static double[][] Transpose(double[][] A)
		{
			int rows = A.Length;
			int cols = A[0].Length;
			double[][] result = new double[cols][];
			for (int i = 0; i < cols; i++)
			{
				result[i] = new double[rows];
				for (int j = 0; j < rows; j++)
					result[i][j] = A[j][i];
			}
			return result;
		}

		private static double[][] MultiplyMatrix(double[][] A, double[][] B)
		{
			int rowsA = A.Length;
			int colsA = A[0].Length;
			int rowsB = B.Length;
			int colsB = B[0].Length;

			if (colsA != rowsB)
				throw new ArgumentException("Несовместимые размеры матриц");

			double[][] result = new double[rowsA][];
			for (int i = 0; i < rowsA; i++)
			{
				result[i] = new double[colsB];
				for (int j = 0; j < colsB; j++)
				{
					double sum = 0;
					for (int k = 0; k < colsA; k++)
						sum += A[i][k] * B[k][j];
					result[i][j] = sum;
				}
			}
			return result;
		}

		private static double[][] InvertMatrix(double[][] A)
		{
			int n = A.Length;

			// Создаем расширенную матрицу [A | I]
			double[][] augmented = new double[n][];
			for (int i = 0; i < n; i++)
			{
				augmented[i] = new double[2 * n];
				for (int j = 0; j < n; j++)
					augmented[i][j] = A[i][j];

				// Заполняем единичную матрицу справа
				augmented[i][n + i] = 1.0;
			}

			// Прямой ход метода Гаусса
			for (int i = 0; i < n; i++)
			{
				// Поиск главного элемента
				int maxRow = i;
				for (int k = i + 1; k < n; k++)
					if (Math.Abs(augmented[k][i]) > Math.Abs(augmented[maxRow][i]))
						maxRow = k;

				// Перестановка строк
				if (maxRow != i)
				{
					double[] temp = augmented[i];
					augmented[i] = augmented[maxRow];
					augmented[maxRow] = temp;
				}

				// Проверка на вырожденность
				if (Math.Abs(augmented[i][i]) < 1e-10)
					throw new InvalidOperationException("Матрица вырождена, невозможно вычислить обратную");

				// Нормализация текущей строки
				double divisor = augmented[i][i];
				for (int j = 0; j < 2 * n; j++)
					augmented[i][j] /= divisor;

				// Обнуление остальных строк в текущем столбце
				for (int k = 0; k < n; k++)
				{
					if (k != i)
					{
						double factor = augmented[k][i];
						for (int j = 0; j < 2 * n; j++)
							augmented[k][j] -= factor * augmented[i][j];
					}
				}
			}

			// Извлечение обратной матрицы из правой части
			double[][] result = new double[n][];
			for (int i = 0; i < n; i++)
			{
				result[i] = new double[n];
				for (int j = 0; j < n; j++)
					result[i][j] = augmented[i][n + j];
			}

			return result;
		}

		private static double[][] LocalVariance(double[][] X, int K)
		{
			int M = X.Length;
			int N = X[0].Length;

			if (K % 2 == 0) K++;

			double[][] result = new double[M][];
			for (int i = 0; i < M; i++)
				result[i] = new double[N];

			for (int i = 0; i < M; i++)
			{
				for (int j = 0; j < N; j++)
				{
					int iStart = Math.Max(0, i - K / 2);
					int iEnd = Math.Min(M - 1, i + K / 2);
					int jStart = Math.Max(0, j - K / 2);
					int jEnd = Math.Min(N - 1, j + K / 2);

					int count = 0;
					double sum = 0, sum2 = 0;

					for (int ii = iStart; ii <= iEnd; ii++)
					{
						for (int jj = jStart; jj <= jEnd; jj++)
						{
							double val = X[ii][jj];
							sum += val;
							sum2 += val * val;
							count++;
						}
					}

					if (count > 1)
					{
						double mean = sum / count;
						result[i][j] = (sum2 / count) - (mean * mean);
					}
					else
					{
						result[i][j] = 0;
					}
				}
			}

			return result;
		}

		

		
		#endregion
	}
}
