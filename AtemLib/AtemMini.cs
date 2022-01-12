using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
 
namespace AtemLib
{
    public class AtemMini : IDisposable
    {
        private UDPServer _udp;
        private int _state;
 
        public int SessionID { get; private set; }
        public int PacketID { get; private set; }
        public int Counter { get; private set; }
 
        public bool Ready { get; private set; }
 
        public event EventHandler ReadyEvent;
 
        public AtemMini()
        {
        }
 
        public void Initialize()
        {
            _state = 0;
        }
 
        public void Connect(string address, int port)
        {
            _udp = new UDPServer(address, port, 10000, EthernetAdapterType.EthernetLANAdapter);
             
            Ready = false;
 
            try
            {
                _udp.EnableUDPServer();
                _udp.ReceiveDataAsync(OnReceiveData);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Exception in Connect: {0}", e.Message);
            }
        }
 
        public void Disconnect()
        {
            Ready = false;
 
            if (_udp != null)
            {
                _udp.DisableUDPServer();
            }
        }
 
        public void Dispose()
        {
            Disconnect();
 
            if (_udp != null)
            {
                _udp.Dispose();
            }
 
            _udp = null;
        }
 
        public void Send(byte[] msg)
        {
            _udp.SendData(msg, msg.Length);
        }
 
        public void OnReceiveData(UDPServer sock, int bytesReceived)
        {
            var msg = sock.IncomingDataBuffer;
 
            switch (_state)
            {
                case 1: // Hello
                    HelloAck();
                    break;
                case 2: // DeviceInfo
                    if (msg[0] == 0x0d)
                    {
                        SessionID = msg[2] * 256 + msg[3];
                    }
                    else if (msg[0] == 0x88)
                    {
                        PacketID = msg[10] * 256 + msg[11];
                        Ack();
                    }
                    break;
                case 3: // Idle
                    if (msg[0] == 0x88)
                    {
                        PacketID = msg[10] * 256 + msg[11];
                        Ack();
 
                        if (!Ready)
                        {
                            Ready = true;
 
                            if (ReadyEvent != null)
                            {
                                ReadyEvent(this, new EventArgs());
                            }
                        }
                    }
                    break;
                case 4: // Cut
                    if (msg[0] == 0x88)
                    {
                        PacketID = msg[10] * 256 + msg[11];
                        CutAck();
                    }
                    break;
                case 5: // Auto
                    if (msg[0] == 0x88)
                    {
                        PacketID = msg[10] * 256 + msg[11];
                        AutoAck();
                    }
                    break;
                case 6: // SetPreview
                    if (msg[0] == 0x88)
                    {
                        PacketID = msg[10] * 256 + msg[11];
                        SetPreviewAck();
                    }
                    break;
                default:
                    CrestronConsole.PrintLine("Received {0} bytes in state {1}, type: {2:X2}", msg.Length, _state, msg[0]);
                    break;
            }
 
            sock.ReceiveDataAsync(OnReceiveData);
        }
 
        public static byte Low(int n)
        {
            return (byte)(n & 0xFF);
        }
 
        public static byte High(int n)
        {
            return (byte)(n >> 8);
        }
 
        private byte[] CreateMessage(byte type, int size)
        {
            byte[] msg = new byte[size];
 
            msg[0] = type;
            msg[1] = (byte)size;
            msg[2] = High(SessionID);
            msg[3] = Low(SessionID);
 
            return msg;
        }
 
        public void Hello()
        {
            SessionID = 0x1234;
            Counter = 0;
 
            byte[] msg = CreateMessage(0x10, 20);
 
            _state = 1; // Hello
 
            msg[9] = 0x3f;
            msg[12] = 0x01;
 
            Send(msg);
        }
 
        private void HelloAck()
        {
            byte[] msg = CreateMessage(0x80, 12);
 
            _state = 2; // DeviceInfo
            Counter = 1;
 
            msg[9] = 0xc3;
 
            Send(msg);
        }
 
        private void Ack()
        {
            byte[] msg;
 
            _state = 3; // Idle
 
            msg = CreateMessage(0x80, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[9] = 0x30;
 
            Send(msg);
 
            msg = CreateMessage(0x88, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[11] = 0x01;
 
            Send(msg);
        }
 
        public void Cut()
        {
            byte[] msg = CreateMessage(0x08, 24);
 
            _state = 4; // Cut
            Counter += 1;
 
            msg[10] = High(Counter);
            msg[11] = Low(Counter);
            msg[13] = 0x0c;
            msg[14] = 0x4f;
            msg[15] = 0x03;
            msg[16] = 0x44; // D
            msg[17] = 0x43; // C
            msg[18] = 0x75; // u
            msg[19] = 0x74; // t
            msg[21] = 0x30;
            msg[22] = 0x73;
            msg[23] = 0x01;
 
            Send(msg);
        }
 
        private void CutAck()
        {
            byte[] msg;
 
            _state = 3;
 
            msg = CreateMessage(0x80, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[9] = 0x51;
 
            Send(msg);
 
            Counter += 1;
 
            msg = CreateMessage(0x88, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[10] = High(Counter);
            msg[11] = Low(Counter);
 
            Send(msg);
        }
 
        public void Auto()
        {
            byte[] msg = CreateMessage(0x08, 24);
 
            _state = 5; // Auto
            Counter += 1;
 
            msg[10] = High(Counter);
            msg[11] = Low(Counter);
            msg[13] = 0x0c;
            msg[14] = 0x4f;
            msg[15] = 0x03;
            msg[16] = 0x44; // D
            msg[17] = 0x41; // A
            msg[18] = 0x75; // u
            msg[19] = 0x74; // t
            msg[21] = 0x9d;
            msg[22] = 0x0b;
            msg[23] = 0x01;
 
            Send(msg);
        }
 
        private void AutoAck()
        {
            byte[] msg;
 
            _state = 3;
 
            msg = CreateMessage(0x80, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[9] = 0x45;
 
            Send(msg);
 
            Counter += 1;
 
            msg = CreateMessage(0x88, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[10] = High(Counter);
            msg[11] = Low(Counter);
 
            Send(msg);
        }
 
        public void SetPreview(int camera)
        {
            byte[] msg = CreateMessage(0x88, 24);
 
            _state = 6;
            Counter += 1;
 
            msg[10] = High(Counter);
            msg[11] = Low(Counter);
            msg[13] = 0x0c;
            msg[14] = 0x01;
            msg[15] = 0x01;
            msg[16] = 0x43; // C
            msg[17] = 0x50; // P
            msg[18] = 0x76; // v
            msg[19] = 0x49; // I
            msg[21] = 0x47; // G
            msg[23] = (byte)camera;
 
            Send(msg);
        }
 
        private void SetPreviewAck()
        {
            byte[] msg;
 
            _state = 3;
 
            msg = CreateMessage(0x80, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[9] = 0xbb;
 
            Send(msg);
 
            Counter += 1;
 
            msg = CreateMessage(0x88, 12);
            msg[4] = High(PacketID);
            msg[5] = Low(PacketID);
            msg[10] = High(Counter);
            msg[11] = Low(Counter);
 
            Send(msg);
        }
    }
}