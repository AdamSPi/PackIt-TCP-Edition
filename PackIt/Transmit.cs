using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static PackIt.Packet;

namespace PackIt
{
    class Transmitter
    {
        // program constant definitions
        private const int SIZE_OF_PACKET = 7;
        private const byte PAD = 0xFF;
        private const byte SPACE = 32;     // ASCII - lowest printable char
        private const byte TILDE = 126;    // ASCII - highest printable char

        // const characters      
        private const byte EOT = 0x04;
        private const byte ENQ = 0x05;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte LF = 0x0A;
        private const byte CR = 0x0D;
        private const byte SOH = 0x01;
        
        private byte[] chkConn;         // byte[] used for serial port send
        private byte[] nak;
        private byte[] ack;
        
        // lists to hold packets on both sender and receiver sides
        public List<byte[]> SendPackets;   // stores packets to be sent
        private Packet _packet;
        
        public Transmitter()
        {
            SendPackets = new List<byte[]>();
            _packet = new Packet();
        }

        public void PacketizeFile(string filepath)
        {
            var streamer = new StreamReader(filepath);
            var packetData = new char[5];
            while (!streamer.EndOfStream)
            {
                var array = new byte[SIZE_OF_PACKET];
                for (var i = 0; i < 5; i++)
                    packetData[i] = char.MinValue;
                var bytesLeft = streamer.Read(packetData, 0, 5);

                // remainder = 0
                if (bytesLeft == 5 && streamer.EndOfStream)
                {
                    // add last of chars
                    var packet = _packet.Packetize(packetData);
                    SendPackets.Add(packet);
                    // last packet will just have a EOT byte
                    var emptyPacket = new byte[7];
                    SendPackets.Add(_packet.EndPacket(emptyPacket));
                }
                // remainder < 5
                else if (bytesLeft < 5)
                {
                    var lastByte = _packet.Packetize(packetData);
                    // whatever's leftover will be EOT'ed
                    SendPackets.Add(_packet.EndPacket(lastByte));
                }
                // haven't reached EOT yet
                else
                {
                    SendPackets.Add(_packet.Packetize(packetData));
                }
            }
            streamer.Close();
        }
        
    }
}
