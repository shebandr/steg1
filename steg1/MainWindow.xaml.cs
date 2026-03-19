
using Microsoft.Win32;
using Steg1;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
namespace steg1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string SelectedFolder = string.Empty;
        private string BMPInPath = string.Empty;
        private string TXTInPath = string.Empty;
        private string BMPOutPath = string.Empty;
        private string TXTOutPath = string.Empty;
		private string testBMPFolder = string.Empty;
		private string refBMPPath = string.Empty;
		private List<int> MPPZZero = new List<int>();
		private List<int> MPPZPeak = new List<int>();
		private int xiBlockSize = 32;
		public MainWindow()
        {

            InitializeComponent();
        }

		public void part1()
		{
			
			if (string.IsNullOrEmpty(SelectedFolder))
				return;

			string[] bmpFiles = Directory.GetFiles(SelectedFolder, "*.bmp");
			SelectedFolder = System.IO.Path.Combine(SelectedFolder, "output");
			Directory.CreateDirectory(SelectedFolder);
			foreach (string bmpPath in bmpFiles)
			{
				string fileNameWithoutExt =
					System.IO.Path.GetFileNameWithoutExtension(bmpPath);

				string outputDir = System.IO.Path.Combine(
					SelectedFolder,
					fileNameWithoutExt
				);

				Directory.CreateDirectory(outputDir);

				List<byte> data = l4.GetBytesFromBMP(bmpPath);

				for (int i = 0; i < 8; i++)
				{
					List<byte> result = l4.SelectByteFromBMP(data, i);

					string outputPath = System.IO.Path.Combine(
						outputDir,
						$"{fileNameWithoutExt}_{i}.bmp"
					);

					l4.SetBytesToBMP(outputPath, result);
				}

				string outputPath2 = System.IO.Path.Combine(
					outputDir,
					$"{System.IO.Path.GetFileName(bmpPath)}"
				);

				l4.SetBytesToBMP(outputPath2, data);
			}
		}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            part1();
        }

		private void SelectFolder_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "Выберите папку",
				CheckFileExists = false,
				CheckPathExists = true,

				FileName = "Выбор папки"
			};

			if (dialog.ShowDialog() == true)
			{
				SelectedFolder = Path.GetDirectoryName(dialog.FileName);

			
			}
		}

        private void getTXT_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();


            if (openFileDialog.ShowDialog() == true)
            {
                TXTInPath = openFileDialog.FileName;
            }
            //Console.WriteLine(TXTInPath);
        }

        private void getBMP_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();


            if (openFileDialog.ShowDialog() == true)
            {
                BMPInPath = openFileDialog.FileName;
            }
        }

        private void CalcX2_Click(object sender, RoutedEventArgs e)
        {
			string BMPInPath2 = "";

			OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                BMPInPath = openFileDialog.FileName;
            }

			openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				BMPInPath2 = openFileDialog.FileName;
			}

			List<byte> data = l4.GetBytesFromBMP(BMPInPath);
			List<byte> data2 = l4.GetBytesFromBMP(BMPInPath2);

			List<List<double>> chi = Steganalysis.FullChiCalc(data, xiBlockSize);

			List<List<double>> chi2 = Steganalysis.FullChiCalc(data2, xiBlockSize);
			double sum = 0;
			string output = string.Empty;
			for (int i = 0; i < chi.Count; i++) 
			{
				for(int q = 0; q< chi[i].Count; q++)
				{
					output += (chi[i][q] - chi2[i][q]).ToString("F2");
					output += " ";
					sum += chi[i][q] - chi2[i][q];
				}
				output += "\n";
			}
			Debug.WriteLine(sum);
            metricsOutput.Text = output;


        }

		private void calcRS_Click(object sender, RoutedEventArgs e)
		{

			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				BMPInPath = openFileDialog.FileName;
			}

			

			List<byte> data = l4.GetBytesFromBMP(BMPInPath);

			Steganalysis.RunRSAnalysis(data);

			string output = string.Empty;
		
			metricsOutput.Text = output;

		}

		private void calcAUMP_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				BMPInPath = openFileDialog.FileName;
			}



			List<byte> data = l4.GetBytesFromBMP(BMPInPath);
			List<List<byte>> pixels = Steganalysis.SelectPixelsFromBMP(data);

			byte[][] result = pixels.Select(row => row.ToArray()).ToArray();


			double beta_aump = AUMP.Aump(result, m: 16, d: 2);
			Debug.WriteLine($"AUMP beta: {beta_aump}");


			
		}


		private void calsXi2Mass_Click(object sender, RoutedEventArgs e)
		{



            Dictionary<string, List<List<double>>> allResults = new Dictionary<string, List<List<double>>>();
            string[] bmpFiles = Directory.GetFiles(testBMPFolder, "*.bmp");
            foreach (var bmpFile in bmpFiles)
            {
                try
                {
                    List<byte> data = l4.GetBytesFromBMP(bmpFile);

                    List<List<double>> chi = Steganalysis.FullChiCalc(data, xiBlockSize);

                    allResults.Add(Path.GetFileNameWithoutExtension(bmpFile), chi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла: {bmpFile}");
                    Console.WriteLine($"Сообщение: {ex.Message}");
                }
            }
            StringBuilder sb = new StringBuilder();

            foreach (var pair in allResults)
            {
				sb.Append(pair.Key);
				sb.AppendLine();
				foreach(var pair2 in pair.Value)
				{
                    string temp = "";
					foreach(var pair3 in pair2)
					{
						string t = pair3.ToString("F2");

                        temp += $"{t}\t";
					}


					sb.Append(temp);
                }

				sb.AppendLine();
            }

            string resultString = sb.ToString();
            string outputPath = Path.Combine(testBMPFolder, "resultsXi.tsv");
            File.WriteAllText(outputPath, resultString, Encoding.UTF8);
			metricsOutput.Text = resultString;
        }

		private void calcRSMass_Click(object sender, RoutedEventArgs e)
		{
			Dictionary<string, double> allResults = new Dictionary<string, double>();
			string[] bmpFiles = Directory.GetFiles(testBMPFolder, "*.bmp");
			foreach (var bmpFile in bmpFiles)
			{
				try
				{
					List<byte> data = l4.GetBytesFromBMP(bmpFile);
					double rsResult = Steganalysis.RunRSAnalysis(data);

					allResults.Add(Path.GetFileNameWithoutExtension(bmpFile), rsResult);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка при обработке файла: {bmpFile}");
					Console.WriteLine($"Сообщение: {ex.Message}");
				}
			}
			StringBuilder sb = new StringBuilder();

			foreach (var pair in allResults)
			{
				string value = pair.Value.ToString(new CultureInfo("ru-RU"));

				sb.Append(pair.Key)
				  .Append('\t')
				  .Append(value)
				  .AppendLine();
			}

			string resultString = sb.ToString(); 
			string outputPath = Path.Combine(testBMPFolder, "resultsRS.tsv");
			File.WriteAllText(outputPath, resultString, Encoding.UTF8);
            metricsOutput.Text = resultString;
        }

		private void calcAUMPMass_Click(object sender, RoutedEventArgs e)
		{
			Dictionary<string, double> allResults = new Dictionary<string, double>();
			string[] bmpFiles = Directory.GetFiles(testBMPFolder, "*.bmp");
			foreach (var bmpFile in bmpFiles)
			{
				try
				{
					List<byte> data = l4.GetBytesFromBMP(bmpFile);
					List<List<byte>> pixels = Steganalysis.SelectPixelsFromBMP(data);

					byte[][] result = pixels.Select(row => row.ToArray()).ToArray();


					double beta_aump = AUMP.Aump(result, m: 16, d: 2);
					allResults.Add(Path.GetFileNameWithoutExtension(bmpFile), beta_aump);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка при обработке файла: {bmpFile}");
					Console.WriteLine($"Сообщение: {ex.Message}");
				}
			}

			StringBuilder sb = new StringBuilder();
			foreach (var pair in allResults)
			{
				string value = pair.Value.ToString(new CultureInfo("ru-RU"));

				sb.Append(pair.Key)
				  .Append('\t')
				  .Append(value)
				  .AppendLine();
			}

			string resultString = sb.ToString();
			string outputPath = Path.Combine(testBMPFolder, "resultsAUMP.tsv");
			File.WriteAllText(outputPath, resultString, Encoding.UTF8);
            metricsOutput.Text = resultString;
        }

		
		



		private void getBMP2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();


            if (openFileDialog.ShowDialog() == true)
            {
                BMPInPath = openFileDialog.FileName;
            }
        }


		
	

		private void getFolder_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "Выберите папку",
				CheckFileExists = false,
				CheckPathExists = true,

				FileName = "Выбор папки"
			};

			if (dialog.ShowDialog() == true)
			{
				testBMPFolder = Path.GetDirectoryName(dialog.FileName);


			}
		}

		private void getOriginal_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();


			if (openFileDialog.ShowDialog() == true)
			{
				refBMPPath = openFileDialog.FileName;
			}
		}

		private void calcMetrics_Click(object sender, RoutedEventArgs e)
		{
			metricsOutput.Clear();
			string outputMetrics = string.Empty;

			string[] bmpFiles = Directory.GetFiles(testBMPFolder, "*.bmp");
			foreach (var bmpFile in bmpFiles) 
			{
				outputMetrics += ImageMetrics.CompareImages(refBMPPath, bmpFile);
				outputMetrics += "\n";

			}



			metricsOutput.Text = outputMetrics;
		}

		private void getBMPFolder_Click(object sender, RoutedEventArgs e)
		{

		}

        private void calcMetrics2_Click(object sender, RoutedEventArgs e)
        {
            string[] bmpFiles = Directory.GetFiles(testBMPFolder, "*.bmp");
            string folder = Path.GetDirectoryName(testBMPFolder)!;
            string csvPath = Path.Combine(
                folder,
                Path.GetFileNameWithoutExtension(testBMPFolder) + ".csv"
            );

            if (!File.Exists(csvPath))
            {
                var header = new StringBuilder("Image");
                for (int i = 1; i < 8; i++) 
				{
					header.Append($";{i} MAX");
					header.Append($";{i} FREE");
				}
                header.AppendLine();

                File.WriteAllText(csvPath, header.ToString());
            }

            foreach (var bmpFile in bmpFiles)
            {
                var line = new StringBuilder();
                List<int> hist = ImageMetrics.BuildHistogram(bmpFile);

                line.Append(Path.GetFileNameWithoutExtension(bmpFile));

                for (int i = 1; i < 8; i++)
                {
                    (int maxCapacity, int usedCapacity) = HistogramShifting.CalcSpace(hist, i);

                    line.Append($";{maxCapacity}");
                    line.Append($";{usedCapacity}");
                }

                line.AppendLine();
                File.AppendAllText(csvPath, line.ToString());
            }
		}
    }
}