using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
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

        private SerialPort _port;
        private Transmitter _transmitter;
        private Receiver _receiver;

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

        public bool IsTransmit = true;
        public bool IsHex = true;
        public bool IsConnected = false;
        public bool IsTested = false;

        public MainWindow()
        {

            _port = new SerialPort();
            _transmitter = new Transmitter();
            _receiver = new Receiver();

            InitializeComponent();
            InitializeComPort();
        }

        public void InitializeComPort()
        {
            foreach (var port in SerialPort.GetPortNames())
            {
                COMPortBox.Items.Add(port);
                COMPortBox.SelectedValue = port;
                _port.PortName = port;
            }
            CRCErrorBox.SelectedIndex = 0;
            _port.BaudRate = 4800;
            _port.DataBits = 8;
            _port.StopBits = StopBits.One;
            _port.Parity = Parity.None;
            _port.Handshake = Handshake.None;

            TextDisplay.AppendText("\n");
            _state = State.SOH;
            _port.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
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
                _transmitter.PacketizeFile(openFileDialog.FileName);
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
            StatusBarCOMPORT.Content = "Ready to send";
            IsTransmit = true;
            OpenItem.IsEnabled = true;
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
            StatusBarCOMPORT.Content = "Ready to receive";
            FileName.Content = "No file selected";
            IsTransmit = false;
            OpenItem.IsEnabled = false;
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
                _port.Open();
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
                _port.Close();
                Disconnected();
            }
            catch (Exception err)
            {
                StatusBarCOMPORT.Content = err.Message;
            }
        }

        private void Connected()
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

            StatusBarCOMPORT.Content = "Connected to " + _port.PortName;
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

        private void COMPortBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var text = (sender as ComboBox).SelectedItem as string;
                _port.PortName = text;
            }
            catch (Exception)
            {
                StatusBarCOMPORT.Content = "Disconnect before switching coms";
            }
        }

        private void Clear_png_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            FileName.Content = "No file selected";
            TextDisplay.Document.Blocks.Clear();
            send_not_png.Visibility = Visibility.Visible;
            send_png.Visibility = Visibility.Hidden;
            TextDisplay.AppendText("\n");
        }

        private void Send_png_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsConnected && IsTested)
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

                while (numOfErrors < 3 && iter < _transmitter.SendPackets.Count )
                {
                    var sendPacket = _transmitter.SendPackets[iter];

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
                        _port.Write(sendPacket, 0, sendPacket.Length);
                        byteToRead = _port.ReadByte();
                    }
                    catch (TimeoutException err)
                    {
                        numOfErrors++;
                        var to = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                        to.Text = "TIMEOUT\n";
                        to.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                        TextDisplay.ScrollToEnd();
                        continue;
                    }
                    catch (Exception err)
                    {
                        var pc = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                        pc.Text = "Port closed\n";
                        pc.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                        TextDisplay.ScrollToEnd();

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
                    _port.Close();
                    Disconnected();
                }
                else
                {
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
                _transmitter.SendPackets.Clear();
            }

        }

        private bool Corrupt(byte[] packet, ErrorRecovery mode)
        {
            switch (mode)
            {
                case ErrorRecovery.CRCRecovery:
                    packet[6] = (byte) ((uint) packet[6] + 1U);
                    return true;
                case ErrorRecovery.CRCNoRecovery:
                    packet[6] = (byte) ((uint) packet[6] + 1U);
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
            if (!IsConnected)
            {
                return;
            }
            StatusBarText.Content = "Connecting";
            var chkConn = new byte[1];
            chkConn[0] = (byte) 5;
            _port.Write(System.Text.Encoding.Default.GetString(chkConn));
            var response = 255;
            _port.ReadTimeout = 1000;

            try
            {
                response = _port.ReadByte();
            }
            catch (Exception err)
            {
                StatusBarCOMPORT.Content = err.Message;
                StatusBarText.Content = "Waiting";
                return;
            }

            switch (response)
            {
                case NAK:
                    StatusBarCOMPORT.Content = "Failure...";
                    StatusBarText.Content = "Waiting";
                    break;
                case ACK:
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

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if(IsConnected  && !IsTransmit) { 
            var packet = new byte[7];
            var response = new byte[1];
            while (_port.BytesToRead > 0)
            {
                var temp = (byte)_port.ReadByte();
                if (temp == SOH)
                {
                    _state = State.SOH;
                }
                else if(_state == State.SOH)
                {
                    if (temp == ENQ)
                    {
                        response[0] = ACK;
                        _port.Write(response, 0, response.Length);
                        Dispatcher.Invoke(delegate
                        {
                            connect_png.Visibility = Visibility.Visible;
                            connect_not_png.Visibility = Visibility.Hidden;
                        });
                        return;
                    }
                    else
                    {
                        packet[0] = temp;
                        _port.ReadTimeout = 1000;
                        for (var i = 1; i < SIZE_OF_PACKET; i++)
                        {
                            try
                            {
                                temp = (byte) _port.ReadByte();
                                packet[i] = temp;
                            }
                            catch (Exception err)
                            {
                                i = 7;
                            }
                        }
                        Dispatcher.Invoke(delegate
                        {
                            switch (IsHex)
                            {
                                case true:
                                    var hx = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                                    hx.Text = " Recv: " + BitConverter.ToString(packet).Replace('-', ' ') + "\n";
                                    hx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                                    TextDisplay.ScrollToEnd();
                                    break;
                                case false:
                                    var tx = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                                    tx.Text = " Recv: " + System.Text.Encoding.ASCII.GetString(packet) +"\n";
                                    tx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                                    TextDisplay.ScrollToEnd();
                                    break;
                            }
                        });

                        response[0] = NAK;
                        _port.Write(response, 0, response.Length);
                    }
                }
                switch (_state)
                {
                    case State.SOH:
                        if (temp == SOH)
                        {
                            _state = State.Data;
                            _data = new byte[7];
                            _totalRead = 0;
                            _totalLeft = SIZE_OF_PACKET;
                            _data[_totalRead] = temp;
                            _totalRead++;
                            _totalLeft--;
                        }
                        continue;
                    case State.Data:
                        _data[_totalRead] = temp;
                        _totalRead++;
                        _totalLeft--;

                        if (_totalLeft < 1)
                        {
                            if (new CRC_Class(_data).crcCalc() == 0)
                            {
                                response[0] = ACK;
                                _port.Write(response, 0, response.Length);

                                Dispatcher.Invoke(delegate { 
                                    switch (IsHex)
                                    {
                                        case true:
                                            TextDisplay.AppendText("Recv: " + BitConverter.ToString(_data).Replace('-', ' ') +
                                                                   "\n");
                                            TextDisplay.ScrollToEnd();
                                            break;
                                        case false:
                                            TextDisplay.AppendText("Recv: " + System.Text.Encoding.ASCII.GetString(_data) +
                                                                   "\n");
                                            TextDisplay.ScrollToEnd();
                                            break;
                                    }
                                });

                                _receiver.ReceivePackets.Add(_data);

                                foreach (var bytes in _data)
                                {
                                    if (bytes == EOT)
                                    {
                                        if (!string.IsNullOrEmpty(_receiver.FileToSave))
                                        {
                                            SaveFileDialog saveDialog = new SaveFileDialog();
                                            if (saveDialog.ShowDialog() == true)
                                            {
                                                _receiver.FileToSave = saveDialog.FileName;
                                                if (_receiver.ReceivePackets.Count > 0)
                                                {
                                                    StringBuilder stringBuilder = new StringBuilder();
                                                    var count = 5;
                                                    char[] charlie = new char[7];
                                                    foreach (byte[] bagets in _receiver.ReceivePackets)
                                                    {
                                                        for (var i = 1; i < bagets.Length - 1; i++)
                                                        {
                                                            charlie[i] = (char) bagets[i];
                                                            if (bagets[i] == EOT)
                                                                count = i - 1;
                                                        }
                                                        stringBuilder.Append(charlie, 1, count);
                                                    }
                                                    StreamWriter streamWriter =
                                                        new StreamWriter((Stream) File.Create(_receiver.FileToSave));
                                                    streamWriter.Write((object) stringBuilder);
                                                    streamWriter.Close();
                                                    Dispatcher.Invoke(delegate {
                                                        TextDisplay.AppendText("Successful transfer\n");
                                                        StatusBarCOMPORT.Content = "File Successfully Saved!";
                                                    });
                                                }
                                            }
                                        }
                                        _receiver.ReceivePackets.Clear();
                                    }
                                }
                            }
                            // Bad Packets go here...
                            else
                            {
                                response[0] = NAK;
                                _port.Write(response, 0, response.Length);
                                Dispatcher.Invoke(delegate {
                                    switch (IsHex)
                                    {
                                        case true:
                                            var hx = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                                            hx.Text = " Recv: " + BitConverter.ToString(_data).Replace('-', ' ') + "\n";
                                            hx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                                            TextDisplay.ScrollToEnd();
                                            break;
                                        case false:
                                            var tx = new TextRange(TextDisplay.Document.ContentEnd, TextDisplay.Document.ContentEnd);
                                            tx.Text = " Recv: " + System.Text.Encoding.ASCII.GetString(_data) + "\n";
                                            tx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkRed);
                                            TextDisplay.ScrollToEnd();
                                            break;
                                    }
                                });
                                
                            }
                            // Reset for next packet
                            _state = State.SOH;
                            continue;
                        }
                        continue;
                }
                        break;
                }
            }
        }
    }
}
