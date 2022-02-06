using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DotNetPacketCaptor.Models;
using SharpPcap;

namespace DotNetPacketCaptor.Core
{
    public class PacketCaptor : INotifyPropertyChanged
    {
        #region private fields

        private bool _isRunning;
        private uint _packetId;
        private DateTime _startTime;
        private ILiveDevice _selectedDevice;

        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler CaptureStart;
        public event EventHandler CaptureStop;

        #endregion

        #region public properties

        public CaptureDeviceList Instance { get; }
        public ObservableCollection<DotNetRawPacket> PacketCollection { get; }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public DeviceConfiguration Config { get; set; } = null;

        #endregion

        #region constructor

        public PacketCaptor()
        {
            Instance = CaptureDeviceList.Instance;
            IsRunning = false;
            PacketCollection = new ObservableCollection<DotNetRawPacket>();
        }

        #endregion

        #region public methods

        public void StartCapture(int index)
        {
            if (index >= 0 && index < Instance.Count)
            {
                _selectedDevice = Instance[index];
                StartCapture();
            }
            else
                throw new ArgumentException();
        }

        public void StopCapture()
        {
            if (_selectedDevice != null && IsRunning)
            {
                _selectedDevice.StopCapture();
                _selectedDevice.Close();
                IsRunning = false;
                CaptureStop?.Invoke(this, null);
            }
            else
                throw new InvalidOperationException();
        }

        public void RestartCapture(int index = -1)
        {
            StopCapture();
            if (index != -1)
                StartCapture(index);
            else
                StartCapture();
        }

        #endregion

        #region private methods

        private void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void StartCapture()
        {
            if (_selectedDevice != null)
            {
                // Events register. In case register repeatedly happens
                _selectedDevice.OnPacketArrival -= OnPacketArrival;
                _selectedDevice.OnPacketArrival += OnPacketArrival;
                // Parameter initialization
                _packetId = 1;
                _startTime = DateTime.Now;
                // Clear the useless packets captured last time
                PacketCollection.Clear();
                GC.Collect();
                // Waiting for GC clearing previous collection packets
                Thread.Sleep(100);
                if (Config != null)
                    _selectedDevice.Open(Config);
                else
                    // No configuration
                    _selectedDevice.Open();
                // Non-blocking invoking
                _selectedDevice.StartCapture();
                IsRunning = true;
                CaptureStart?.Invoke(this, null);
            }
            else
                throw new NullReferenceException();
        }

        private void OnPacketArrival(object sender, PacketCapture e)
        {
            var packet = new DotNetRawPacket(e, _packetId++, _startTime);
            /*
             * Dispatcher is not designed to run long blocking operation (such as retrieving data from a WebServer). 
             * You can use the Dispatcher when you want to run an operation that will be executed on the UI thread 
             * (such as updating the value of a progress bar).
             */
            // By using BeginInvoke, we can asynchronously execute
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => { PacketCollection.Add(packet); }));
        }

        #endregion
    }
}