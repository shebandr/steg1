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

		

		
		#endregion
	}
}
