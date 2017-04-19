using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using CRC8_Class;

namespace PackIt
{
    public partial class MainWindow : Window
    {
        enum ErrorRecovery
        {
            NONE,
            CRCRecovery,
            CRCNoRecovery,
            SOHRecovery
        }


        enum State
        {
            SOH,
            Data
        }

        private byte[] _data;
        private int _totalRead;
        private int _totalLeft;
        
        private Client _client;
        private Server _server;
        
        private Thread _serverThread;

        private State _state;
        private ErrorRecovery _errorRecovery = ErrorRecovery.NONE;


        private const int SIZE_OF_PACKET = 7;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;
        private const byte EOT = 0x04;
        private const byte ENQ = 0x05;
        private const byte LF = 0x0A;
        private const byte CR = 0x0D;
        private const byte SOH = 0x01;

        public bool IsClient = true;
        public bool IsHex = true;
        public bool IsConnected = false;
        public bool IsTested = false;

        public MainWindow()
        {
            _client = new Client();
            _server = new Server();

            InitializeComponent();
            InitializeWindow();

            _server.portNumber = int.Parse(PortText.Text);
            _client.portNumber = int.Parse(PortText.Text);
        }

        public void InitializeWindow()
        {
            CRCErrorBox.SelectedIndex = 0;

            TextDisplay.AppendText("\n");
            _state = State.SOH;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                e.Handled = true;
                this.DragMove();
            }
        }

        private void Close_MouseDown(object sender, RoutedEventArgs e)
        {
            if (!IsClient)
            {
                try { 
                    _serverThread.Abort();
                }
                catch (Exception err)
                {

                }
        }
            
            Close();
        }

