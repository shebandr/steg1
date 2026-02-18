using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;


namespace steg1
{
    internal class WaterMark
    {

		private static int GetStableSeed(string key)
		{
			string normalizedKey = key.Normalize(NormalizationForm.FormC);
			byte[] keyBytes = Encoding.UTF8.GetBytes(normalizedKey);

			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(keyBytes);

				int seed = 0;
				for (int i = 0; i < hash.Length; i += 4)
				{
					int part = BitConverter.ToInt32(hash, i);
					seed ^= part;
				}
				seed = Math.Abs(seed % int.MaxValue);
				return seed;
			}
		}



		public static int getMaxSizeForContainer(List<byte> data)
        {
            byte[] width = (data.GetRange(18, 4).ToArray());
            byte[] height = (data.GetRange(22, 4).ToArray());
            return (BitConverter.ToInt32(width) * BitConverter.ToInt32(height))/8*8;
        }

		public static List<int> GenPositions(int size, string key)
		{
			Debug.WriteLine($"ключ = {key}");

			List<int> positions = new List<int>(size);

			for (int i = 0; i < size; i++)
				positions.Add(i);

			int seed = GetStableSeed(key);
			Debug.WriteLine($"ключ = {seed}");
			Random rnd = new Random(seed);

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

			List<T> result = new List<T>(new T[array.Count]);

			for (int i = 0; i < array.Count; i++)
			{
				result[i] = array[positions[i]];
			}

			return result;
		}

		public static List<T> UnshuffleArray<T>(List<T> shuffled, List<int> positions)
		{
			if (shuffled.Count != positions.Count)
				throw new ArgumentException("Shuffled and positions must have same length");

			List<T> result = new List<T>(new T[shuffled.Count]);

			for (int i = 0; i < shuffled.Count; i++)
			{
				result[positions[i]] = shuffled[i];
			}

			return result;
		}


		public static List<byte> SetWMToBMP(List<byte> data, List<byte> watermarkBytes, string key, int bitIndex)
		{
			List<byte> result = new List<byte>(data);

			int pixelOffset = BitConverter.ToInt32(data.GetRange(10, 4).ToArray(), 0);
			int capacity = data.Count - pixelOffset;

			int wmLength = watermarkBytes.Count;
			byte[] lengthBytes = BitConverter.GetBytes(wmLength);

			List<byte> payloadBytes = new List<byte>();
			payloadBytes.AddRange(lengthBytes);
			payloadBytes.AddRange(watermarkBytes);

			List<bool> payloadBits = l7.ByteListToBitList(payloadBytes);

			if (payloadBits.Count > capacity)
				throw new Exception("Watermark too large");

			while (payloadBits.Count < capacity)
				payloadBits.Add(false);

			List<int> positions = GenPositions(capacity, key);

			List<bool> shuffledBits = new List<bool>(payloadBits.Count);
			for (int i = 0; i < payloadBits.Count; i++)
				shuffledBits.Add(payloadBits[positions[i]]);

			for (int i = 0; i < capacity; i++)
			{
				byte temp = result[pixelOffset + i];

				if (shuffledBits[i])
					temp |= (byte)(1 << bitIndex);
				else
					temp &= (byte)~(1 << bitIndex);

				result[pixelOffset + i] = temp;
			}

			return result;
		}

		public static List<byte> GetWMFromBMP(List<byte> data, string key, int bitIndex)
		{
			int pixelOffset = BitConverter.ToInt32(data.GetRange(10, 4).ToArray(), 0);
			int capacity = data.Count - pixelOffset;

			List<bool> bits = new List<bool>(capacity);
			for (int i = 0; i < capacity; i++)
			{
				byte temp = data[pixelOffset + i];
				bits.Add((temp & (1 << bitIndex)) != 0);
			}

			List<int> positions = GenPositions(capacity, key);

			List<bool> unshuffled = new List<bool>(bits.Count);
			for (int i = 0; i < bits.Count; i++)
				unshuffled.Add(false);
			for (int i = 0; i < bits.Count; i++)
				unshuffled[positions[i]] = bits[i];

			int wmLength = BitConverter.ToInt32(l7.BitListToByteList(unshuffled.Take(32).ToList()).ToArray(), 0);

			if (wmLength <= 0 || wmLength > capacity / 8)
				throw new Exception($"Invalid watermark length: {wmLength}");

			List<bool> wmBits = unshuffled.Skip(32).Take(wmLength * 8).ToList();

			return l7.BitListToByteList(wmBits);
		}









	}
}