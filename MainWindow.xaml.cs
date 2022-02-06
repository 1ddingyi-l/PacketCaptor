using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DotNetPacketCaptor.ViewModels;

namespace DotNetPacketCaptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(DataContext is MainViewModel mv) || !(sender is ListViewItem item))
                return;
            mv.ShowPacketDetailWindowCommand.Execute(LvPacketList.SelectedItem);
        }

        private void OnGetFilterInput(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox tb) || !(DataContext is MainViewModel mv))
                return;
            var stringItems = tb.Text.Split('&');
            var filter = mv.Filter; 
            if (stringItems.Any(stringItem => !filter.CheckValidity(stringItem)))
            {
                filter.CanFilter = false;
                tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#ffa39e");
                return;
            }
            filter.CanFilter = true;
            tb.Background = Brushes.White;
        }
    }
}