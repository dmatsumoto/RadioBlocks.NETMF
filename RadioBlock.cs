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
            // instead of adding a test project for one function, I just uncomment this
            // block to make sure the CRC calculation matches the result from the
            // last page of the RadioBlock Serial Protocol manual.
            /*
            byte[] crc = CalculateCRC( new byte[] { 0xAB, 0x02, 0x00, 0x00, 0x00, 0x00 } );
            Debug.Assert( crc[0] == 0x51);
            Debug.Assert( crc[0] == 0xE2);
             */

            Debug.Print( "creating serial port (" + port + ", " + baudrate + ", " + Parity.None + ", 8, " + StopBits.One + ")");
            _port = new SerialPort( port, baudrate, Parity.None, 8, StopBits.One);
            Debug.Print( "setting serial port read timeout");
            _port.ReadTimeout = 1000;
            Debug.Print( "opening serial port");
            _port.Open();
            Debug.Print( "flushing serial port buffer");
            _port.Flush();
            Debug.Print( "registering serial port data event handler");
            _port.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);            
            Debug.Print( "RAM left: " + Debug.GC( true) + " bytes");
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// This event handler gets fired each time new data arrives in the serial buffer.  It appends the data to the
        /// buffer and then calls the command parser function.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // try reading bytes until no bytes left            
            int bytes_actually_read = 0;
            while( _port.BytesToRead > 0) {
                int bytes_to_read = 0;
                try {
                    bytes_to_read = _port.BytesToRead;
                    byte[] temp = new byte[bytes_to_read];
                    bytes_actually_read = _port.Read( temp, 0, bytes_to_read);
                    Debug.Assert( bytes_to_read == bytes_actually_read, "Need to handle case where we don't read all of the bytes");
                } catch( Exception ex) {
                    Debug.Print( "ERROR: detected error in serialPort_DataReceived (" + ex.Message + ")!");
                    Debug.Print( "=> _port.BytesToRead: " + bytes_to_read);
                    Debug.Print( "Flushing buffer and starting over");
                    _port.Flush();
                    return;
                }
            }

            // if we get into this handler but there wasn't actually any data to read, then bail
            if( bytes_actually_read == 0)
                return;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------
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
            byte[] crc = CalculateCRC( packet);
            packet[total_packet_size - 2] = crc[0];
            packet[total_packet_size - 1] = crc[1];

            _port.Write( packet, 0, packet.Length);
            // ignoring response for now
        }

        /// <summary>
        /// Given the entire data packet, fills in the CRC bits.
        /// </summary>
        /// <param name="packet"></param>
        private byte[] CalculateCRC( byte[] packet)
        {
            short crc = 0x1234;
            // CRC is calculated for the payload only, so start the calcs from offset 2
            // packet length includes the CRC, so only loop up to the third-from-last element
            for( int i=2; i<packet.Length-2; i++) {
                byte data = packet[i];
                data ^= (byte)(crc & 0xFF);
                data ^= (byte)(data << 4);
                short a1 = (short)((short)data << 8);
                short a2 = (short)((crc >> 8) & 0xFF);
                short a = (short)(a1 | a2);
                short b = (short)(data >> 4);
                short c = (short)((short)data << 3);
                crc = (short)(a ^ b ^ c);
            }

            return new byte[] { (byte)(crc & 0xFF), (byte)((crc & 0xFF00) >> 8) };
        }

        private void Test()
        {
            SendCommand( new byte[] { 0x01 });

        }

        public void ToggleLed()
        {
            SendCommand( new byte[] { 0x80, 0x02 });
        }
    }
}
