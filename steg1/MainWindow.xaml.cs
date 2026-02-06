using GPILabs;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace steg1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();
        }

        public void part1()
        {
            List<byte> data = l4.GetBytesFromBMP("C:\\Users\\andreyeyeye\\source\\repos\\ptzi1\\ptzi1\\1.bmp");
            List<byte> data2 = l4.SelectBiteFromBMP(data, 7);
            l4.SetBytesToBMP("C:\\Users\\andreyeyeye\\source\\repos\\ptzi1\\ptzi1\\2.bmp", data2);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            part1();
        }
    }
}