using System;
using Microsoft.SPOT;
using System.IO.Ports;

namespace RadioBlocks.NETMF
{
    public class RadioBlock
    {
        private SerialPort _port;

        /// <summary>
        /// Not sure if I need to use anything but check for !Success, but I put the responses in here anyway.
        /// </summary>
        /// <remarks>
        /// Packet format: | Command ID | Status |
        ///                |   1 byte   | 1 byte |
        /// </remarks>
        private class Responses
        {
            public static byte Success = 0x00;
            public static byte UnknownError = 0x01;
            public static byte OutOfMemory = 0x02;
            public static byte NoAckReceived = 0x11;
            public static byte ChannelAccessFailure = 0x40;
            public static byte NoPhysicalAckReceived = 0x41;
            public static byte InvalidCommandSize = 0x80;
            public static byte InvalidCRC = 0x81;
            public static byte Timeout = 0x82;
            public static byte UnknownCommand = 0x83;
            public static byte MalformedCommand = 0x84;
            public static byte InternalFlashError = 0x85;
            public static byte InvalidDataRequestPayloadSize = 0x86;
        }

        public RadioBlock( string port, int baudrate)
        {
            Debug.Print( "creating serial port (" + port + ", " + baudrate + ", " + Parity.None + ", 8, " + StopBits.One + ")");
            _port = new SerialPort( port, baudrate, Parity.None, 8, StopBits.One);
            Debug.Print( "setting serial port read timeout");
            _port.ReadTimeout = 1000;
            Debug.Print( "opening serial port");
            _port.Open();
            Debug.Print( "flushing serial port buffer");
            _port.Flush();

            Debug.Print( "RAM left: " + Debug.GC( true) + " bytes");
        }

        /// <summary>
        /// Just pass in the payload bytes and this method will create the packet + CRC
        /// </summary>
        /// <remarks>
        /// General command format: | Start Byte |  Size  | Payload |   CRC   |
        ///                         |   1 byte   | 1 byte | N bytes | 2 bytes |
        /// </remarks>
        /// <param name="payload"></param>
        private void SendCommand( byte[] payload)
        {
            int total_packet_size = 4 + payload.Length;
            byte[] packet = new byte[total_packet_size];
            packet[0] = 0xAB; // start byte is a constant
            packet[1] = (byte)payload.Length; // size is the size of the command id + options in payload field
            Array.Copy( payload, 0, packet, 2, payload.Length);
            CalculateCRC( packet);
        }

        /// <summary>
        /// Given the entire data packet, fills in the CRC bits
        /// </summary>
        /// <param name="packet"></param>
        private void CalculateCRC( byte[] packet)
        {

        }
    }
}
