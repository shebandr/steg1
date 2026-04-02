
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


			//Tardos.BuildCode(10, 2, 0.1);

			//// 2. Получение инфы
			//var info = Tardos.GetInfo();
			//Debug.WriteLine($"Users: {info.users}, Length: {info.codeLength}, c: {info.collisionSize}");

			//// 3. Симуляция атаки
			//int[] attackers = { 2, 5, 7, 9 };

			//var y = Tardos.Collide(attackers);

			//// 4. Детект
			//var result = Tardos.Detect(y);

			//// 5. Вывод топ подозреваемых
			//Debug.WriteLine("Top suspects:");
			//for (int i = 0; i < 10; i++)
			//{
			//	Debug.WriteLine($"User {result[i].user}, score = {result[i].score:F4}");
			//}
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
			string outMetrics = string.Empty; 
			string key = keyField.Text;
			if (key == "")
			{
				key = defaultKey;
			}
			int origUsersCout = Int32.Parse(origUsersCountField.Text);
			int origAttackersCout = Int32.Parse(origAttackersCountField.Text);
			List<byte> data = l4.GetBytesFromBMP(BMPInPath);

			Tardos.BuildCode(origUsersCout, origAttackersCout, 0.1);

			var info = Tardos.GetInfo();
			outMetrics += $"Пользователей: {info.users}, Длина в битах: {info.codeLength * 8}, c: {info.collisionSize} \n";

			int[] attackers = { 2, 5, 7, 9 };
			var y = Tardos.Collide(attackers);

			char[][] originalCodes = Tardos.GetCodeTable();
			char[] pirateCode = y;

			string selectedFolder = Path.Combine(Path.GetDirectoryName(BMPInPath), "Tardos");
			Directory.CreateDirectory(selectedFolder);

			string fileName = Path.GetFileNameWithoutExtension(BMPInPath);



			List<byte> pirateBytes = pirateCode
				.Select(c => (byte)(c - '0'))
				.ToList();

			List<byte> outPirate = WaterMark.SetWMToBMP(data, pirateBytes, key, 0);

			string piratePath = Path.Combine(selectedFolder, $"{fileName}_pirate.bmp");

			l4.SetBytesToBMP(piratePath, outPirate);


			for (int q = 0; q < originalCodes.Length; q++)
			{
				List<byte> temp = originalCodes[q]
					.Select(c => (byte)(c - '0'))
					.ToList();

				List<byte> outBytes = WaterMark.SetWMToBMP(data, temp, key, 0);

				string outPath = Path.Combine(selectedFolder, $"{fileName}_{q}.bmp");

				l4.SetBytesToBMP(outPath, outBytes);
			}


			var result = Tardos.Detect(y);

			outMetrics += "Топ подозреваемых: \n";
			for (int i = 0; i < 10; i++)
			{
				outMetrics += $"Пользователь {result[i].user}, очки = {result[i].score:F4} \n";
			}
			metricsOutput.Text = outMetrics;
		}


		private void CalcExtraction_Click(object sender, RoutedEventArgs e)
		{

		}

		private void calcTardosAll_Click(object sender, RoutedEventArgs e)
		{
			int N = 50;                
			double eps = 0.1;
			int experiments = 10;

			int[] cValues = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

			string folder = Path.Combine(Path.GetDirectoryName(BMPInPath), "Tardos");
			Directory.CreateDirectory(folder);

			string csvPath = Path.Combine(folder, "tardos_results.csv");

			var lines = new List<string>();

			lines.Add("c\tc_real\tm\tZ\tDetectionProbability");

			Random rnd = new Random();

			foreach (int c in cValues)
			{
				Tardos.BuildCode(N, c, eps);

				var info = Tardos.GetInfo();
				int m = info.codeLength;

				double Z = Math.Sqrt(m * Math.Log(N / eps));

				for (int c_real = 1; c_real <= 20; c_real++)
				{
					int success = 0;

					for (int iter = 0; iter < experiments; iter++)
					{
						int[] attackers = Enumerable.Range(0, N)
							.OrderBy(x => rnd.Next())
							.Take(c_real)
							.ToArray();

						var y = Tardos.Collide(attackers);
						var result = Tardos.Detect(y); 

						bool detected = false;

						foreach (var r in result)
						{
							if (r.score > Z && attackers.Contains(r.user))
							{
								detected = true;
								break;
							}
						}

						if (detected)
							success++;
					}

					double probability = (double)success / experiments;

					lines.Add($"{c}\t{c_real}\t{m}\t{Z:F4}\t{probability:F4}");
				}
			}

			File.WriteAllLines(csvPath, lines);

			Debug.WriteLine($"CSV saved to: {csvPath}");


			//string mGraphPath = Path.Combine(folder, "tardos_m_vs_c.csv");

			//var mLines = new List<string>();

			//mLines.Add("c\tm");

			//foreach (int cVal in cValues)
			//{
			//	double k_local = Math.Ceiling(Math.Log(1 / eps));
			//	int m_local = (int)(100 * cVal * cVal * k_local);

			//	mLines.Add($"{cVal}\t{m_local}");
			//}

			//File.WriteAllLines(mGraphPath, mLines);

			//Debug.WriteLine($"m(c) CSV saved to: {mGraphPath}");


			//string zGraphPath = Path.Combine(folder, "tardos_Z_vs_c.csv");

			//var zLines = new List<string>();

			//zLines.Add("c\tZ");

			//foreach (int cVal in cValues)
			//{
			//	double k_local = Math.Ceiling(Math.Log(1 / eps));
			//	int m_local = (int)(100 * cVal * cVal * k_local);

			//	double Z_local = Math.Sqrt(m_local * Math.Log(N / eps));

			//	zLines.Add($"{cVal}\t{Z_local:F4}");
			//}

			//File.WriteAllLines(zGraphPath, zLines);

			//Debug.WriteLine($"Z(c) CSV saved to: {zGraphPath}");

		}
	}
}