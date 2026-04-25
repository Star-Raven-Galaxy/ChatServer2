using System;
using System.Windows;

namespace ChatServerApp
{
    public partial class MainWindow : Window
    {
        private readonly ChatServerCore _server;

        public MainWindow()
        {
            InitializeComponent();
            _server = new ChatServerCore();

            _server.OnLogMessage += LogHandler;
            _server.OnClientCountChanged += count => Dispatcher.Invoke(() => ClientCountText.Text = count.ToString());
            _server.OnClientConnected += nickname => Dispatcher.Invoke(() => AddLog("Подключился: " + nickname));
            _server.OnClientDisconnected += nickname => Dispatcher.Invoke(() => AddLog("Отключился: " + nickname));
            _server.OnMessageReceived += (sender, msg) =>
                Dispatcher.Invoke(() => AddLog("Сообщение от " + sender + ": " + msg));
        }

        private void LogHandler(string message) =>
            Dispatcher.Invoke(() => AddLog(message));

        private void AddLog(string text)
        {
            LogList.Items.Add(text);
            LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortBox.Text, out int port) || port < 1 || port > 65535)
            {
                System.Windows.MessageBox.Show("Некорректный порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _server.Start(port);
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
            PortBox.IsEnabled = false;
            StatusText.Text = "Сервер запущен на порту " + port;
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
            PortBox.IsEnabled = true;
            StatusText.Text = "Сервер остановлен";
            StatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e) =>
            LogList.Items.Clear();
    }
}