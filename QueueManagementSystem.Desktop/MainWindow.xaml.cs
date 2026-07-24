using System;
using System.IO.Ports;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace QueueManagementSystem.Desktop
{
    public partial class MainWindow : Window
    {
        private HubConnection _hubConnection;
        private SerialPort _serialPort;

        public MainWindow()
        {
            InitializeComponent();
            LoadPorts();
        }

        // Bilgisayardaki COM portlarını bulup ComboBox'a doldurur
        private void LoadPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                cmbPorts.Items.Add(port);
            }
            if (cmbPorts.Items.Count > 0) cmbPorts.SelectedIndex = 0;
        }

        // Sistemi Başlat butonuna tıklandığında çalışır
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPorts.SelectedItem == null)
            {
                MessageBox.Show("Lütfen Arduino'nun bağlı olduğu bir COM portu seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetupSerialPort(cmbPorts.SelectedItem.ToString());
            await SetupSignalRAsync();
        }

        // Arduino bağlantısını kurar
        private void SetupSerialPort(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, 9600); // Baud Rate: 9600
                _serialPort.Open();
                LogAdd($"Arduino'ya {portName} üzerinden bağlanıldı.");
                btnConnect.IsEnabled = false; // Bağlandıktan sonra butonu pasif yap
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // API (SignalR) bağlantısını kurar ve dinlemeye başlar
        private async Task SetupSignalRAsync()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("https://localhost:7146/queueHub")
                    .Build();

                // DÜZELTME BURADA YAPILDI: <string, int> yerine <int, int> olarak değiştirildi.
                _hubConnection.On<int, int>("ReceiveNewCall", (ticketNumber, counterId) =>
                {
                    string message = $"Gişe {counterId} bilet çağırdı: {ticketNumber}";

                    // ÖNEMLİ: SignalR arka planda çalışır. WPF arayüzüne (ListBox) yazı yazmak için Dispatcher kullanmalıyız!
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogAdd(message);
                    });

                    // Gelen bilet numarasını Arduino'ya yolla
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        // DÜZELTME: Seri port metin kabul ettiği için int olan ticketNumber'ı ToString() ile dönüştürdük.
                        _serialPort.WriteLine(ticketNumber.ToString());
                    }
                });

                await _hubConnection.StartAsync();
                LogAdd("API (SignalR) bağlantısı başarılı. Sistem dinleniyor...");
            }
            catch (Exception ex)
            {
                LogAdd($"SignalR Hatası: Lütfen API projenizin çalıştığından emin olun. Hata: {ex.Message}");
            }
        }

        // Ekrana yeni satır ekleme yardımcı metodu
        private void LogAdd(string message)
        {
            lstLogs.Items.Add($"{DateTime.Now.ToShortTimeString()} - {message}");
            // Otomatik olarak en alt satıra kaydır
            lstLogs.ScrollIntoView(lstLogs.Items[lstLogs.Items.Count - 1]);
        }

        // Uygulama kapatıldığında portu güvenlice serbest bırak
        protected override void OnClosed(EventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();

            base.OnClosed(e);
        }
    }
}