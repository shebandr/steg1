using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace steg1
{
    class HistogramShifting
    {

        static (List<int>, List<int>) MaxMinSearch(List<int> hist, int count)
		{
			List<int> min = new List<int>();
			List<int> max = new List<int>();
			List<int> tempHist = new List<int>(hist);
			for(int q = 0; q< count; q++)
			{
				int tempMin = int.MaxValue;
				int tempIndex = 0;
				for (int i = 1; i < tempHist.Count - 1; i++)
				{
					if (tempHist[i] < tempMin && tempHist[i] >= 0)
					{
						tempMin = tempHist[i];
						tempIndex = i;
					}
					
				}
				min.Add(tempIndex);
				tempHist[tempIndex] = -1;
				Debug.WriteLine(tempMin);
			}

			min.Sort();




			max.Add(MaxSearch(new List<int>(hist), 0, min[0])); 
			for(int i = 0; i < count-1; i++)
			{
				max.Add(MaxSearch(new List<int>(hist), min[i], (min[i] + min[i + 1]) / 2)); 
				max.Add(MaxSearch(new List<int>(hist), (min[i] + min[i+1]) / 2, min[i + 1])); 

			}
			max.Add(MaxSearch(new List<int>(hist), min[count-1], hist.Count));


			List<int> selectedMax = new List<int>();


			for(int i = 0; i<count; i++)
			{
				
				int pairStart = i * 2;

				// защита от выхода за границы (последняя пара может быть одной)
				if (pairStart + 1 < max.Count)
				{
					selectedMax.Add(max[max[pairStart] > max[pairStart + 1] ? pairStart : pairStart + 1]);
				}
				else
				{
					// если остался один максимум в конце
					selectedMax.Add(max[pairStart]);
				}
				
			}

			Debug.WriteLine($"{min[0]} {min[1]} {min[2]} ");
			Debug.WriteLine($"{selectedMax[0]} {selectedMax[1]} {selectedMax[2]} ");
			return (min, selectedMax);
		}

		static int MaxSearch(List<int> hist, int L, int R)
		{
			int max = 0;
			int maxIndex = 0;
			for (int i = L; i < R; i++)
			{
				if(max < hist[i])
				{
					max = hist[i];
					maxIndex = i;
				}
			}
			return maxIndex;
		}

		public static (List<byte> markedImage, List<int> zeroPoints, List<int> peakPoints) MPPZIn(
	List<int> hist, List<byte> originalImage, List<byte> dataHide)
		{
			int wmLength = dataHide.Count;
			Debug.WriteLine(wmLength);
			byte[] lengthBytes = BitConverter.GetBytes(wmLength);

			List<byte> payloadBytes = new List<byte>();
			payloadBytes.AddRange(lengthBytes);
			payloadBytes.AddRange(dataHide);


			List<bool> dataHideBits = l7.ByteListToBitList(payloadBytes);


			int pixelOffset = BitConverter.ToInt32(originalImage.GetRange(10, 4).ToArray(), 0);
			var (zeroPoints, peakPoints) = MaxMinSearch(hist, 3);


			int capacity = 0;
			foreach (var peak in peakPoints)
				capacity += hist[peak];

			

			Debug.WriteLine($"емкость {capacity} бит или {capacity/8} байт");
			if (dataHideBits.Count > capacity)
				throw new Exception($"Слишком много данных: доступно {capacity} бит, а передано {dataHideBits.Count} бит.");

			List<byte> markedImage = new List<byte>(originalImage);

			//считаем индексы нулей для сохранения
			List<List<int>> zerosInfo = new List<List<int>>();
			


			int dataHideIndex = 0;
			for (int p = 0; p < peakPoints.Count; p++)
			{
				zerosInfo.Add(new List<int>());
				int peak = peakPoints[p];
				int zero = zeroPoints[p];
				// логика сохранения нулей
				for (int i = pixelOffset; i < markedImage.Count; i++)
				{
					byte pixel = markedImage[i];

					if (pixel == zero)
					{
						zerosInfo[p].Add(i);
					}

				}
			}

			List<int> zeros = new List<int>();
			foreach (var a in zerosInfo) 
			{
				zeros.Add(a.Count);
				zeros.AddRange(a);
			}
			zeros.Add(Int32.MaxValue);

			var zerosBytes = new List<byte>();
			foreach (var a in zeros)
			{
				zerosBytes.AddRange(BitConverter.GetBytes(a).ToList());
			}
			dataHideBits.AddRange(l7.ByteListToBitList(zerosBytes));


			Debug.WriteLine($"емкость {capacity} бит или {capacity / 8} байт, объем встраиваемых данных {dataHideBits.Count/8} байт");
			if (dataHideBits.Count > capacity)
				throw new Exception($"Слишком много данных: доступно {capacity} бит, а передано {dataHideBits.Count} бит.");

			for (int p = 0; p < peakPoints.Count; p++)
			{
				int peak = peakPoints[p];
				int zero = zeroPoints[p];

				//логика сдвига
				for (int i = pixelOffset; i < markedImage.Count; i++)
				{
					byte pixel = markedImage[i];

					if (zero < peak)
					{
						if ((int)pixel < peak && (int)pixel > zero)
						{
							pixel--;
						}
					}
					else
					{
						if ((int)pixel > peak && (int)pixel < zero)
						{
							pixel++;
						}
					}
					markedImage[i] = pixel;
				}
				
				for (int i = pixelOffset; i < markedImage.Count; i++)
				{
					byte pixel = markedImage[i];
					
					//логика записи
					//четкое разделение сразу, если 0 в данных - пишем в более младший цвет, независимо от того, будет он оригинальным пиком или модифицироваться
					if(pixel == peak)
					{
						if(dataHideIndex >= dataHideBits.Count)
						{
							break;
						}
						if (zero < peak) //пишем в пик-1
						{
							if (dataHideBits[dataHideIndex] == false)
							{
								pixel--;
							} else
							{
								//оставляем пиксель оригинальным цветом
							}
						}
						else // пишем в пик+1
						{
							if (dataHideBits[dataHideIndex] == false)
							{
								//оставляем пиксель оригинальным цветом
							}
							else
							{
								pixel++;
								
							}
						}
						markedImage[i] = pixel;
						dataHideIndex++;
					}
					
				}
				
				
			}

			return (markedImage, zeroPoints, peakPoints);
		}

		public static (List<byte> payload, List<byte>) MPPZOut(
	List<byte> markedImage, List<int> zeroPoints, List<int> peakPoints)
		{
			int pixelOffset = BitConverter.ToInt32(markedImage.GetRange(10, 4).ToArray(), 0);
			List<bool> extractedBits = new List<bool>();
			List<byte> restoredImage = new List<byte>(markedImage);

			// обрабатываем каждую пару пик–ноль
			for (int p = 0; p < peakPoints.Count; p++)
			{
				int peak = peakPoints[p];
				int zero = zeroPoints[p];



				for (int i = pixelOffset; i < markedImage.Count; i++)
				{
					byte pixel = markedImage[i];

					//логика чтения
					//четкое разделение сразу, если 0 в данных - пишем в более младший цвет, независимо от того, будет он оригинальным пиком или модифицироваться
					if (zero < peak) //пишем в пик-1
					{

						if (pixel == peak)
						{
							extractedBits.Add(true);
						}
						else if (pixel == peak - 1)
						{
							extractedBits.Add(false);
							pixel++;
						}
					}
					else // пишем в пик+1
					{
						if (pixel == peak)
						{
							extractedBits.Add(false);
						}
						else if (pixel == peak + 1)
						{
							extractedBits.Add(true);
							pixel--;
						}
					}

					markedImage[i] = pixel;


				}
			}

		
			




			List<bool> lengthBits = extractedBits.Take(32).ToList(); //восстановление исходной информации
			int wmLength = BitConverter.ToInt32(l7.BitListToByteList(lengthBits).ToArray(), 0);
			Debug.WriteLine(wmLength);
			List<bool> wmBits = extractedBits.Skip(32).Take(wmLength * 8).ToList();
			List<byte> data = l7.BitListToByteList(wmBits);


			List<bool> zerosBytesList = extractedBits.Skip(32 + wmLength * 8).ToList(); //восстановление индексов нулей

			List<byte> zerosAllBytes = l7.BitListToByteList(zerosBytesList);
			List<List<int>> zerosIndexes = new List<List<int>>();

			using (var ms = new MemoryStream(zerosAllBytes.ToArray()))
			using (var br = new BinaryReader(ms))
			{
				while (br.BaseStream.Position + 4 <= br.BaseStream.Length)
				{
					int blockLength = br.ReadInt32();
					if (blockLength == Int32.MaxValue)
						break;

					List<int> blockIndexes = new List<int>();
					for (int i = 0; i < blockLength; i++)
					{
						if (br.BaseStream.Position + 4 > br.BaseStream.Length)
							throw new Exception("Ошибка: неожиданный конец данных при чтении индексов нулей.");

						blockIndexes.Add(br.ReadInt32());
					}

					zerosIndexes.Add(blockIndexes);
				}
			}



			for (int p = 0; p < peakPoints.Count; p++) // ЛОГИКА ВОССТАНОВЛЕНИЯ
			{
				int peak = peakPoints[p];
				int zero = zeroPoints[p];

					//логика сдвига
				for (int i = pixelOffset; i < markedImage.Count; i++)
				{

					byte pixel = markedImage[i];


					if (zerosIndexes[p].Contains(i))
					{

					} else
					{
						if (zero < peak)
						{
							if ((int)pixel < peak && (int)pixel >= zero)
							{
								pixel++;
							}
						}
						else
						{
							if ((int)pixel > peak && (int)pixel <= zero)
							{
								pixel--;
							}
						}
					}

					
					markedImage[i] = pixel;
				}
			}

		


			return (data, markedImage);
		}

	}
}
