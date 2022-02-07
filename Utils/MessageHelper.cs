using System.Windows;

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

        public static void ShowTest(string content, string title = "Test")
            => MessageBox.Show(content, title);
    }
}