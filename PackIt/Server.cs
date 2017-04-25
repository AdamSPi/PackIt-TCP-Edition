using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using CRC8_Class;
using Microsoft.Win32;
using static PackIt.MainWindow;

namespace PackIt
{
    class Server
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

        private byte[] chkConn; // byte[] used for serial port send
        private byte[] nak;
        private byte[] ack;

        private Socket clientConnection;
        private Thread listener;

        public IPAddress addr;
        public int portNumber = 5000;
        public NetworkStream Streamer;

        // lists to hold packets on both sender and receiver sides
        public List<byte[]> ReceivePackets; // stores packets to be received
        public string FileToSave;
        public TcpClient ConnectededClient;

        enum State
        {
            SOH,
            Data
        }

        private State _state;


        private byte[] _data;
        private int _totalRead;
        private int _totalLeft;

        public Server()
        {
            ReceivePackets = new List<byte[]>();
            FileToSave = "Meep";
            _state = State.SOH;

            string host = Dns.GetHostName();
            IPAddress[] addrs = Dns.GetHostAddresses(host);

            foreach (IPAddress ip in addrs)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var meep = ip.ToString();
                    var meeparr = meep.Split('.');
                    // No VMware IPs
                    if(meeparr[0] != "192")
                    {
                        addr = ip;
                    }
                }
            }
        }

        // Start listening on socket for client connections/data
        public async void SocketListener()
        {
            var sockListener = new TcpListener(addr, portNumber);
            try
            {
                sockListener.Start();
            }
            catch (Exception e)
            {
                await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                 {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).StatusBarCOMPORT.Content = "Port already in use";
                })
                );
                return;
            }

            // Wait for connection to establish
            ConnectededClient = await sockListener.AcceptTcpClientAsync();

            await Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).TextDisplay.AppendText(
                        "Client connected from " + ConnectededClient.Client.RemoteEndPoint.ToString().Split(':')[0] + "\n");
                })
            );

            while (true)
            {
                    Streamer = ConnectededClient.GetStream();
                    var packet = new byte[7];
                    var response = new byte[1];
                    while (Streamer.DataAvailable)
                    {
                        var recv = (byte) Streamer.ReadByte();
                        if (recv == SOH && _state != State.Data)
                        {
                            _state = State.SOH;
                        }
                        else if (_state == State.SOH)
                        {
                            if (recv == ENQ)
                            {
                                response[0] = ACK;
                                Streamer.Write(response, 0, response.Length);
                                await Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Background,
                                    new Action(() =>
                                        {
                                            ((MainWindow)
                                                    System.Windows.Application.Current.MainWindow)
                                                .connect_png.Visibility = Visibility.Visible;
                                            ((MainWindow)
                                                    System.Windows.Application.Current.MainWindow)
                                                .connect_not_png.Visibility = Visibility.Hidden;
                                        }
                                    )
                                );
                            }
                            else
                            {
                                packet[0] = recv;
                                Streamer.ReadTimeout = 1000;
                                for (var i = 1; i < SIZE_OF_PACKET; i++)
                                {
                                    try
                                    {
                                        recv = (byte) Streamer.ReadByte();
                                        packet[i] = recv;
                                    }
                                    catch (Exception err)
                                    {
                                        i = 7;
                                    }
                                }

                                await Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Background,
                                    new Action(() =>
                                        {
                                            if (((MainWindow) System.Windows.Application.Current.MainWindow).IsHex)
                                            {
                                                var hx =
                                                    new TextRange(
                                                        ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                        .TextDisplay.Document.ContentEnd,
                                                        ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                        .TextDisplay.Document.ContentEnd);
                                                hx.Text = " Recv: " +
                                                          BitConverter.ToString(packet).Replace('-', ' ') + "\n";
                                                hx.ApplyPropertyValue(TextElement.ForegroundProperty,
                                                    Brushes.DarkRed);
                                                ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                    .TextDisplay.ScrollToEnd();
                                            }
                                            else { 
                                               var tx =
                                                        new TextRange(
                                                            ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.Document.ContentEnd,
                                                            ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.Document.ContentEnd);
                                                    tx.Text = " Recv: " + System.Text.Encoding.ASCII.GetString(packet) +
                                                              "\n";
                                                    tx.ApplyPropertyValue(TextElement.ForegroundProperty,
                                                        Brushes.DarkRed);
                                                    ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                        .TextDisplay.ScrollToEnd();
                                            }
                                        }
                                    )
                                );

                                response[0] = NAK;
                                Streamer.Write(response, 0, response.Length);
                            }
                        }
                        switch (_state)
                        {
                            case State.SOH:
                                if (recv == SOH)
                                {
                                    _state = State.Data;
                                    _data = new byte[7];
                                    _totalRead = 0;
                                    _totalLeft = SIZE_OF_PACKET;
                                    _data[_totalRead] = recv;
                                    _totalRead++;
                                    _totalLeft--;
                                }
                                continue;
                            case State.Data:
                                _data[_totalRead] = recv;
                                _totalRead++;
                                _totalLeft--;

                                if (_totalLeft < 1)
                                {
                                    if (new CRC_Class(_data).crcCalc() == 0)
                                    {
                                        response[0] = ACK;
                                        Streamer.Write(response, 0, response.Length);

                                        await Application.Current.Dispatcher.BeginInvoke(
                                            DispatcherPriority.Background,
                                            new Action(() =>
                                            {
                                                if(((MainWindow) System.Windows.Application.Current.MainWindow).IsHex)
                                                {
                                                    ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                        .TextDisplay.AppendText(
                                                            "Recv: " +
                                                            BitConverter.ToString(_data).Replace('-', ' ') +
                                                            "\n");
                                                    ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                        .TextDisplay.ScrollToEnd();
                                                }
                                                else{ 
                                                        ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.AppendText(
                                                                "Recv: " + System.Text.Encoding.ASCII.GetString(_data) +
                                                                "\n");
                                                        ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.ScrollToEnd();
                                                }
                                            })
                                        );

                                        ReceivePackets.Add(_data);

                                        var dataBytes = new byte[SIZE_OF_PACKET - 2];
                                        for (var i = 1; i < SIZE_OF_PACKET - 1; i++)
                                        {
                                            dataBytes[i - 1] = _data[i];
                                        }

                                        foreach (var bytes in dataBytes)
                                        {
                                            if (bytes == EOT)
                                            {
                                                if (!string.IsNullOrEmpty(FileToSave))
                                                {
                                                    SaveFileDialog saveDialog = new SaveFileDialog();
                                                    if (saveDialog.ShowDialog() == true)
                                                    {
                                                        FileToSave = saveDialog.FileName;
                                                        if (ReceivePackets.Count > 0)
                                                        {
                                                            StringBuilder stringBuilder = new StringBuilder();
                                                            var count = 5;
                                                            char[] charlie = new char[7];
                                                            foreach (byte[] bagets in ReceivePackets)
                                                            {
                                                                for (var i = 1; i < bagets.Length - 1; i++)
                                                                {
                                                                    charlie[i] = (char) bagets[i];
                                                                    if (bagets[i] == EOT)
                                                                        count = i - 1;
                                                                }
                                                                // Append just data no soh or crc
                                                                stringBuilder.Append(charlie, 1, count);
                                                            }
                                                            var dest = new char[stringBuilder.Length];
                                                            stringBuilder.CopyTo(0, dest, 0, stringBuilder.Length);
                                                            var final = new byte[dest.Length];
                                                            for (var i = 0; i < dest.Length; i++)
                                                            {
                                                                final[i] = (byte) dest[i];
                                                            }
                                                            File.WriteAllBytes(FileToSave, final);
                                                            await Application.Current.Dispatcher.BeginInvoke(
                                                                DispatcherPriority.Background,
                                                                new Action(() =>
                                                                    {
                                                                        ((MainWindow)
                                                                            System.Windows.Application.Current
                                                                                .MainWindow).TextDisplay.AppendText(
                                                                            "Successful transfer\n");
                                                                        ((MainWindow)
                                                                                System.Windows.Application.Current
                                                                                    .MainWindow).StatusBarCOMPORT
                                                                            .Content =
                                                                            "File Successfully Saved!";
                                                                        ((MainWindow)
                                                                                System.Windows.Application.Current
                                                                                    .MainWindow).Percent
                                                                            .Content =
                                                                            "100%";
                                                                        ((MainWindow)
                                                                                System.Windows.Application.Current
                                                                                    .MainWindow).ProgressBar.Value =
                                                                            100;
                                                                    }
                                                                )
                                                            );
                                                        }
                                                    }
                                                }
                                                ReceivePackets.Clear();
                                            }
                                        }
                                    }
                                    // Bad Packets go here...
                                    else
                                    {
                                        response[0] = NAK;
                                        Streamer.Write(response, 0, response.Length);

                                        await Application.Current.Dispatcher.BeginInvoke(
                                            DispatcherPriority.Background,
                                            new Action(() =>
                                            {
                                                if(((MainWindow) System.Windows.Application.Current.MainWindow).IsHex)
                                                {
                                                    var hx =
                                                        new TextRange(
                                                            ((MainWindow)
                                                                System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.Document.ContentEnd,
                                                            ((MainWindow)
                                                                System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.Document.ContentEnd);
                                                    hx.Text = " Recv: " +
                                                              BitConverter.ToString(_data).Replace('-', ' ') + "\n";
                                                    hx.ApplyPropertyValue(TextElement.ForegroundProperty,
                                                        Brushes.DarkRed);
                                                    ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                        .TextDisplay.ScrollToEnd();
                                                }
                                                else { 
                                                    var tx =
                                                            new TextRange(
                                                                ((MainWindow)
                                                                    System.Windows.Application.Current.MainWindow)
                                                                .TextDisplay.Document.ContentEnd,
                                                                ((MainWindow)
                                                                    System.Windows.Application.Current.MainWindow)
                                                                .TextDisplay.Document.ContentEnd);
                                                        tx.Text = " Recv: " +
                                                                  System.Text.Encoding.ASCII.GetString(_data) + "\n";
                                                        tx.ApplyPropertyValue(TextElement.ForegroundProperty,
                                                            Brushes.DarkRed);
                                                        ((MainWindow) System.Windows.Application.Current.MainWindow)
                                                            .TextDisplay.ScrollToEnd();
                                                }
                                            })
                                        );

                                    }
                                    // Reset for next packet
                                    _state = State.SOH;
                                }
                                continue;
                        }
                    }


                }
        }
    }
}
