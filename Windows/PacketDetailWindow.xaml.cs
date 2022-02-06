using System.Windows;
using DotNetPacketCaptor.ViewModels;

namespace DotNetPacketCaptor.Windows
{
    public partial class PacketDetailWindow : Window
    {   
        protected PacketDetailWindow()
        {
            InitializeComponent();
        }

        public PacketDetailWindow(PacketDetailViewModel viewModel) : this()
            => DataContext = viewModel;
    }
}