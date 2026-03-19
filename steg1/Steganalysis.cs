using Accord.Math;
using Emgu.CV.Bioinspired;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace steg1
{
    internal static class Steganalysis
    {
		#region XI2
		public static List<List<double>> FullChiCalc(List<byte> data, int blockSize)
        {
            List<List<byte>> pixels = SelectPixelsFromBMP(data);

            int height = pixels.Count/blockSize;
            int width = pixels[0].Count/blockSize;

            List<List<double>> chi = new List<List<double>>();
            int blockNum = 0;
            for (int i = 0; i < height; i++) 
            {
                chi.Add(new List<double>());
                for(int q = 0; q < width; q++)
                {
                    List<List<byte>> tempData = GetPart(pixels, blockSize, blockNum);
                    List<int> hist = BuildHistogram(tempData);
                    chi[i].Add(ChiCalc(hist));
                    blockNum++;
                }
            }
            return chi;
        }
        public static List<byte> SelectHeaderFromBMP(List<byte> data)
        {
            int colorsCount = BitConverter.ToInt32(data.GetRange(46, 50).ToArray(), 0);
            Debug.WriteLine(colorsCount);
            byte[] width = (data.GetRange(18, 4).ToArray());
            byte[] height = (data.GetRange(22, 4).ToArray());
            int heightInt = BitConverter.ToInt32(height, 0);
            int widthInt = BitConverter.ToInt32(width, 0);
            List<byte> data2 = new List<byte>(data).Take(54).ToList();
            return data2;

        }

        public static List<List<byte>> SelectPixelsFromBMP(List<byte> data)
        {
            int colorsCount = BitConverter.ToInt32(data.GetRange(46, 50).ToArray(), 0);
            //Debug.WriteLine(colorsCount);
            byte[] width = (data.GetRange(18, 4).ToArray());
            byte[] height = (data.GetRange(22, 4).ToArray());
            int heightInt = BitConverter.ToInt32(height, 0);
            int widthInt = BitConverter.ToInt32(width, 0);
            List<List<byte>> data2 = new List<List<byte>>();


            int originalStride = (((data.Count - 54) / heightInt) % widthInt);
            int currentIndex = 54;
            for (int i = 0; i < heightInt; i++)
            {
                data2.Add(new List<byte>());
                for (int j = 0; j < widthInt; j++)
                {
                    data2[i].Add(data[currentIndex]);
                    currentIndex++;
                }
                currentIndex += originalStride;
            }

            return data2;
        }

        public static List<List<byte>> GetPart(List<List<byte>> data, int size, int number) // number - номер текущего блока, считая с нуля, по нему считается отступ
        {
            List<List<byte>> result = new List<List<byte>>();

            int blocksPerRow = data[0].Count / size;

            int xStart = (number % blocksPerRow) * size;
            int yStart = (number / blocksPerRow) * size;

            for (int i = 0; i < data.Count; i++)
            {
                if(i >= yStart && i < yStart + size)
                {
                    result.Add(new List<byte>());
                }
                for(int q = 0; q < data[0].Count; q++)
                {
                    if (i >= yStart && i < yStart + size && q >= xStart && q < xStart + size)
                    {
                        result.Last().Add(data[i][q]);
                    }
                }
            }

            return result;
        }
        public static List<int> BuildHistogram(List<List<byte>> data)
        {

            List<int> hist = Enumerable.Repeat(0, 256).ToList();

            for (int y = 0; y < data.Count; y++)
            {
                for (int x = 0; x < data[0].Count; x++)
                {
                    hist[data[y][x]]++;
                }
            }
            return hist;
        }

        public static double ChiCalc(List<int> hist)
        {
            double chi = 0;

            for (int i = 0; i < 128; i++)
            {
                int even = hist[2 * i];
                int odd = hist[2 * i + 1];

                double expected = (even + odd) / 2.0;

                if (expected > 0)
                    chi += Math.Pow(odd - expected, 2) / expected;
            }
            return chi;
        }

		#endregion

		#region RS
		private static int Smoothness(byte[] block)
		{
			int sum = 0;
			for (int i = 0; i < block.Length - 1; i++)
			{
				sum += Math.Abs(block[i] - block[i + 1]);
			}
			return sum;
		}

		static byte[] ApplyMask(byte[] block, int[] mask)
		{
			byte[] result = new byte[block.Length];

			for (int i = 0; i < block.Length; i++)
			{
				int t = block[i] + mask[i];
				if(t > 255)
				{
					t = 0;
				}
				if (t < 0)
				{
					t = 255;
				}
				result[i] = (byte)t;

			}

			return result;
		}

		static int Classify(byte[] block, int[] mask)
		{
			int fOriginal = Smoothness(block);
			byte[] flipped = ApplyMask(block, mask);
			int fFlipped = Smoothness(flipped);

			if (fFlipped > fOriginal)
				return 1; // Regular
			else if (fFlipped < fOriginal)
				return -1; // Singular
			else
				return 0; // Unusable
		}

		public static (double Rm, double Sm, double RmInv, double SmInv) RSAnalysis(byte[] pixels)
		{
			int[] mask = { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 };    
			int[] maskInv = { -1, -1, -1, -1, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0 };

			int groupSize = 16;

			int Rm = 0, Sm = 0;
			int RmInv = 0, SmInv = 0;

			for (int i = 0; i <= pixels.Length - groupSize; i += groupSize)
			{
				byte[] block = new byte[groupSize];
				Array.Copy(pixels, i, block, 0, groupSize);

				int res = Classify(block, mask);
				if (res == 1) Rm++;
				else if (res == -1) Sm++;

				int resInv = Classify(block, maskInv);
				if (resInv == 1) RmInv++;
				else if (resInv == -1) SmInv++;
			
				
			}

			return (Rm, Sm, RmInv, SmInv);
		}

		public static double RunRSAnalysis(List<byte> data)
		{

			List<List<byte>> pixels2D = Steganalysis.SelectPixelsFromBMP(data);

			List<byte> pixels1D = new List<byte>();

			for (int y = 0; y < pixels2D.Count; y++)
			{
				for (int x = 0; x < pixels2D[0].Count; x++)
				{
					pixels1D.Add(pixels2D[y][x]);
				}
			}

			var result = RSAnalysis(pixels1D.ToArray());

			double Rm = result.Rm;
			double Sm = result.Sm;
			double RmInv = result.RmInv;
			double SmInv = result.SmInv;
			//Debug.WriteLine($"Rm={Rm} Sm={Sm} RmInv{RmInv} SmInv{SmInv}");

			double numerator = (Rm - Sm) - (RmInv - SmInv);
			double denominator = (Rm - Sm) + (RmInv - SmInv);

			double p = 0;
			if (Math.Abs(denominator) > 1e-10)
			{
				p = numerator / denominator;
			}
			//Debug.WriteLine($"{p} = {numerator}/{denominator}");

			double percent = Math.Abs(p) * 100.0;


			//Debug.WriteLine($"Оценка скрытия: {percent:F2}%");

		    return percent;

		}
		#endregion
	}
}
