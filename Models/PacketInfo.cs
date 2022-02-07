using System;
using PacketDotNet;

namespace DotNetPacketCaptor.Models
{
    [Serializable]
    public struct PacketInfo
    {
        public ulong PacketId { get; }
        public double ArrivalTime { get; }
        public uint LengthOnLine { get; }
        public uint ActualLength { get; }
        public LinkLayers LinkType { get; }
        public byte[] Raw { get; }

        public PacketInfo(DotNetRawPacket rawPacket)
        {
            PacketId = rawPacket.Number;
            ArrivalTime = rawPacket.ArrivalTime;
            Raw = rawPacket.Bytes;
            LengthOnLine = rawPacket.PacketBytesOnLine;
            ActualLength = rawPacket.PacketBytesCaptured;
            LinkType = rawPacket.LinkLayerType;
        }
    }
}