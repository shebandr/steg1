using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace steg1
{
    internal class l4
    {
        public static List<byte> SelectByteFromBMP(List<byte> data, int byteIndex)
        {
            byteIndex = 7 - byteIndex;
            int colorsCount = BitConverter.ToInt32(data.GetRange(46, 50).ToArray(), 0);
            Debug.WriteLine(colorsCount);
            byte[] width = (data.GetRange(18, 4).ToArray());
            byte[] height = (data.GetRange(22, 4).ToArray());
            int heightInt = BitConverter.ToInt32(height, 0);
            int widthInt = BitConverter.ToInt32(width, 0);
            Debug.WriteLine($"{heightInt} * {widthInt} = {heightInt * widthInt} ||| {colorsCount} ||| {data.Count()} ||| {data.Count() - heightInt * widthInt}");
            List<byte> data2 = new List<byte>( data);

            
            int originalStride = (((data.Count - 54) / heightInt) % widthInt);
            int currentIndex = 54;
            for (int i = 0; i < heightInt; i++)
            {
                
                for (int j = 0; j < widthInt; j++)
                {
                    int bitValue = (data[currentIndex] >> (7 - byteIndex)) & 1;
                    if (bitValue == 1)
                    {
                        data2[currentIndex] = (byte)255;
                    } else
                    {
                        data2[currentIndex] = (byte)0;
                    }
                    currentIndex++;
                }
                currentIndex += originalStride;
            }



            return data2;


        }


        public static List<byte> GetBytesFromBMP(string path)
        {
            List<byte> bytes = new List<byte>();

            try
            {
                bytes = File.ReadAllBytes(path).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при чтении файла: " + ex.Message);
                return null;
            }


            return bytes;
        }

        public static void SetBytesToBMP(string path, List<byte> bytes)
        {
            try
            {
                // Записываем массив байтов в файл
                File.WriteAllBytes(path, bytes.ToArray());
                Console.WriteLine("Файл успешно записан.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи файла: {ex.Message}");
            }
        }


    }
}



