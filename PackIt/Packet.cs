using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CRC8_Class;

namespace PackIt
{
    class Packet
    {
        // program constant definitions
        private const int SIZE_OF_PACKET = 7;
        private const byte PAD = 0xFF;
        private const byte SPACE = 32; // ASCII - lowest printable char
        private const byte TILDE = 126; // ASCII - highest printable char

        // const characters      
        private const byte EOT = 0x04;
        private const byte ENQ = 0x05;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte LF = 0x0A;
        private const byte CR = 0x0D;
        private const byte SOH = 0x01;

        public Packet()
        {
        }

        public byte[] Packetize(char[] data)
        {
            var packetized = new byte[SIZE_OF_PACKET];

            for (var i = 1; i < SIZE_OF_PACKET-1; i++)
            {
                packetized[i] = (byte)data[i - 1];
            }

            packetized[0] = SOH;
            packetized[6] = 0;

            var crcer = new CRC_Class(packetized).crcCalc();

            packetized[6] = (byte)crcer;
            return packetized;
        }
        
        public byte[] EndPacket(byte[] data)
        {
            var eotFlag = false;
            for (var i = 1; i < SIZE_OF_PACKET - 1; i++)
            {
                if ((int)data[i] == 0 && !eotFlag)
                {
                    data[i] = EOT; // End of transmission
                    eotFlag = true;
                }
                else if ((int)data[i] == 0)
                {
                    data[i] = byte.MaxValue;
                }
            }
            data[0] = SOH;
            data[6] = 0;

            var crcer = new CRC_Class(data).crcCalc();

            data[6] = (byte)crcer;
            return data;
        }
    }
}
