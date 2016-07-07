using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClientMessenger
{
    /// <summary>
    /// Логика взаимодействия для Wellcome.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Gifes");
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Sounds");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void textName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Start();
            }
        }

        void Start()
        {
            if (textName.Text == "")
            {
                return;
            }
            this.Visibility = Visibility.Collapsed;
            Chat chat = new Chat(textName.Text);
            chat.Show();
            this.Close();
        }
    }
}
