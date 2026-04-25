using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServerApp
{
    public class ChatServerCore
    {
        private TcpListener _listener;
        private readonly Dictionary<string, StreamWriter> _clients = new Dictionary<string, StreamWriter>();
        private readonly object _lock = new object();
        private bool _isRunning;
        private Thread _listenerThread;

        // События для UI (лог, счётчик)
        public event Action<string> OnLogMessage;
        public event Action<int> OnClientCountChanged;

        // Обязательные события по п.3.2 ТЗ
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action<string, string> OnMessageReceived;   // отправитель, текст

        public bool IsRunning => _isRunning;

        public void Start(int port = 5000)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isRunning = true;
                Log($"Сервер запущен на порту {port}");

                _listenerThread = new Thread(AcceptClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();
            }
            catch (Exception ex)
            {
                Log($"Ошибка запуска сервера: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            lock (_lock)
            {
                foreach (var writer in _clients.Values)
                    try { writer.Close(); } catch { }
                _clients.Clear();
            }
            UpdateClientCount();
            Log("Сервер остановлен");
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    var tcpClient = _listener.AcceptTcpClient();
                    var handler = new ClientHandler(tcpClient, this);
                    var thread = new Thread(handler.HandleClient)
                    {
                        IsBackground = true
                    };
                    thread.Start();
                }
                catch (SocketException)
                {
                    if (_isRunning) Log("Ошибка при принятии подключения");
                }
                catch (Exception ex)
                {
                    if (_isRunning) Log($"Ошибка: {ex.Message}");
                }
            }
        }

        public void AddClient(string nickname, StreamWriter writer)
        {
            lock (_lock)
            {
                if (!_clients.ContainsKey(nickname))
                {
                    _clients.Add(nickname, writer);
                    Log($"Клиент {nickname} подключился");
                    Broadcast($"SERVER: {nickname} присоединился к чату");
                    UpdateClientCount();
                    OnClientConnected?.Invoke(nickname);
                }
            }
        }

        public void RemoveClient(string nickname)
        {
            lock (_lock)
            {
                if (_clients.Remove(nickname))
                {
                    Log($"Клиент {nickname} отключился");
                    Broadcast($"SERVER: {nickname} покинул чат");
                    UpdateClientCount();
                    OnClientDisconnected?.Invoke(nickname);
                }
            }
        }

        public void Broadcast(string message, string excludeNickname = null)
        {
            List<KeyValuePair<string, StreamWriter>> snapshot;
            lock (_lock)
            {
                snapshot = new List<KeyValuePair<string, StreamWriter>>(_clients);
            }

            foreach (var pair in snapshot)
            {
                if (pair.Key == excludeNickname) continue;
                try
                {
                    pair.Value.WriteLine(message);
                    pair.Value.Flush();
                }
                catch
                {
                    RemoveClient(pair.Key);
                }
            }
        }

        public void SendUserList(StreamWriter writer)
        {
            lock (_lock)
            {
                var list = string.Join(",", _clients.Keys);
                writer.WriteLine($"USERLIST:{list}");
                writer.Flush();
            }
        }

        public bool IsNicknameTaken(string nickname)
        {
            lock (_lock)
            {
                return _clients.ContainsKey(nickname);
            }
        }

        /// <summary>Вызывается из ClientHandler при получении текстового сообщения.</summary>
        public void RaiseMessageReceived(string sender, string message)
        {
            OnMessageReceived?.Invoke(sender, message);
        }

        public void Log(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            OnLogMessage?.Invoke(entry);
        }

        private void UpdateClientCount()
        {
            lock (_lock)
            {
                OnClientCountChanged?.Invoke(_clients.Count);
            }
        }
    }
}