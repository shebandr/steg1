using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;


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
			return (BitConverter.ToInt32(width) * BitConverter.ToInt32(height)) / 8 * 8;
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
			foreach (int i in positions)
			{
				if (i > max) { max = i; }
			}
			Debug.WriteLine($"{max}, {positions.Count}");
			List<bool> planeBits = new List<bool>(capacity);
			for (int i = 0; i < capacity; i++)
			{
				byte temp = result[pixelOffset + i];
				planeBits.Add((temp & (1 << bitIndex)) != 0);
			}

			for (int i = 0; i < MVLength; i++)
			{
				int pos = positions[i];
				planeBits[pos] = payloadBits[i];
			}

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

			List<bool> planeBits = new List<bool>(capacity);
			for (int i = 0; i < capacity; i++)
			{
				byte temp = data[pixelOffset + i];
				planeBits.Add((temp & (1 << bitIndex)) != 0);
			}

			List<int> positions = GenPositions(capacity, key);

			List<bool> lengthBits = new List<bool>(32);
			for (int i = 0; i < 32; i++)
				lengthBits.Add(planeBits[positions[i]]);

			int wmLength = BitConverter.ToInt32(
				l7.BitListToByteList(lengthBits).ToArray(), 0);

			if (wmLength <= 0 || wmLength > capacity / 8)
				throw new Exception($"Invalid watermark length: {wmLength}");

			List<bool> wmBits = new List<bool>(wmLength * 8);
			for (int i = 32; i < 32 + wmLength * 8; i++)
				wmBits.Add(planeBits[positions[i]]);

			return l7.BitListToByteList(wmBits);
		}






		private static List<byte> GetImageWithoutBitPlane(List<byte> data, int pixelOffset, int bitIndex)
		{
			List<byte> copy = new List<byte>(data);

			for (int i = pixelOffset; i < copy.Count; i++)
			{
				copy[i] = (byte)(copy[i] & ~(1 << bitIndex));
			}

			return copy;
		}

		private static double[] ComputeGradientMap(List<byte> data, int pixelOffset, int width, int height)
		{
			int stride = ((width + 3) / 4) * 4;
			double[] grad = new double[stride * height];

			for (int y = 1; y < height - 1; y++)
			{
				for (int x = 1; x < width - 1; x++)
				{
					int idx = y * stride + x;

					int p00 = data[pixelOffset + (y - 1) * stride + (x - 1)];
					int p01 = data[pixelOffset + (y - 1) * stride + (x)];
					int p02 = data[pixelOffset + (y - 1) * stride + (x + 1)];
					int p10 = data[pixelOffset + (y) * stride + (x - 1)];
					int p12 = data[pixelOffset + (y) * stride + (x + 1)];
					int p20 = data[pixelOffset + (y + 1) * stride + (x - 1)];
					int p21 = data[pixelOffset + (y + 1) * stride + (x)];
					int p22 = data[pixelOffset + (y + 1) * stride + (x + 1)];

					int gx =
						-p00 - 2 * p10 - p20 +
						 p02 + 2 * p12 + p22;

					int gy =
						-p00 - 2 * p01 - p02 +
						 p20 + 2 * p21 + p22;

					grad[idx] = Math.Sqrt(gx * gx + gy * gy);
				}
			}

			return grad;
		}


		public static List<byte> SetWMToBMPAdaptive(List<byte> data, List<byte> watermarkBytes, string key, int bitIndex)
		{
			List<byte> result = new List<byte>(data);

			int pixelOffset = BitConverter.ToInt32(data.GetRange(10, 4).ToArray(), 0);
			int width = BitConverter.ToInt32(data.GetRange(18, 4).ToArray(), 0);
			int height = BitConverter.ToInt32(data.GetRange(22, 4).ToArray(), 0);

			int stride = ((width + 3) / 4) * 4;
			int capacity = stride * height;

			// Формируем payload
			int wmLength = watermarkBytes.Count;
			List<byte> lengthBytes = BitConverter.GetBytes(wmLength).ToList<byte>();

			List<byte> payloadBytes = new List<byte>();
			payloadBytes.AddRange(lengthBytes);
			payloadBytes.AddRange(watermarkBytes);

			List<bool> legthBits = l7.ByteListToBitList(lengthBytes);
			List<bool> watermarkBits = l7.ByteListToBitList(watermarkBytes);

            int seed = GetStableSeed(key);
            List<int> positions = GenPositions(width * height, key);

			for (int i = watermarkBits.Count - 1; i > 0; i--)
			{
				int j = positions[i] % (i + 1);
				(watermarkBits[i], watermarkBits[j]) = (watermarkBits[j], watermarkBits[i]);
			}

			List<bool> payloadBits = new List<bool>();
			payloadBits.AddRange(legthBits);
			payloadBits.AddRange(watermarkBits);

			if (payloadBits.Count > capacity)
				throw new Exception("Watermark too large");

            

            List<byte> cleanData = GetImageWithoutBitPlane(data, pixelOffset, bitIndex);

			double[] gradient = ComputeGradientMap(cleanData, pixelOffset, width, height);

			List<int> indices = Enumerable.Range(0, capacity)
										  .OrderByDescending(i => gradient[i])
										  .ToList();

			

			for (int i = 0; i < payloadBits.Count; i++)
			{
				int pos = indices[i];
				byte temp = result[pixelOffset + pos];

				if (payloadBits[i])
					temp |= (byte)(1 << bitIndex);
				else
					temp &= (byte)~(1 << bitIndex);

				result[pixelOffset + pos] = temp;
			}

			return result;
		}


        public static List<byte> GetWMFromBMPAdaptive(List<byte> data, string key, int bitIndex)
        {
            int pixelOffset = BitConverter.ToInt32(data.GetRange(10, 4).ToArray(), 0);
            int width = BitConverter.ToInt32(data.GetRange(18, 4).ToArray(), 0);
            int height = BitConverter.ToInt32(data.GetRange(22, 4).ToArray(), 0);

            int stride = ((width + 3) / 4) * 4;
            int capacity = width * height;

            // Получаем те же индексы по градиенту
            List<byte> cleanData = GetImageWithoutBitPlane(data, pixelOffset, bitIndex);
            double[] gradient = ComputeGradientMap(cleanData, pixelOffset, width, height);

            List<int> indices = Enumerable.Range(0, capacity)
                                          .OrderByDescending(i => gradient[i])
                                          .ToList();

            // 1️⃣ Сначала считываем ВСЕ потенциальные биты payload
            List<bool> allBits = new List<bool>();

            for (int i = 0; i < capacity; i++)
            {
                int pos = indices[i];
                byte temp = data[pixelOffset + pos];
                allBits.Add((temp & (1 << bitIndex)) != 0);
            }

            int seed = GetStableSeed(key);
            List<int> positions = GenPositions(width * height, key);

            // watermarkBits уже считаны из изображения
            // выполняем обратный Fisher-Yates


            // 3️⃣ Теперь первые 32 бита — длина
            List<bool> lengthBits = allBits.Take(32).ToList();

            int wmLength = BitConverter.ToInt32(
                l7.BitListToByteList(lengthBits).ToArray(), 0);

            if (wmLength <= 0 || wmLength > capacity / 8)
                throw new Exception($"Invalid watermark length: {wmLength}");

            // 4️⃣ Читаем watermark
            List<bool> wmBits = allBits.Skip(32)
                                       .Take(wmLength * 8)
                                       .ToList();

            for (int i = 1; i < wmBits.Count; i++)
            {
                int j = positions[i] % (i + 1);
                (wmBits[i], wmBits[j]) = (wmBits[j], wmBits[i]);
            }



            return l7.BitListToByteList(wmBits);
        }






    }
}