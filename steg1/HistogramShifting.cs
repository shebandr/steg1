using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace steg1
{
    class HistogramShifting
    {

        static (List<int>, List<int>) MaxMinSearch(List<int> hist)
		{
			List<int> min = new List<int>();
			List<int> max = new List<int>();
			List<int> tempHist = new List<int>(hist);
			for(int q = 0; q<3; q++)
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

			max.Add(MaxSearch(new List<int>(hist), 0, min[0])); //a1

			max.Add(MaxSearch(new List<int>(hist), min[0], (min[0] + min[1])/2)); // a12
			max.Add(MaxSearch(new List<int>(hist), (min[0] + min[1]) / 2, min[1])); // a21

			max.Add(MaxSearch(new List<int>(hist), min[1], (min[1] + min[2]) / 2)); // a23
			max.Add(MaxSearch(new List<int>(hist), (min[1] + min[2]) / 2, min[2])); // a32

			max.Add(MaxSearch(new List<int>(hist), min[2], hist.Count)); // a3

			List<int> selectedMax = new List<int>();
			selectedMax.Add(max[max[0] > max[1] ? 0 : 1]); // (a1,a12) 
			selectedMax.Add(max[max[2] > max[3] ? 2 : 3]); // (a21,a23)
			selectedMax.Add(max[max[4] > max[5] ? 4 : 5]); // (a32,a3)

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


			List<bool> dataHideBits = l7.ByteListToBitList(dataHide);


			int pixelOffset = BitConverter.ToInt32(originalImage.GetRange(10, 4).ToArray(), 0);
			var (zeroPoints, peakPoints) = MaxMinSearch(hist);


			int capacity = 0;
			foreach (var peak in peakPoints)
				capacity += hist[peak];

			if (dataHideBits.Count > capacity)
				throw new Exception($"Слишком много данных: доступно {capacity} бит, а передано {dataHideBits.Count} бит.");

			List<byte> markedImage = new List<byte>(originalImage);
			int dataIndex = 0;

			int dataHideIndex = 0;
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

		public static (List<byte> payload, List<byte> ) MPPZOut(
	List<byte> markedImage, List<int> zeroPoints, List<int> peakPoints)
		{
			int pixelOffset = BitConverter.ToInt32(markedImage.GetRange(10, 4).ToArray(), 0);
			List<bool> extractedBits = new List<bool>();
			List<byte> restoredImage = new List<byte>(markedImage);

			// обрабатываем каждую пару пик–ноль
			for (int p = peakPoints.Count-1; p >= 0; p--)
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

				//логика сдвига
				for (int i = pixelOffset; i < markedImage.Count; i++)
				{
					byte pixel = markedImage[i];

					if (zero < peak)
					{
						if ((int)pixel < peak && (int)pixel > zero)
						{
							pixel++;
						}
					}
					else
					{
						if ((int)pixel > peak && (int)pixel < zero)
						{
							pixel--;
						}
					}
					markedImage[i] = pixel;
				}


			}

			List<bool> lengthBits = extractedBits.Take(32).ToList();
			int wmLength = BitConverter.ToInt32(
				l7.BitListToByteList(lengthBits).ToArray(), 0);
			Debug.WriteLine(wmLength);

			List<bool> wmBits = extractedBits.Skip(32)
									   .Take(wmLength * 8)
									   .ToList();



			List<byte> data = l7.BitListToByteList(wmBits);


			return (data, markedImage);
		}

	}
}
