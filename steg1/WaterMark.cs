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
			int MVLength = payloadBits.Count;
			if (payloadBits.Count > capacity)
				throw new Exception("Watermark too large");


			while (payloadBits.Count < capacity)
				payloadBits.Add(false);

			List<int> positions = GenPositions(capacity, key);

			int max = 0;
			foreach(int i in positions)
			{
				if(i> max) {  max = i; }
			}
			Debug.WriteLine($"{max}, {positions.Count}");
			// 1. Копируем битовую плоскость
			List<bool> planeBits = new List<bool>(capacity);
			for (int i = 0; i < capacity; i++)
			{
				byte temp = result[pixelOffset + i];
				planeBits.Add((temp & (1 << bitIndex)) != 0);
			}

			// 2. Вносим watermark по перемешанным позициям
			for (int i = 0; i < MVLength; i++)
			{
				int pos = positions[i]; // перемешанная позиция внутри плоскости
				planeBits[pos] = payloadBits[i]; // изменяем только нужные биты
			}

			// 3. Записываем обратно в оригинальный BMP
			for (int i = 0; i < capacity; i++)
			{
				byte temp = result[pixelOffset + i];
				if (planeBits[i])
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

			// 1. Читаем всю плоскость
			List<bool> planeBits = new List<bool>(capacity);
			for (int i = 0; i < capacity; i++)
			{
				byte temp = data[pixelOffset + i];
				planeBits.Add((temp & (1 << bitIndex)) != 0);
			}

			// 2. Генерируем те же позиции
			List<int> positions = GenPositions(capacity, key);

			// 3. Читаем длину напрямую по positions
			List<bool> lengthBits = new List<bool>(32);
			for (int i = 0; i < 32; i++)
				lengthBits.Add(planeBits[positions[i]]);

			int wmLength = BitConverter.ToInt32(
				l7.BitListToByteList(lengthBits).ToArray(), 0);

			if (wmLength <= 0 || wmLength > capacity / 8)
				throw new Exception($"Invalid watermark length: {wmLength}");

			// 4. Читаем сами данные тем же способом
			List<bool> wmBits = new List<bool>(wmLength * 8);
			for (int i = 32; i < 32 + wmLength * 8; i++)
				wmBits.Add(planeBits[positions[i]]);

			return l7.BitListToByteList(wmBits);
		}










	}
}