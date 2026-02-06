using GPILabs;
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
namespace steg1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string SelectedFolder = string.Empty;
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

				// создаём подпапку с именем оригинального файла
				string outputDir = System.IO.Path.Combine(
					SelectedFolder,
					fileNameWithoutExt
				);

				Directory.CreateDirectory(outputDir);

				// читаем исходный BMP один раз
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

				// важно: имя-заглушка
				FileName = "Выбор папки"
			};

			if (dialog.ShowDialog() == true)
			{
				SelectedFolder = Path.GetDirectoryName(dialog.FileName);

			
			}
		}
	}
}