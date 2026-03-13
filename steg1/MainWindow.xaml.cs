
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Diagnostics;
using Steg1;
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
            OpenFileDialog openFileDialog = new OpenFileDialog();


            if (openFileDialog.ShowDialog() == true)
            {
                BMPInPath = openFileDialog.FileName;
            }

            List<byte> data = l4.GetBytesFromBMP(BMPInPath);

			List<List<double>> chi = Steganalysis.FullChiCalc(data, 16);
			string output = string.Empty;
			for (int i = 0; i < chi.Count; i++) 
			{
				for(int q = 0; q< chi[i].Count; q++)
				{
					output += chi[i][q].ToString("F2");
					output += " ";
				}
				output += "\n";
			}
            metricsOutput.Text = output;


        }

		private void CalcIntegrationAll_Click(object sender, RoutedEventArgs e)
		{
			

		}
		private void getBMP2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();


            if (openFileDialog.ShowDialog() == true)
            {
                BMPInPath = openFileDialog.FileName;
            }
        }


		private void CalcExtraction_Click(object sender, RoutedEventArgs e)
        {
            List<byte> data = l4.GetBytesFromBMP(BMPInPath);
			List<byte> result = new List<byte>();
			List<byte> original = new List<byte>();
			
			(result, original) = HistogramShifting.MPPZOut(data, MPPZZero, MPPZPeak);
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
			if (saveFileDialog.ShowDialog() == true)
            {
                TXTOutPath = saveFileDialog.FileName;
            }
			l4.SetBytesToBMP(TXTOutPath, result);

			saveFileDialog = new SaveFileDialog();
			if (saveFileDialog.ShowDialog() == true)
			{
				TXTOutPath = saveFileDialog.FileName;
			}
			l4.SetBytesToBMP(TXTOutPath, original);
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
                for (int i = 1; i < 8; i++) // Начинаем с 1, так как 0 бит обычно не используется
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

                // Добавляем имя файла один раз в начале строки
                line.Append(Path.GetFileNameWithoutExtension(bmpFile));

                // Затем для каждого бита добавляем значения
                for (int i = 1; i < 8; i++)
                {
                    (int maxCapacity, int usedCapacity) = HistogramShifting.CalcSpace(hist, i);

                    // Рассчитываем доступное место (можно использовать разные метрики)
                    // Или можно использовать процент: (double)availableSpace / maxCapacity * 100
                    line.Append($";{maxCapacity}");
                    line.Append($";{usedCapacity}");
                }

                line.AppendLine();
                File.AppendAllText(csvPath, line.ToString());
            }
        




    }

        private void CalcIntegration_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}