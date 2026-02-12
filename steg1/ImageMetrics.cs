using OpenCvSharp;
using OpenCvSharp.Quality;
using System;
using System.IO;
using System.Text;
using ImreadModes = OpenCvSharp.ImreadModes;

namespace Steg1
{
	public static class ImageMetrics
	{
		public static string CompareImages(string refPath, string testPath)
		{
			using Mat refImg = Cv2.ImRead(refPath, ImreadModes.Grayscale);
			using Mat testImg = Cv2.ImRead(testPath, ImreadModes.Grayscale);

			if (refImg.Empty() || testImg.Empty())
				throw new Exception("Не удалось загрузить одно из изображений.");

			double psnr = Cv2.PSNR(refImg, testImg);

			Scalar ssimScalar = QualitySSIM.Compute(refImg, testImg, null);
			double ssim = ssimScalar.Val0;

			double mse = 0.0;

			for (int y = 0; y < refImg.Rows; y++)
			{
				for (int x = 0; x < refImg.Cols; x++)
				{
					byte p1 = refImg.At<byte>(y, x);
					byte p2 = testImg.At<byte>(y, x);
					double diff = p1 - p2;
					mse += diff * diff;
				}
			}

			mse /= (refImg.Rows * refImg.Cols);

			int[] hist = new int[256];

			for (int y = 0; y < testImg.Rows; y++)
			{
				for (int x = 0; x < testImg.Cols; x++)
				{
					byte val = testImg.At<byte>(y, x);
					hist[val]++;
				}
			}

			string folder = Path.GetDirectoryName(refPath)!;
			string csvPath = Path.Combine(
				folder,
				Path.GetFileNameWithoutExtension(refPath) + ".csv"
			);

			if (!File.Exists(csvPath))
			{
				var header = new StringBuilder("Image");
				for (int i = 0; i < 256; i++)
					header.Append($";{i}");
				header.AppendLine();

				File.WriteAllText(csvPath, header.ToString());
			}

			var line = new StringBuilder();
			line.Append(Path.GetFileNameWithoutExtension(testPath));

			for (int i = 0; i < 256; i++)
				line.Append($";{hist[i]}");

			line.AppendLine();

			File.AppendAllText(csvPath, line.ToString());

			string name = Path.GetFileNameWithoutExtension(testPath);

			// Ровно 10 символов
			name = name.Length > 10
				? name.Substring(0, 10)
				: name.PadRight(10);

			// Ровно 9 символов на число
			return $"{name} - " + $"MSE= {mse,9:F4} " + $"PSNR= {psnr,6:F2} dB " +  $"SSIM= {ssim,6:F4}";


		}
	}
}
