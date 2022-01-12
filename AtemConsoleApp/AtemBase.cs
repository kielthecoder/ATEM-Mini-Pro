using System;
using System.Net;
using System.Net.Sockets;

namespace AtemConsoleApp
{
    class AtemBase : IDisposable
    {
        private UdpClient _udp;
        private int _state;

        public int SessionID { get; private set; }
        public int PacketID { get; private set; }
        public int Counter { get; private set; }

        public AtemBase()
        {
        }

        public void Connect(string address, int portNumber)
        {
            _udp = new UdpClient();

            SessionID = 0x1234; // temporary

            try
            {
                _udp.Connect(address, portNumber);
                _udp.BeginReceive(new AsyncCallback(OnReceiveData), _udp);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Connect: {0}", e.Message);
            }
        }

        public void Disconnect()
        {
            if (_udp != null)
            {
                _udp.Close();
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

        private void OnReceiveData(IAsyncResult result)
        {
            var sock = result.AsyncState as UdpClient;
            var src = new IPEndPoint(0, 0);
            var msg = sock.EndReceive(result, ref src);

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
                    Console.WriteLine("Received {0} bytes in state {1}, type: {2:X2}", msg.Length, _state, msg[0]);
                    break;
            }

            sock.BeginReceive(new AsyncCallback(OnReceiveData), sock);
        }

        public void Send(byte[] msg)
        {
            if (_udp != null)
            {
                _udp.Send(msg, msg.Length);
            }
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
            byte[] msg = CreateMessage(0x10, 20);

            _state = 1;

            msg[9] = 0x3f;
            msg[12] = 0x01;

            Send(msg);
        }

        private void HelloAck()
        {
            byte[] msg = CreateMessage(0x80, 12);

            _state = 2;
            Counter = 1;

            msg[9] = 0xc3;

            Send(msg);
        }

        public void Ack()
        {
            byte[] msg;

            _state = 3;

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
            byte[] msg = CreateMessage(0x08, 24); ;

            _state = 4;
            Counter += 1;

            msg[10] = High(Counter);
            msg[11] = Low(Counter);
            msg[13] = 0x0c;
            msg[14] = 0x4f;
            msg[15] = 0x03;
            msg[16] = 0x44;
            msg[17] = 0x43;
            msg[18] = 0x75;
            msg[19] = 0x74;
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
            byte[] msg = CreateMessage(0x08, 24); ;

            _state = 5;
            Counter += 1;

            msg[10] = High(Counter);
            msg[11] = Low(Counter);
            msg[13] = 0x0c;
            msg[14] = 0x4f;
            msg[15] = 0x03;
            msg[16] = 0x44;
            msg[17] = 0x41;
            msg[18] = 0x75;
            msg[19] = 0x74;
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
            msg[16] = 0x43;
            msg[17] = 0x50;
            msg[18] = 0x76;
            msg[19] = 0x49;
            msg[21] = 0x47;
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

        static byte Low(int n)
        {
            return (byte)(n & 0xFF);
        }

        static byte High(int n)
        {
            return (byte)(n >> 8);
        }
    }
}
