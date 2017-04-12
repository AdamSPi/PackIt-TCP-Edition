using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackIt
{
    class Receiver
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
        public List<byte[]> ReceivePackets;   // stores packets to be received
        public string FileToSave;

        public Receiver()
        {
            ReceivePackets = new List<byte[]>();
            FileToSave = "Meep";
        }
    }
}
