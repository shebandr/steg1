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
            Debug.WriteLine(colorsCount);
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

    }
}
