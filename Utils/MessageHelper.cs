using System.Windows;
using System.Windows.Forms.VisualStyles;

namespace DotNetPacketCaptor.Utils
{
    public static class MessageHelper
    {
        public static void ShowWarning(string content, string title = "Warning")
            => MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public static void ShowError(string content, string title = "Error")
            => MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public static MessageBoxResult ShowCustomizedWarning(string content, MessageBoxButton button, string title = "Warning")
            => MessageBox.Show(content, title, button, MessageBoxImage.Warning);

        public static void ShowPopup(string content, string title = "Info")
            => MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);
        
        public static void ShowTest(string content, string title = "Test")
            => MessageBox.Show(content, title);
    }
}