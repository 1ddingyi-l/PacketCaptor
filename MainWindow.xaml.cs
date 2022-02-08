﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DotNetPacketCaptor.Utils;
using DotNetPacketCaptor.ViewModels;
using SharpPcap;

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
            if (!(DataContext is MainViewModel mv))
                return;
            mv.ShowPacketDetailWindowCommand.Execute(LvPacketList.SelectedItem);
        }

        private void CheckFormat(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox tb) || !(DataContext is MainViewModel mv))
                return;
            var stringItems = tb.Text.Split('&');
            var filter = mv.Filter; 
            if (stringItems.Any(stringItem => !filter.CheckValidity(stringItem.Trim())))
            {
                filter.CanFilter = false;
                tb.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#ffa39e");
                return;
            }
            filter.CanFilter = true;
            tb.Background = Brushes.White;
        }

        private void StartCapture(object sender, RoutedEventArgs e)
        {
            if (CbMode.SelectedIndex == -1 || (!(DataContext is MainViewModel mv))) 
                return;
            Console.WriteLine(CbMode.SelectedItem);
            switch (CbMode.SelectedItem.ToString())
            {
                case "Promiscuous":
                    var config = new DeviceConfiguration()
                    {
                        Mode = DeviceModes.Promiscuous
                    };
                    mv.Config = config;
                    break;
            }
        }
    }
}