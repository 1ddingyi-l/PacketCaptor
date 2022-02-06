﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using DotNetPacketCaptor.Core;
using DotNetPacketCaptor.Models;
using DotNetPacketCaptor.Windows;
using SharpPcap;

namespace DotNetPacketCaptor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region private fields

        private readonly PacketCaptor _captor;
        private readonly Dictionary<int, Predicate<object>> _keyValuePairs;
        private bool _isCaptorRunning;
        private string _colNumber;
        private string _colRaw;
        private string _colAscii;
        private readonly List<Window> _windows;
        private readonly List<Window> _obsoleteWindows;

        #endregion

        #region events

        public event EventHandler CaptureStart
        {
            add => _captor.CaptureStart += value;
            remove => _captor.CaptureStart -= value;
        }

        public event EventHandler CaptureStop
        {
            add => _captor.CaptureStop += value;
            remove => _captor.CaptureStop -= value;
        }

        #endregion

        #region public properties

        public CaptureDeviceList DeviceList => _captor.Instance;

        public bool IsCaptorRunning
        {
            get => _isCaptorRunning;
            set
            {
                _isCaptorRunning = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView CollectionView { get; }
        public List<string> Modes { get; }

        public string ColNumber
        {
            get => _colNumber;
            private set
            {
                _colNumber = value;
                OnPropertyChanged();
            }
        }

        public string ColRaw
        {
            get => _colRaw;
            private set
            {
                _colRaw = value;
                OnPropertyChanged();
            }
        }

        public string ColAscii
        {
            get => _colAscii;
            private set
            {
                _colAscii = value;
                OnPropertyChanged();
            }
        }

        public PacketFilter Filter { get; }

        public DeviceConfiguration Config { get; set; }
        
        public RelayCommand<int> StartCaptureCommand { get; }
        public RelayCommand StopCaptureCommand { get; }
        public RelayCommand<int> RestartCaptureCommand { get; }
        public RelayCommand<string> PacketFilterCommand { get; }
        public RelayCommand<DotNetRawPacket> ShowPacketDetailCommand { get; }
        public RelayCommand WindowClosingCommand { get; }
        public RelayCommand<DotNetRawPacket> ShowPacketDetailWindowCommand { get; }

        #endregion

        #region constructors

        public MainViewModel()
        {
            _captor = new PacketCaptor();
            _captor.PropertyChanged += CaptorRunningStateChanged;
            _keyValuePairs = new Dictionary<int, Predicate<object>>();
            _windows = new List<Window>();
            _obsoleteWindows = new List<Window>();
            // Get that default view of packet collection
            CollectionView = CollectionViewSource.GetDefaultView(_captor.PacketCollection);
            Filter = new PacketFilter();
            Modes = new List<string> {"Promiscuous"};
            StartCaptureCommand = new RelayCommand<int>(StartCapture);
            StopCaptureCommand = new RelayCommand(StopCapture);
            RestartCaptureCommand = new RelayCommand<int>(RestartCapture);
            PacketFilterCommand = new RelayCommand<string>(PacketFilter);
            ShowPacketDetailCommand = new RelayCommand<DotNetRawPacket>(ShowPacketDetail);
            WindowClosingCommand = new RelayCommand(WindowClosing);
            ShowPacketDetailWindowCommand = new RelayCommand<DotNetRawPacket>(ShowPacketDetailWindow);
        }

        #endregion

        #region private methods

        private void StartCapture(int index)
        {
            if (Config != null)
                _captor.Config = Config;
            _captor.StartCapture(index);
            // Refresh collection view
            CollectionView.Refresh();
            // Waiting for collection view refreshing
            Thread.Sleep(100);
        }

        private void StopCapture()
            => _captor.StopCapture();

        private void RestartCapture(int index)
            => _captor.RestartCapture(index);

        private void PacketFilter(string filterString)
        {
            if (!Filter.CanFilter)
                return;
            /* Remove all conditions in CollectionView.Filter
             * _keyValuePairs is empty at the beginning, so these statements below will not be executed
             */
            foreach (var key in _keyValuePairs.Keys)
                CollectionView.Filter -= _keyValuePairs[key];
            _keyValuePairs.Clear();
            if (filterString == "")
                return;


            /* Parse parameter and add new conditions into CollectionView.Filter */
            var filterItems = filterString.Split('&');
            foreach (var t in filterItems)
            {
                /* Check for format validity */
                var filterItem = t.Trim();
                Filter.GetFilterLogic(filterItem, out var cond);
                _keyValuePairs.Add(cond.GetHashCode(), cond);
                CollectionView.Filter += cond;
            }
        }

        private void ShowPacketDetail(DotNetRawPacket packet)
        {
            if (packet != null)
            {
                var bytes = packet.Bytes;

                /* Block text */
                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();
                var sb3 = new StringBuilder();
                for (var i = 0; i < bytes.Length; i += 16)
                    sb1.AppendLine(Convert.ToString(i, 16).PadLeft(4, '0'));
                for (var i = 1; i <= bytes.Length; i++)
                {
                    byte b = bytes[i - 1];
                    sb2.Append(Convert.ToString(b, 16).PadLeft(2, '0') + " ");
                    if (b >= 33 && b <= 126)
                        sb3.Append(((char) b).ToString() + " ");
                    else
                        sb3.Append("· ");
                    if (i % 8 == 0 && i % 16 != 0 && i != 0)
                    {
                        sb2.Append(" ");
                        sb3.Append(" ");
                    }
                    else if (i % 16 == 0 && i != 0)
                    {
                        sb2.AppendLine();
                        sb3.AppendLine();
                    }
                }

                ColNumber = sb1.ToString();
                ColRaw = sb2.ToString();
                ColAscii = sb3.ToString();
                return;
            }

            ColNumber = "";
            ColRaw = "";
            ColAscii = "";
        }

        private void WindowClosing()
        {
            if (IsCaptorRunning)
                _captor.StopCapture();
            foreach (var window in _obsoleteWindows)
                window.Close();
            foreach (var window in _windows)
                window.Close();
        }

        private void ShowPacketDetailWindow(DotNetRawPacket packet)
        {
            var viewModel = new PacketDetailViewModel()
            {
                ColNumber = ColNumber,
                ColRaw = ColRaw,
                ColAscii = ColAscii
            };
            var window = new PacketDetailWindow(viewModel)
            {
                Title = "[Packet-" + packet.Number + "]",
            };
            window.Show();
            _windows.Add(window);
        }

        private void CaptorRunningStateChanged(object sender, PropertyChangedEventArgs e)
        {
            IsCaptorRunning = _captor.IsRunning;
            if (!IsCaptorRunning)
                return;
            foreach (var window in _windows)
            {
                window.Title += " [no capture file]";
                _obsoleteWindows.Add(window);
            }

            _windows.Clear();
        }

        #endregion
    }
}