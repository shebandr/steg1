using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace GPILabs
{
	internal class l7
	{
		public static List<byte> TextToBMP(List<byte> data, List<byte> text, int bitIndex)
		{
			List<byte> data2 = new List<byte>(data);
			List<byte> result = new List<byte>(data.GetRange(0, 54));
			List<bool> bitList = ByteListToBitList(text);
			int width = BitConverter.ToInt32(data.GetRange(18, 4).ToArray(), 0);
			int height = BitConverter.ToInt32(data.GetRange(22, 4).ToArray(), 0);
			int currentIndex = 54;
			int originalStride = ((data.Count-54) / height) % width;
			int currentBit = 0;
            Debug.WriteLine($"{height} * {width} = {height* width} ||| {data.Count()} ||| {data.Count() - height * width}");

            for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					byte tempByte = data[currentIndex];
						
					if(currentBit != bitList.Count - 1)
					{
								
							
						if (bitList[currentBit])
						{
							tempByte |= (byte)(1 << bitIndex);
						}
						else
						{
							tempByte &= (byte)~(1 << bitIndex); 
						}
						currentBit++;
					}



                    data2[currentIndex] = tempByte;

					currentIndex++;
				}
				
				
			}


			return result;
		}


		public static List<byte> BMPToText(List<byte> data, int bitIndex)
		{
			List<byte> result = new List<byte>(data.GetRange(0, 54));
			List<bool> bitList = new List<bool>();
			int width = BitConverter.ToInt32(data.GetRange(18, 4).ToArray(), 0);
			int height = BitConverter.ToInt32(data.GetRange(22, 4).ToArray(), 0);
			int currentIndex = 54;
			int originalStride = ((data.Count - 54) / height) % width;
			int currentBit = 0;

			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					byte tempByte = data[currentIndex];
					bool bitValue = (tempByte & (1 << bitIndex)) != 0;
					bitList.Add(bitValue);
					currentBit++;	
					currentIndex++;
					
				}
				currentIndex += originalStride;

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
