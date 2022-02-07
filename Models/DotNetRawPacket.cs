using System;
using System.Runtime.Serialization;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace DotNetPacketCaptor.Models
{
    public class DotNetRawPacket
    {
        public byte[] Bytes { get; }

        public byte Layers { get; } = 1;

        public ulong Number { get; }

        public double ArrivalTime { get; }
        
        public uint PacketBytesOnLine { get; }

        public uint PacketBytesCaptured { get; }

        public LinkLayers LinkLayerType { get; }

        public Packet LinkLayerPacket { get; }

        public string SourcePhysicalAddress { get; }

        public string DestinationPhysicalAddress { get; }

        public IPv4Packet NetworkIpV4Packet { get; }

        public IPv6Packet NetworkIpV6Packet { get; }

        public ArpPacket NetworkArpPacket { get; }

        public TcpPacket NetworkTcpPacket { get; }

        public UdpPacket NetworkUdpPacket { get; }

        public IcmpV4Packet NetworkIcmpV4Packet { get; }

        public IcmpV6Packet NetworkIcmpV6Packet { get; }

        public IgmpV2Packet NetworkIgmpV2Packet { get; }

        public string Protocol { get; }

        public string Source { get; }

        public string Destination { get; }

        public string Info { get; }

        public DotNetRawPacket(PacketInfo packetInfo)
        {
            Number = packetInfo.PacketId;
            Bytes = packetInfo.Raw;
            ArrivalTime = packetInfo.ArrivalTime;
            PacketBytesOnLine = packetInfo.LengthOnLine;
            PacketBytesCaptured = packetInfo.ActualLength;
            LinkLayerType = packetInfo.LinkType;
            LinkLayerPacket = Packet.ParsePacket(packetInfo.LinkType, packetInfo.Raw);
            
            
            
            /* Get layers number(save for data) */
            var linkLayerPacket = LinkLayerPacket;
            while (linkLayerPacket.HasPayloadPacket || linkLayerPacket.HasPayloadData)
            {
                if (linkLayerPacket.HasPayloadPacket)  // Payload packet
                {
                    linkLayerPacket = linkLayerPacket.PayloadPacket;
                    Layers++;
                }
                else  // Payload data
                    break;
            }

            var type = LinkLayerPacket.GetType();
            if (type == typeof(EthernetPacket))
            {
                var ethernetPacket = (EthernetPacket)LinkLayerPacket;
                SourcePhysicalAddress = ethernetPacket.SourceHardwareAddress.ToString();
                DestinationPhysicalAddress = ethernetPacket.DestinationHardwareAddress.ToString();
                if (Layers == 1)  // Only link layer, broadcast frame
                {
                    Source = ethernetPacket.SourceHardwareAddress.ToString();
                    Destination = "broadcast";
                    Protocol = ethernetPacket.Type.ToString();
                    Info = "Ethernet II";
                    return;
                }

                if (Layers == 2)  // Only network layer, such as Arp
                {
                    if (ethernetPacket.Type == EthernetType.Arp)
                    {
                        NetworkArpPacket = ethernetPacket.PayloadPacket as ArpPacket;
                        if (NetworkArpPacket == null)
                            throw new NullReferenceException();
                        Source = NetworkArpPacket.SenderProtocolAddress.ToString();
                        Destination = NetworkArpPacket.TargetProtocolAddress.ToString();
                        Protocol = "Arp";
                        switch (NetworkArpPacket.Operation)
                        {
                            case ArpOperation.Request:
                                Info = $"Who has {Destination}? Tell {Source}";
                                break;
                            case ArpOperation.Response:
                                Info = $"{Source} is in {SourcePhysicalAddress}";
                                break;
                            default:
                                throw new NotImplementedException("Can't identify the kind of this packet");
                        }
                        return;
                    }

                    throw new NotImplementedException("Can't identify the kind of this packet");
                }

                switch (ethernetPacket.Type)
                {
                    case EthernetType.IPv4:
                        NetworkIpV4Packet = ethernetPacket.PayloadPacket as IPv4Packet;
                        if (NetworkIpV4Packet == null)
                            throw new NullReferenceException();
                        Source = NetworkIpV4Packet.SourceAddress.ToString();
                        Destination = NetworkIpV4Packet.DestinationAddress.ToString();
                        if (NetworkIpV4Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV4Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            /*
                                 *  the first byte
                                 *    --------------------------------------
                                 *    |  Source port  |  Destination port  |
                                 *    --------------------------------------
                                 *    |          Sequence number           |
                                 *    --------------------------------------
                                 *    |        Acknowledgement number      |
                                 *    --------------------------------------
                                 *    |              Others                |
                                 *    --------------------------------------
                                 *    |   Check sum   |    Urgent pointer  |
                                 *    --------------------------------------
                                 *    |             Options                |
                                 *    --------------------------------------
                                 *    |                Data                |
                                 *    --------------------------------------
                                 *
                                 */
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV4Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV4Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV4Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV4Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                    case EthernetType.IPv6:
                        NetworkIpV6Packet = ethernetPacket.PayloadPacket as IPv6Packet;
                        Source = NetworkIpV6Packet.SourceAddress.ToString();
                        Destination = NetworkIpV6Packet.DestinationAddress.ToString();
                        if (NetworkIpV6Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV6Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV6Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV6Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV6Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV6Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                }
            }
            else if (type == typeof(NullPacket))
            {
                var nullPacket = (NullPacket)LinkLayerPacket;
                switch (nullPacket.Protocol)
                {
                    case NullPacketType.IPv4:
                        NetworkIpV4Packet = nullPacket.PayloadPacket as IPv4Packet;
                        Source = NetworkIpV4Packet.SourceAddress.ToString();
                        Destination = NetworkIpV4Packet.DestinationAddress.ToString();
                        if (NetworkIpV4Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV4Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV4Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV4Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV4Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV4Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                    case NullPacketType.IPv6:
                        NetworkIpV6Packet = nullPacket.PayloadPacket as IPv6Packet;
                        Source = NetworkIpV6Packet.SourceAddress.ToString();
                        Destination = NetworkIpV6Packet.DestinationAddress.ToString();
                        if (NetworkIpV6Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV6Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV6Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV6Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV6Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV6Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                }
            }
            throw new NotImplementedException("Can't identify the kind of this packet");
        }

        public DotNetRawPacket(PacketCapture packetCapture, ulong packetId, DateTime startTime)
        {
            var rawCapture = packetCapture.GetPacket();
            Number = packetId;
            Bytes = rawCapture.Data;
            var dt = rawCapture.Timeval.Date.ToLocalTime();
            ArrivalTime = (dt - startTime).TotalSeconds;
            var header = (PcapHeader)packetCapture.Header;
            PacketBytesOnLine = header.PacketLength;
            PacketBytesCaptured = header.CaptureLength;
            LinkLayerType = rawCapture.LinkLayerType;
            LinkLayerPacket = Packet.ParsePacket(LinkLayerType, rawCapture.Data);
            
            
            
            /* Get layers number(save for data) */
            var linkLayerPacket = LinkLayerPacket;
            while (linkLayerPacket.HasPayloadPacket || linkLayerPacket.HasPayloadData)
            {
                if (linkLayerPacket.HasPayloadPacket)  // Payload packet
                {
                    linkLayerPacket = linkLayerPacket.PayloadPacket;
                    Layers++;
                }
                else  // Payload data
                    break;
            }

            var type = LinkLayerPacket.GetType();
            if (type == typeof(EthernetPacket))
            {
                var ethernetPacket = (EthernetPacket)LinkLayerPacket;
                SourcePhysicalAddress = ethernetPacket.SourceHardwareAddress.ToString();
                DestinationPhysicalAddress = ethernetPacket.DestinationHardwareAddress.ToString();
                if (Layers == 1)  // Only link layer, broadcast frame
                {
                    Source = ethernetPacket.SourceHardwareAddress.ToString();
                    Destination = "broadcast";
                    Protocol = ethernetPacket.Type.ToString();
                    Info = "Ethernet II";
                    return;
                }

                if (Layers == 2)  // Only network layer, such as Arp
                {
                    if (ethernetPacket.Type == EthernetType.Arp)
                    {
                        NetworkArpPacket = ethernetPacket.PayloadPacket as ArpPacket;
                        if (NetworkArpPacket == null)
                            throw new NullReferenceException();
                        Source = NetworkArpPacket.SenderProtocolAddress.ToString();
                        Destination = NetworkArpPacket.TargetProtocolAddress.ToString();
                        Protocol = "Arp";
                        switch (NetworkArpPacket.Operation)
                        {
                            case ArpOperation.Request:
                                Info = $"Who has {Destination}? Tell {Source}";
                                break;
                            case ArpOperation.Response:
                                Info = $"{Source} is in {SourcePhysicalAddress}";
                                break;
                            default:
                                throw new NotImplementedException("Can't identify the kind of this packet");
                        }
                        return;
                    }

                    throw new NotImplementedException("Can't identify the kind of this packet");
                }

                switch (ethernetPacket.Type)
                {
                    case EthernetType.IPv4:
                        NetworkIpV4Packet = ethernetPacket.PayloadPacket as IPv4Packet;
                        if (NetworkIpV4Packet == null)
                            throw new NullReferenceException();
                        Source = NetworkIpV4Packet.SourceAddress.ToString();
                        Destination = NetworkIpV4Packet.DestinationAddress.ToString();
                        if (NetworkIpV4Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV4Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            /*
                                 *  the first byte
                                 *    --------------------------------------
                                 *    |  Source port  |  Destination port  |
                                 *    --------------------------------------
                                 *    |          Sequence number           |
                                 *    --------------------------------------
                                 *    |        Acknowledgement number      |
                                 *    --------------------------------------
                                 *    |              Others                |
                                 *    --------------------------------------
                                 *    |   Check sum   |    Urgent pointer  |
                                 *    --------------------------------------
                                 *    |             Options                |
                                 *    --------------------------------------
                                 *    |                Data                |
                                 *    --------------------------------------
                                 *
                                 */
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV4Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV4Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV4Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV4Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                    case EthernetType.IPv6:
                        NetworkIpV6Packet = ethernetPacket.PayloadPacket as IPv6Packet;
                        Source = NetworkIpV6Packet.SourceAddress.ToString();
                        Destination = NetworkIpV6Packet.DestinationAddress.ToString();
                        if (NetworkIpV6Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV6Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV6Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV6Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV6Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV6Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                }
            }
            else if (type == typeof(NullPacket))
            {
                var nullPacket = (NullPacket)LinkLayerPacket;
                switch (nullPacket.Protocol)
                {
                    case NullPacketType.IPv4:
                        NetworkIpV4Packet = nullPacket.PayloadPacket as IPv4Packet;
                        Source = NetworkIpV4Packet.SourceAddress.ToString();
                        Destination = NetworkIpV4Packet.DestinationAddress.ToString();
                        if (NetworkIpV4Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV4Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV4Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV4Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV4Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV4Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV4Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                    case NullPacketType.IPv6:
                        NetworkIpV6Packet = nullPacket.PayloadPacket as IPv6Packet;
                        Source = NetworkIpV6Packet.SourceAddress.ToString();
                        Destination = NetworkIpV6Packet.DestinationAddress.ToString();
                        if (NetworkIpV6Packet.Protocol == ProtocolType.Tcp)
                        {
                            NetworkTcpPacket = NetworkIpV6Packet.PayloadPacket as TcpPacket;
                            Protocol = ProtocolType.Tcp.ToString();
                            Info = NetworkTcpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Udp)
                        {
                            NetworkUdpPacket = NetworkIpV6Packet.PayloadPacket as UdpPacket;
                            Protocol = ProtocolType.Udp.ToString();
                            Info = NetworkUdpPacket.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Icmp)  // IcmpV4
                        {
                            NetworkIcmpV4Packet = NetworkIpV6Packet.PayloadPacket as IcmpV4Packet;
                            Protocol = ProtocolType.Icmp.ToString();
                            Info = NetworkIcmpV4Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.IcmpV6)
                        {
                            NetworkIcmpV6Packet = NetworkIpV6Packet.PayloadPacket as IcmpV6Packet;
                            Protocol = ProtocolType.IcmpV6.ToString();
                            Info = NetworkIcmpV6Packet.ToString();
                            return;
                        }
                        else if (NetworkIpV6Packet.Protocol == ProtocolType.Igmp)
                        {
                            NetworkIgmpV2Packet = NetworkIpV6Packet.PayloadPacket as IgmpV2Packet;
                            Protocol = ProtocolType.Igmp.ToString();
                            Info = NetworkIgmpV2Packet.ToString();
                            return;
                        }
                        else
                            break;
                }
            }
            throw new NotImplementedException("Can't identify the kind of this packet");
        }
    }
}