namespace DotNetPacketCaptor.ViewModels
{
    public class PacketDetailViewModel
    {
        public string ColNumber { get; }
        public string ColRaw { get; }
        public string ColAscii { get; }

        public PacketDetailViewModel(string colNumber, string colRaw, string colAscii)
        {
            ColNumber = colNumber;
            ColRaw = colRaw;
            ColAscii = colAscii;
        }
    }
}