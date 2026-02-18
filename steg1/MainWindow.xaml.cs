
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
		private string defaultKey = "TESTKEY";

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
					List<byte> result = l4.SelectBiteFromBMP(data, i);

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

        private void CalcIntegration_Click(object sender, RoutedEventArgs e)
        {
            if(BMPInPath == "" || TXTInPath == "")
            {
                return;
            }


            List<byte> data = l4.GetBytesFromBMP(BMPInPath);
            List<byte> dataWM = l4.GetBytesFromBMP(TXTInPath);
            
			
			int bits = Convert.ToInt32(((ComboBoxItem)selectBit1.SelectedItem).Content);
            string key = keyField.Text;
			int contSize = WaterMark.getMaxSizeForContainer(data);
			int WMSize = (dataWM.Count) * 8;



			if (contSize >= WMSize + 32)
			{

				sizeFileStatus.Content = $"вм подходит {WMSize + 32} > {contSize} ";
			} else
			{
				sizeFileStatus.Content = $"вм слишком большая {WMSize + 32} > {contSize} " ;

                return;
			}
			if(key == "")
			{
				key = defaultKey;
			}
			List<byte> data2 = WaterMark.SetWMToBMP(data, dataWM, key, bits);

			SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                BMPOutPath = saveFileDialog.FileName;
            }
            l4.SetBytesToBMP(BMPOutPath, data2);
        }

		private void CalcIntegrationAll_Click(object sender, RoutedEventArgs e)
		{
			if (BMPInPath == "" || TXTInPath == "")
			{
				return;
			}


			List<byte> data = l4.GetBytesFromBMP(BMPInPath);
			List<byte> dataWM = l4.GetBytesFromBMP(TXTInPath);


			int bits = Convert.ToInt32(((ComboBoxItem)selectBit1.SelectedItem).Content);
			string key = keyField.Text;
			int contSize = WaterMark.getMaxSizeForContainer(data);
			int WMSize = (dataWM.Count) * 8;



			if (contSize >= WMSize + 32)
			{

				sizeFileStatus.Content = $"вм подходит {WMSize + 32} > {contSize} ";
			}
			else
			{
				sizeFileStatus.Content = $"вм слишком большая {WMSize + 32} > {contSize} ";

				return;
			}
			if (key == "")
			{
				key = defaultKey;
			}

			string SelectedFolderLocal = System.IO.Path.Combine(Path.GetDirectoryName(BMPInPath), $"TXT_{Path.GetFileNameWithoutExtension(BMPInPath)}");
			Directory.CreateDirectory(SelectedFolderLocal);

			for (int i = 0; i < 8; i++)
			{

			
				List<byte> data2 = WaterMark.SetWMToBMP(data, dataWM, key, i);



				string outputPath = System.IO.Path.Combine(
						SelectedFolderLocal,
						$"{Path.GetFileNameWithoutExtension(BMPInPath)}_{i}.bmp"
					);

				l4.SetBytesToBMP(outputPath, data2);

			}
			string outputPathOriginal = System.IO.Path.Combine(
						SelectedFolderLocal,
						$"{Path.GetFileName(BMPInPath)}"
					);

			l4.SetBytesToBMP(outputPathOriginal, data);

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
            int bits = Convert.ToInt32(((ComboBoxItem)selectBit1.SelectedItem).Content);
            List<byte> result = new List<byte>();
			string key = keyField.Text;
			if (key == "")
			{
				key = defaultKey;
			}
			result = WaterMark.GetWMFromBMP(data, key, bits);
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
			Debug.WriteLine($"{data.Count} {result.Count}");
			if (saveFileDialog.ShowDialog() == true)
            {
                TXTOutPath = saveFileDialog.FileName;
            }
			
            l4.SetBytesToBMP(TXTOutPath, result);
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

		
	}
}