using System;
using System.Collections.Generic;
using System.ComponentModel;
using DotNetPacketCaptor.Models;

namespace DotNetPacketCaptor.Ext
{
    public static class CollectionViewSourceExtension
    {
        public static List<PacketInfo> GetPacketInfo(this ICollectionView viewSource)
        {
            var collection = new List<PacketInfo>();
            foreach (var item in viewSource)
            {
                if (!(item is DotNetRawPacket packet))
                    throw new ArgumentException();
                var packetInfo = new PacketInfo(packet);
                collection.Add(packetInfo);
            }

            return collection;
        }
    }
}