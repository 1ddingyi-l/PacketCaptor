using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using DotNetPacketCaptor.Core;
using DotNetPacketCaptor.Ext;
using DotNetPacketCaptor.Models;
using DotNetPacketCaptor.Utils;
using DotNetPacketCaptor.Windows;
using SharpPcap;

namespace DotNetPacketCaptor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region private fields

        private readonly PacketCaptor _captor;
        private readonly List<Predicate<object>> _predicates;
        private bool _isCaptorRunning;
        private bool _hasPacketUnsaved;
        private string _colNumber;
        private string _colRaw;
        private string _colAscii;
        private string _windowTitle;
        private readonly List<Window> _windows;
        private readonly List<Window> _obsoleteWindows;
        private ICollectionView _collectionView;

        #endregion

        #region private properties

        private bool HasPacketsUnsaved
        {
            get => _hasPacketUnsaved;
            set
            {
                _hasPacketUnsaved = value;
                if (_hasPacketUnsaved)
                    Title += "*";
                else if (Title[Title.Length - 1] == '*')
                    Title = Title.Remove(Title.Length - 1, 1);
            }
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

        public ICollectionView CollectionView
        {
            get => _collectionView;
            private set
            {
                _collectionView = value;
                OnPropertyChanged();
            }
        }

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

        public string Title
        {
            get => _windowTitle;
            private set
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public PacketFilter Filter { get; }

        public DeviceConfiguration Config { get; set; }

        #endregion

        #region commands

        public RelayCommand<int> StartCaptureCommand { get; }
        public RelayCommand StopCaptureCommand { get; }
        public RelayCommand<int> RestartCaptureCommand { get; }
        public RelayCommand<string> PacketFilterCommand { get; }
        public RelayCommand<DotNetRawPacket> ShowPacketDetailCommand { get; }
        public RelayCommand WindowClosingCommand { get; }
        public RelayCommand<DotNetRawPacket> ShowPacketDetailWindowCommand { get; }
        public RelayCommand GetPacketsFromFileCommand { get; }
        public RelayCommand SavePacketsToFileCommand { get; }

        #endregion

        #region constructors

        public MainViewModel()
        {
            // Initialize private fields
            _captor = new PacketCaptor();
            _captor.PropertyChanged += CaptorRunningStateChanged;
            _predicates = new List<Predicate<object>>();
            _windows = new List<Window>();
            _obsoleteWindows = new List<Window>();
            _isCaptorRunning = false;
            _hasPacketUnsaved = false;


            // Initialize public properties
            // Get that default view of packet collection
            CollectionView = CollectionViewSource.GetDefaultView(_captor.PacketCollection);
            Filter = new PacketFilter();
            Modes = new List<string> {"Promiscuous"};
            Title = "DotNetPacketCaptor";


            // Initialize commands for binding
            StartCaptureCommand = new RelayCommand<int>(StartCapture);
            StopCaptureCommand = new RelayCommand(StopCapture);
            RestartCaptureCommand = new RelayCommand<int>(RestartCapture);
            PacketFilterCommand = new RelayCommand<string>(PacketFilter);
            ShowPacketDetailCommand = new RelayCommand<DotNetRawPacket>(ShowPacketDetail);
            WindowClosingCommand = new RelayCommand(WindowClosing);
            ShowPacketDetailWindowCommand = new RelayCommand<DotNetRawPacket>(ShowPacketDetailWindow);
            GetPacketsFromFileCommand = new RelayCommand(GetPacketsFromFile);
            SavePacketsToFileCommand = new RelayCommand(SavePacketsToFile);
        }

        #endregion

        #region command methods

        /* For binding */

        private void StartCapture(int index)
        {
            CheckIfHasPacketsUnsaved();
            // Check index validity
            if (index < 0)
            {
                MessageHelper.ShowError("Please choose a network card before your starting capture");
                return;
            }

            // Refresh collection view
            CollectionView.Refresh();
            // Waiting for collection view refreshing
            Thread.Sleep(100);
            // Check for config
            if (Config != null)
                _captor.Config = Config;
            // Start new capture
            _captor.StartCapture(index);
            // Changed view(unnecessary)
            CollectionView = CollectionViewSource.GetDefaultView(_captor.PacketCollection);
        }

        private void StopCapture()
        {
            _captor.StopCapture();
            if (!CollectionView.IsEmpty)
                HasPacketsUnsaved = true;
        }

        private void RestartCapture(int index)
        {
            if (!CollectionView.IsEmpty)
                HasPacketsUnsaved = true;
            CheckIfHasPacketsUnsaved();
            _captor.RestartCapture(index);
        }

        private void PacketFilter(string filterString)
        {
            if (!Filter.CanFilter)
                return;
            /* Remove all conditions in CollectionView.Filter
             * _keyValuePairs is empty at the beginning, so these statements below will not be executed
             */
            foreach (var item in _predicates)
                CollectionView.Filter -= item;
            _predicates.Clear();
            if (filterString == "")
                return;


            /* Parse parameter and add new conditions into CollectionView.Filter */
            var filterItems = filterString.Split('&');
            foreach (var t in filterItems)
            {
                // Check for format validity
                var filterItem = t.Trim();
                if (filterItem == "")
                    continue;
                Filter.GetFilterLogic(filterItem, out var cond);
                _predicates.Add(cond);
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
                    var b = bytes[i - 1];
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
                StopCaptureCommand.Execute(null);
            CheckIfHasPacketsUnsaved();
            foreach (var window in _obsoleteWindows)
                window.Close();
            foreach (var window in _windows)
                window.Close();
        }

        private void ShowPacketDetailWindow(DotNetRawPacket packet)
        {
            var viewModel = new PacketDetailViewModel(ColNumber, ColRaw, ColAscii);
            var window = new PacketDetailWindow(viewModel)
            {
                Title = "[Packet-" + packet.Number + "]",
            };
            window.Show();
            _windows.Add(window);
        }

        private void GetPacketsFromFile()
        {
            CheckIfHasPacketsUnsaved();
            var fileWindow = new OpenFileDialog()
            {
                Title = @"Select file path",
                Filter = @"All files (*.*)|*.*"
            };
            if (fileWindow.ShowDialog() != DialogResult.OK)
                return;
            var filePath = fileWindow.FileName;
            using (var fs = File.Open(filePath, FileMode.Open))
            {
                var bf = new BinaryFormatter();
                try
                {
                    if (!(bf.Deserialize(fs) is List<PacketInfo> collection))
                        return;
                    var ls = collection.Select(packetInfo => new DotNetRawPacket(packetInfo)).ToList();
                    CollectionView = CollectionViewSource.GetDefaultView(ls);
                }
                catch (ArgumentNullException e)
                {
                    MessageHelper.ShowError(e.Message);
                }
            }
        }

        private void SavePacketsToFile()
        {
            var fileWindow = new SaveFileDialog()
            {
                Title = @"Select file path",
                Filter = @"All files (*.*)|*.*"
            };
            if (fileWindow.ShowDialog() != DialogResult.OK)
                return;
            var filePath = fileWindow.FileName;
            var collection = CollectionView.GetPacketInfo();
            using (var fs = File.Create(filePath))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(fs, collection);
            }

            HasPacketsUnsaved = false;
            MessageHelper.ShowPopup("Done!");
        }

        #endregion

        #region event handler

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

        #region private methods

        private void CheckIfHasPacketsUnsaved()
        {
            if (!HasPacketsUnsaved)
                return;
            var result = MessageHelper.ShowCustomizedWarning("Would you like to save captured packets?",
                MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            else if (result == MessageBoxResult.Yes)
                SavePacketsToFileCommand.Execute(null);
            HasPacketsUnsaved = false;
        }

        #endregion
    }
}