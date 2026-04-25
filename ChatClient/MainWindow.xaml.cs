using ChatClient;
using System;
using System.Windows;
using System.Windows.Input;

namespace ChatClientApp
{
    public partial class MainWindow : Window
    {
        private readonly ChatClientCore _client;

        public MainWindow()
        {
            InitializeComponent();
            _client = new ChatClientCore();

            _client.MessageReceived += msg => Dispatcher.Invoke(() => AddMessage(msg));
            _client.SystemMessage += msg => Dispatcher.Invoke(() => AddMessage("[СИСТЕМА] " + msg));
            _client.UserListUpdated += users => Dispatcher.Invoke(() =>
            {
                UsersList.Items.Clear();
                foreach (var user in users)
                    if (!string.IsNullOrEmpty(user))
                        UsersList.Items.Add(user);
            });
            _client.Connected += () => Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Подключен";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                ConnectBtn.IsEnabled = false;
                DisconnectBtn.IsEnabled = true;
                IpBox.IsEnabled = PortBox.IsEnabled = NickBox.IsEnabled = false;
                MessageBox.IsEnabled = SendBtn.IsEnabled = true;
                MessageBox.Focus();
            });
            _client.ConnectionFailed += err => Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(err, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка: " + err;
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            });
            _client.Disconnected += () => Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Отключен";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
                ConnectBtn.IsEnabled = true;
                DisconnectBtn.IsEnabled = false;
                IpBox.IsEnabled = PortBox.IsEnabled = NickBox.IsEnabled = true;
                MessageBox.IsEnabled = SendBtn.IsEnabled = false;
                UsersList.Items.Clear();
                MessagesList.Items.Clear();
            });
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NickBox.Text))
            {
                System.Windows.MessageBox.Show("Введите никнейм", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(PortBox.Text, out int port) || port < 1 || port > 65535)
            {
                System.Windows.MessageBox.Show("Некорректный порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _client.Connect(IpBox.Text, port, NickBox.Text.Trim());
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e) => _client.Disconnect();

        private void SendBtn_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private void SendMessage()
        {
            string text = MessageBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            _client.SendMessage(text);
            AddMessage(NickBox.Text.Trim() + ": " + text);
            MessageBox.Clear();
        }

        private void AddMessage(string message)
        {
            MessagesList.Items.Add(message);
            MessagesList.ScrollIntoView(MessagesList.Items[MessagesList.Items.Count - 1]);
        }
    }
}