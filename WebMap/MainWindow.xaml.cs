using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WebMap {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            // handle the events in the MainViewModel class
            MainViewModel vm = (MainViewModel)this.DataContext;
            Closing += vm.MainWindow_Closing;
            Button_Connect.Click += vm.Button_Connect_Click;
            Button_EnableWebServer.Click += vm.Button_EnableWebServer_Click;
        }
    }
}
