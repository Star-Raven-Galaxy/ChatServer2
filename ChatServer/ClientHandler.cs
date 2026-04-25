using System;
using System.IO;
using System.Net.Sockets;

namespace ChatServerApp
{
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly ChatServerCore _server;
        private StreamReader _reader;
        private StreamWriter _writer;
        private string _nickname;

        public ClientHandler(TcpClient tcpClient, ChatServerCore server)
        {
            _tcpClient = tcpClient;
            _server = server;
        }

        public void HandleClient()
        {
            try
            {
                var stream = _tcpClient.GetStream();
                _reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                _writer = new StreamWriter(stream, System.Text.Encoding.UTF8) { AutoFlush = true };

                _server.Log("Ожидание ника от клиента...");
                string line = _reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    _server.Log("Клиент отправил пустую строку");
                    return;
                }

                _server.Log($"Получена команда: {line}");
                if (!line.StartsWith("/join "))
                {
                    _server.Log("Неверный формат команды");
                    _writer.WriteLine("ERROR: Неверный формат подключения. Используйте /join <никнейм>");
                    return;
                }

                _nickname = line.Substring(6).Trim();
                if (string.IsNullOrEmpty(_nickname))
                {
                    _writer.WriteLine("ERROR: Никнейм не может быть пустым");
                    return;
                }

                if (_server.IsNicknameTaken(_nickname))
                {
                    _writer.WriteLine("ERROR: Никнейм уже занят");
                    return;
                }

                _writer.WriteLine("OK: Подключение успешно");
                _server.AddClient(_nickname, _writer);
                _server.SendUserList(_writer);
                _server.Log($"Клиент {_nickname} успешно авторизован");

                while (true)
                {
                    string message = _reader.ReadLine();
                    if (message == null)
                    {
                        _server.Log($"Клиент {_nickname} закрыл соединение");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(message)) continue;

                    _server.Log($"От {_nickname}: {message}");

                    if (message.StartsWith("/"))
                    {
                        HandleCommand(message);
                    }
                    else
                    {
                        if (message.Length > 500)
                            message = message.Substring(0, 500) + "...";

                        _server.RaiseMessageReceived(_nickname, message);
                        _server.Broadcast($"{_nickname}: {message}", _nickname);
                    }
                }
            }
            catch (IOException ex)
            {
                _server.Log($"Клиент {_nickname} отключился (IO): {ex.Message}");
            }
            catch (Exception ex)
            {
                _server.Log($"Ошибка обработчика клиента {_nickname}: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(_nickname))
                    _server.RemoveClient(_nickname);

                try { _reader?.Dispose(); } catch { }
                try { _writer?.Dispose(); } catch { }
                try { _tcpClient?.Close(); } catch { }

                _server.Log($"Обработчик клиента {_nickname} завершён");
            }
        }

        private void HandleCommand(string command)
        {
            if (command == "/list")
                _server.SendUserList(_writer);
        }
    }
}