        private void Mini_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void File_MouseDown(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _client.PacketizeFile(openFileDialog.FileName);
                FileName.Content = openFileDialog.FileName;
                if (IsConnected && IsTested)
                {
                    send_not_png.Visibility = Visibility.Hidden;
                    send_png.Visibility = Visibility.Visible;
                }
            }
        }

        private void TransmitOnClick(object sender, RoutedEventArgs e)
        {
            receive_png.Visibility = Visibility.Hidden;
            receive_not_png.Visibility = Visibility.Visible;
            transmit_png.Visibility = Visibility.Visible;
            transmit_not_png.Visibility = Visibility.Hidden;
            test_not_png.Visibility = Visibility.Hidden;
            test_png.Visibility = Visibility.Visible;
            file_png.Visibility = Visibility.Visible;
            file_not_png.Visibility = Visibility.Hidden;
            StatusBarText.Content = "Waiting";
            StatusBarCOMPORT.Content = "Client";
            IsClient = true;
            OpenItem.IsEnabled = true;
            AddrText.IsEnabled = true;
        }

        private void ReceiveOnClick(object sender, RoutedEventArgs e)
        {
            transmit_png.Visibility = Visibility.Hidden;
            transmit_not_png.Visibility = Visibility.Visible;
            receive_png.Visibility = Visibility.Visible;
            receive_not_png.Visibility = Visibility.Hidden;
            file_not_png.Visibility = Visibility.Visible;
            file_png.Visibility = Visibility.Hidden;
            send_png.Visibility = Visibility.Hidden;
            send_not_png.Visibility = Visibility.Visible;
            connect_not_png.Visibility = Visibility.Visible;
            connect_png.Visibility = Visibility.Hidden;
            test_not_png.Visibility = Visibility.Visible;
            test_png.Visibility = Visibility.Hidden;
            StatusBarText.Content = "Waiting";
            StatusBarCOMPORT.Content = "Server";
            FileName.Content = "No file selected";
            IsClient = false;
            OpenItem.IsEnabled = false;
            AddrText.IsEnabled = false;
        }

        private void HexOnClick(object sender, RoutedEventArgs e)
        {
            ascii_png.Visibility = Visibility.Hidden;
            ascii_not_png.Visibility = Visibility.Visible;
            hex_png.Visibility = Visibility.Visible;
            hex_not_png.Visibility = Visibility.Hidden;
            IsHex = true;
        }

        private void TextOnClick(object sender, RoutedEventArgs e)
        {
            hex_png.Visibility = Visibility.Hidden;
            hex_not_png.Visibility = Visibility.Visible;
            ascii_png.Visibility = Visibility.Visible;
            ascii_not_png.Visibility = Visibility.Hidden;
            IsHex = false;
        }

        private void PortOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsClient)
                {
                    try
                    {
                        _client.Connect();
                    }
                    catch (Exception er)
                    {
                        StatusBarCOMPORT.Content = er.Message;
                    }
                }
                else
                {
                    connect_not_png.Visibility = Visibility.Hidden;
                    connect_png.Visibility = Visibility.Visible;
                    _serverThread = new Thread(new ThreadStart(_server.SocketListener));
                    _serverThread.Start();
                    AddrText.Text = _server.addr.ToString();
                }
                Connected();
            }
            catch (Exception err)
            {
                StatusBarCOMPORT.Content = err.Message;
            }
        }

        private void Leave_png_OnMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsClient)
                {
                    _client = new Client();
                }
                else
                {
                    _serverThread.Abort();
                    _server = new Server();
                }
                Disconnected();
            }
            catch (Exception err)
            {
                StatusBarCOMPORT.Content = err.Message;
            }
        }

        public void Connected()
        {
            disconnect_png.Visibility = Visibility.Hidden;
            not_connected_png.Visibility = Visibility.Hidden;

            port_png.Visibility = Visibility.Visible;
            port_not_png.Visibility = Visibility.Hidden;
            leave_png.Visibility = Visibility.Visible;

            connected_png.Visibility = Visibility.Visible;
            connected2_png.Visibility = Visibility.Visible;

            DisconnectItem.IsEnabled = true;
            ConnectItem.IsEnabled = false;
            IsConnected = true;

            if (IsClient)
            {
                StatusBarCOMPORT.Content = "Connected to " + AddrText.Text;
            }
            else
            {
                StatusBarCOMPORT.Content = "Listening on " + _server.addr;
            }
        }

        private void Disconnected()
        {
            connect_png.Visibility = Visibility.Hidden;
            connected_png.Visibility = Visibility.Hidden;
            connected2_png.Visibility = Visibility.Hidden;

            port_not_png.Visibility = Visibility.Visible;
            port_png.Visibility = Visibility.Hidden;
            leave_png.Visibility = Visibility.Hidden;

            connect_not_png.Visibility = Visibility.Visible;
            disconnect_png.Visibility = Visibility.Visible;
            not_connected_png.Visibility = Visibility.Visible;

            send_not_png.Visibility = Visibility.Visible;
            send_png.Visibility = Visibility.Hidden;


            test_not_png.Visibility = Visibility.Hidden;
            test_png.Visibility = Visibility.Visible;

            DisconnectItem.IsEnabled = false;
            ConnectItem.IsEnabled = true;
            IsConnected = false;

            StatusBarCOMPORT.Content = "Not Connected";
            StatusBarText.Content = "Waiting";
        }

        private void Clear_png_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            FileName.Content = "No file selected";
            TextDisplay.Document.Blocks.Clear();
            send_not_png.Visibility = Visibility.Visible;
            send_png.Visibility = Visibility.Hidden;
            TextDisplay.AppendText("\n");
            ProgressBar.Value = 0;
            Percent.Content = "0%";
        }

        private void Send_png_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsConnected && IsTested && IsClient)
            {
                StatusBarText.Content = "Transmitting";

                leave_png.Visibility = Visibility.Hidden;
                send_not_png.Visibility = Visibility.Visible;
                send_png.Visibility = Visibility.Hidden;
                clear_not_png.Visibility = Visibility.Visible;
                clear_png.Visibility = Visibility.Visible;
                file_not_png.Visibility = Visibility.Visible;
                file_png.Visibility = Visibility.Hidden;
                receive_not_png.Visibility = Visibility.Hidden;
                receive_png.Visibility = Visibility.Visible;

                DisconnectItem.IsEnabled = false;
                TransmitItem.IsEnabled = false;
                ReceiveItem.IsEnabled = false;
                OpenItem.IsEnabled = false;

                var numOfErrors = 0;
                var iter = 0;
                var errorFlag = false;
                var packet = new byte[1] {0};
                var acknowledge = new byte[1] {ACK};
                var noAcknowledge = new byte[1] {NAK};

                _client.Streamer.ReadTimeout = 1000;

                while (numOfErrors < 3 && iter < _client.SendPackets.Count )
                {
                    var sendPacket = _client.SendPackets[iter];
                    
                    switch (IsHex)
                    {
                        case true:
                                TextDisplay.AppendText("Sent: " + BitConverter.ToString(sendPacket).Replace('-', ' ') + "\n");
                                TextDisplay.ScrollToEnd();
                            break;
                        case false:
                            TextDisplay.AppendText("Sent: " + System.Text.Encoding.ASCII.GetString(sendPacket) + "\n");
                            TextDisplay.ScrollToEnd();
                            break;
                    }

                    // Error Recovery 
                    if (_errorRecovery != ErrorRecovery.NONE)
                    {
                        if (!errorFlag && iter == 3)
                        {
                            packet = new byte[7];
                            // Copy good packet to a safe packet
                            for (var i = 0; i < sendPacket.Length; i++)
                            {
                                packet[i] = sendPacket[i];
                            }
                            // Set error flag
                            errorFlag = Corrupt(sendPacket, _errorRecovery);
                        }
                        else if (errorFlag)
                        {
                            // Recover
                            for (var i = 0; i < packet.Length; i++)
                            {
                                sendPacket[i] = packet[i];
                            }
                            errorFlag = false;
                        }
                    }

                    // Send the packet !
                    var byteToRead = 0;
                    try
                    {
                        _client.Streamer.Write(sendPacket, 0, sendPacket.Length);
                        byteToRead = _client.Streamer.ReadByte();
                    }
                    catch (TimeoutException err)
                    {
                        numOfErrors++;
                        var to = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                        to.Text = "TIMEOUT\n";
                        to.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                        TextDisplay.ScrollToEnd();
                        StatusBarCOMPORT.Content = err.Message;
                        continue;
                    }
                    catch (Exception err)
                    {
                        var pc = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                        pc.Text = "Server offline\n";
                        pc.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                        TextDisplay.ScrollToEnd();

                        TextDisplay.AppendText(err.Message);
                        
                        StatusBarText.Content = "Waiting";

                        leave_png.Visibility = Visibility.Visible;
                        send_not_png.Visibility = Visibility.Hidden;
                        send_png.Visibility = Visibility.Visible;
                        clear_not_png.Visibility = Visibility.Hidden;
                        clear_png.Visibility = Visibility.Visible;
                        file_not_png.Visibility = Visibility.Hidden;
                        file_png.Visibility = Visibility.Visible;
                        receive_not_png.Visibility = Visibility.Visible;
                        receive_png.Visibility = Visibility.Hidden;

                        DisconnectItem.IsEnabled = true;
                        TransmitItem.IsEnabled = true;
                        ReceiveItem.IsEnabled = true;
                        OpenItem.IsEnabled = true;

                        return;
                    }

                    // Either got a ACK or NAK
                    switch (byteToRead)
                    {
                        case ACK:
                            iter++;
                            numOfErrors = 0;
                            switch (IsHex)
                            {
                                case true:
                                    var hx = new TextRange(TextDisplay.Document.ContentEnd,
                                        TextDisplay.Document.ContentEnd);
                                    hx.Text = " Recv: " + BitConverter.ToString(acknowledge) + "\n";
                                    hx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkGreen);
                                    TextDisplay.ScrollToEnd();
                                    break;
                                case false:
                                    var tx = new TextRange(TextDisplay.Document.ContentEnd,
                                        TextDisplay.Document.ContentEnd);
                                    tx.Text = " Recv: ACK\n";
                                    tx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkGreen);
                                    TextDisplay.ScrollToEnd();
                                    break;
                            }
                            continue;
                        case NAK:
                            numOfErrors++;
                            switch (IsHex)
                            {
                                case true:
                                    var hx = new TextRange(TextDisplay.Document.ContentEnd,
                                        TextDisplay.Document.ContentEnd);
                                    hx.Text = " Recv: " + BitConverter.ToString(noAcknowledge) + "\n";
                                    hx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                                    TextDisplay.ScrollToEnd();
                                    break;
                                case false:
                                    var tx = new TextRange(TextDisplay.Document.ContentEnd,
                                        TextDisplay.Document.ContentEnd);
                                    tx.Text = " Recv: NAK \n";
                                    tx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                                    TextDisplay.ScrollToEnd();
                                    break;
                            }
                            continue;
                    }

                }
                if (numOfErrors >= 3)
                {
                    TextDisplay.AppendText("Too many errors :/\n");
                    TextDisplay.ScrollToEnd();
                    _client.Streamer.Close();
                    Disconnected();
                }
                else
                {
                    ProgressBar.Value = 100;
                    Percent.Content = "100%";
                    TextDisplay.AppendText("Success\n");
                    TextDisplay.ScrollToEnd();
                }

                StatusBarText.Content = "Waiting";

                leave_png.Visibility = Visibility.Visible;
                send_not_png.Visibility = Visibility.Hidden;
                send_png.Visibility = Visibility.Visible;
                clear_not_png.Visibility = Visibility.Hidden;
                clear_png.Visibility = Visibility.Visible;
                file_not_png.Visibility = Visibility.Hidden;
                file_png.Visibility = Visibility.Visible;
                receive_not_png.Visibility = Visibility.Visible;
                receive_png.Visibility = Visibility.Hidden;

                DisconnectItem.IsEnabled = true;
                TransmitItem.IsEnabled = true;
                ReceiveItem.IsEnabled = true;
                OpenItem.IsEnabled = true;

                errorFlag = false;
                _client.SendPackets.Clear();
            }

        }

        private bool Corrupt(byte[] packet, ErrorRecovery mode)
        {
            switch (mode)
            {
                case ErrorRecovery.CRCRecovery:
                    packet[6]++;
                    return true;
                case ErrorRecovery.CRCNoRecovery:
                    packet[6]++;
                    return false;
                case ErrorRecovery.SOHRecovery:
                    packet[0] = (byte) 2;
                    return true;
                default:
                    return true;
            }
        }

        private void Test_png_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsConnected || !IsClient)
            {
                return;
            }
            StatusBarText.Content = "Connecting";
            var res = _client.TestConnection();

            switch (res)
            {
                case false:
                    StatusBarCOMPORT.Content = "Failure...";
                    StatusBarText.Content = "Waiting";
                    break;
                case true:
                    connect_not_png.Visibility = Visibility.Hidden;
                    connect_png.Visibility = Visibility.Visible;

                    test_not_png.Visibility = Visibility.Visible;
                    test_png.Visibility = Visibility.Hidden;

                    StatusBarCOMPORT.Content = "Success!";
                    StatusBarText.Content = "Connected";

                    if ((string) FileName.Content != "No file selected")
                    {
                        send_not_png.Visibility = Visibility.Hidden;
                        send_png.Visibility = Visibility.Visible;
                    }
                    IsTested = true;
                    break;
            }
        }

        private void CRCErrorBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem text = (ComboBoxItem) (sender as ComboBox).SelectedItem;
            switch (text.Content.ToString())
            {
                case "None":
                    _errorRecovery = ErrorRecovery.NONE;
                    break;
                case "CRC Error w/ Recovery":
                    _errorRecovery = ErrorRecovery.CRCRecovery;
                    break;
                case "CRC Error w/o Recovery":
                    _errorRecovery = ErrorRecovery.CRCNoRecovery;
                    break;
                case "SOH Error w/ Recovery":
                    _errorRecovery = ErrorRecovery.SOHRecovery;
                    break;

            }

        }

        public void DataReceived()
        {
            
        }


        private void PortText_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (char.IsDigit(e.Text, e.Text.Length - 1) || e.Text.Equals("."))
                e.Handled = false;
            else
            {
                e.Handled = true;
            }
        }

        private void PortText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsClient)
            {
                _client.portNumber = int.Parse(PortText.Text);
            }
            else
            {
                _server.portNumber = int.Parse(PortText.Text);
            }
        }

        private void AddrText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _client.addr = AddrText.Text;
        }
    }
}
