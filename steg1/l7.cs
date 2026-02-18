using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace steg1
{
	internal class l7
	{
		public static List<byte> TextToBMP(List<byte> data, List<byte> text, int bitIndex)
		{
			List<byte> data2 = new List<byte>(data);
			List<bool> bitList = ByteListToBitList(text);

			int width = BitConverter.ToInt32(data.GetRange(18, 4).ToArray(), 0);
			int height = BitConverter.ToInt32(data.GetRange(22, 4).ToArray(), 0);

			int currentIndex = BitConverter.ToInt32(data.GetRange(10, 4).ToArray(), 0);

			int originalStride = 0;

			int currentBit = 0;

			Debug.WriteLine($"{height} * {width} = {height * width} ||| {data.Count}");

			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					if (currentIndex >= data2.Count)
					{
						Debug.WriteLine($"Выход за пределы массива: index={currentIndex}");
						return data2;
					}

					byte tempByte = data2[currentIndex];

					if (currentBit < bitList.Count)
					{
						if (bitList[currentBit])
							tempByte |= (byte)(1 << bitIndex);
						else
							tempByte &= (byte)~(1 << bitIndex);

						currentBit++;
					}

					data2[currentIndex] = tempByte;
					currentIndex++;
				}

				currentIndex += originalStride; 
			}

			return data2;
		}


		public static List<byte> BMPToText(List<byte> data, int bitIndex)
		{
			List<bool> bitList = new List<bool>();

			int width = BitConverter.ToInt32(data.GetRange(18, 4).ToArray(), 0);
			int height = BitConverter.ToInt32(data.GetRange(22, 4).ToArray(), 0);

			int currentIndex = BitConverter.ToInt32(data.GetRange(10, 4).ToArray(), 0);

			int originalStride = 0;

			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					if (currentIndex >= data.Count)
						return BitListToByteList(bitList);

					byte tempByte = data[currentIndex];
					bool bitValue = (tempByte & (1 << bitIndex)) != 0;
					bitList.Add(bitValue);

					currentIndex++;
				}

				currentIndex += originalStride; // = 0
			}

			return BitListToByteList(bitList);
		}


		public static List<bool> ByteListToBitList(List<byte> byteList)
		{
			BitArray bitArray = new BitArray(byteList.ToArray());
			List<bool> bitList = new List<bool>();

			foreach (bool bit in bitArray)
			{
				bitList.Add(bit);
			}

			return bitList;
		}


		public static List<byte> BitListToByteList(List<bool> bitList)
		{
			List<byte> byteList = new List<byte>();
			int byteCount = (bitList.Count + 7) / 8; 

			for (int i = 0; i < byteCount; i++)
			{
				byte b = 0;

				
				for (int j = 0; j < 8; j++)
				{
					int bitIndex = i * 8 + j;

					
					if (bitIndex < bitList.Count && bitList[bitIndex])
					{
						b |= (byte)(1 << j); 
					}
				}

				byteList.Add(b); 
			}

			return byteList;
		}



	}
}
