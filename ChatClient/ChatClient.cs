using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClientApp
{
    public class ChatClientCore
    {
        private TcpClient _tcpClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private Thread _receiveThread;
        private bool _isConnected;

        public event Action<string> MessageReceived;
        public event Action<string> SystemMessage;
        public event Action<string[]> UserListUpdated;
        public event Action Connected;
        public event Action<string> ConnectionFailed;
        public event Action Disconnected;

        public bool IsConnected { get { return _isConnected; } }

        public void Connect(string ipAddress, int port, string nickname)
        {
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(ipAddress, port);

                var stream = _tcpClient.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                _writer.WriteLine("/join " + nickname);
                string response = _reader.ReadLine();

                if (response == null)
                {
                    ConnectionFailed?.Invoke("Сервер не ответил");
                    Disconnect();
                    return;
                }

                if (response.StartsWith("ERROR:"))
                {
                    ConnectionFailed?.Invoke(response.Substring(7));
                    Disconnect();
                    return;
                }

                _isConnected = true;
                Connected?.Invoke();

                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();
            }
            catch (Exception ex)
            {
                ConnectionFailed?.Invoke("Ошибка подключения: " + ex.Message);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            try { _writer?.Close(); } catch { }
            try { _reader?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
            Disconnected?.Invoke();
        }

        public void SendMessage(string message)
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(message)) return;
            try { _writer.WriteLine(message); }
            catch { HandleDisconnect(); }
        }

        private void ReceiveLoop()
        {
            try
            {
                while (_isConnected)
                {
                    string message = _reader.ReadLine();
                    if (message == null) { HandleDisconnect(); break; }
                    ProcessMessage(message);
                }
            }
            catch (IOException) { HandleDisconnect(); }
            catch (Exception ex)
            {
                SystemMessage?.Invoke("Ошибка приёма: " + ex.Message);
                HandleDisconnect();
            }
        }

        private void ProcessMessage(string message)
        {
            if (message.StartsWith("SERVER:"))
            {
                SystemMessage?.Invoke(message.Substring(7).Trim());
            }
            else if (message.StartsWith("USERLIST:"))
            {
                string list = message.Substring(9);
                string[] users = list.Length > 0 ? list.Split(',') : new string[0];
                UserListUpdated?.Invoke(users);
            }
            else
            {
                MessageReceived?.Invoke(message);
            }
        }

        private void HandleDisconnect()
        {
            if (_isConnected)
            {
                _isConnected = false;
                SystemMessage?.Invoke("Соединение с сервером потеряно");
                Disconnect();
            }
        }
    }
}