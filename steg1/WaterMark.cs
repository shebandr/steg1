using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace steg1
{
    internal class WaterMark
    {
        public static int getMaxSizeForContainer(List<byte> data)
        {
            byte[] width = (data.GetRange(18, 4).ToArray());
            byte[] height = (data.GetRange(22, 4).ToArray());
            return (BitConverter.ToInt32(width) * BitConverter.ToInt32(height))/8;
        }

        public static List<int> GenPositions(int size, string key)
        {
            List<int> positions = new List<int>();
            for(int i = 0; i<size; i++)
            {
                positions.Add(i);
            }

            int keyInt = key.GetHashCode();

            Random rnd = new Random(keyInt);

            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }

            return positions;
        }

        public static List<T> ShuffleArray<T>(List<T> array, List<int> positions)
        {
            if (array.Count != positions.Count)
                throw new ArgumentException("Array and positions must have same length");

            List<T> result = new List<T>(array.Count);

            for (int i = 0; i < positions.Count; i++)
            {
                result.Add(array[positions[i]]);
            }

            return result;
        }
        public static List<T> UnshuffleArray<T>(List<T> shuffled, List<int> positions)
        {
            if (shuffled.Count != positions.Count)
                throw new ArgumentException("Shuffled and positions must have same length");

            List<T> result = new List<T>(new T[shuffled.Count]);

            for (int i = 0; i < positions.Count; i++)
            {
                result[positions[i]] = shuffled[i];
            }

            return result;
        }

    }
}