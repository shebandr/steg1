
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

		private string defaultKey = "DEFAULT KEY";

		private int xiBlockSize = 32;
		public MainWindow()
        {

            InitializeComponent();

		

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

    
        private void getBMP_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();


            if (openFileDialog.ShowDialog() == true)
            {
                BMPInPath = openFileDialog.FileName;
            }
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

		

		

		private void CalcIntegration_Click(object sender, RoutedEventArgs e)
		{
			string key = keyField.Text;
			if (key == "")
			{
				key = defaultKey;
			}
			int origUsersCout = Int32.Parse(origUsersCountField.Text);



			Tardos.BuildCode(10, 2, 0.1);

			// 2. Получение инфы
			var info = Tardos.GetInfo();
			Debug.WriteLine($"Users: {info.users}, Length: {info.codeLength}, c: {info.collisionSize}");

			// 3. Симуляция атаки
			int[] attackers = { 2, 5, 7, 9 };

			var y = Tardos.Collide(attackers);

			// 4. Детект
			var result = Tardos.Detect(y);

			// 5. Вывод топ подозреваемых
			Debug.WriteLine("Top suspects:");
			for (int i = 0; i < 10; i++)
			{
				Debug.WriteLine($"User {result[i].user}, score = {result[i].score:F4}");
			}
		}


		private void CalcExtraction_Click(object sender, RoutedEventArgs e)
		{

		}


	}
}