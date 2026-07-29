using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.Counter
{
    public partial class CounterWindow : Window
    {
        private readonly HttpClient _httpClient;
        private QueueTicket _currentTicket = null;
        private int _currentCounterId = 3;

        // Zamanlayıcılar
        private DispatcherTimer _autoRefreshTimer;
        private DispatcherTimer _liveProcessTimer;
        private DispatcherTimer _clockTimer; // Canlı Saat İçin
        private TimeSpan _currentProcessTime;

        // İstatistikler
        private int _totalCalledToday = 0;
        private TimeSpan _totalProcessTimeToday = TimeSpan.Zero; // Toplam işlem süresi

        public CounterWindow()
        {
            InitializeComponent();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7146/");

            Loaded += CounterWindow_Loaded;
        }

        private async void CounterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshQueueListAsync();

            // Kuyruk Otomatik Yenileme
            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(3); // 3 saniyede bir kuyruğu kontrol et
            _autoRefreshTimer.Tick += async (s, args) => await RefreshQueueListAsync();
            _autoRefreshTimer.Start();

            // İşlem Süresi Sayacı
            _liveProcessTimer = new DispatcherTimer();
            _liveProcessTimer.Interval = TimeSpan.FromSeconds(1);
            _liveProcessTimer.Tick += LiveProcessTimer_Tick;

            // Canlı Saat Sayacı
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1); // Saniyede bir tetiklenir
            _clockTimer.Tick += (s, args) =>
            {
                txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss"); // Saniyeli saat
                txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy"); // Güncel tarih
            };
            _clockTimer.Start();

            // Uygulama ilk açıldığında 1 saniye beklemeden saati hemen ekrana yazsın
            txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        // Gişe ComboBox'ı değiştiğinde aktif gişe ID'sini güncelle
        private void cmbCounterId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCounterId != null && cmbCounterId.SelectedIndex != -1)
            {
                _currentCounterId = cmbCounterId.SelectedIndex + 1; // 0. index = Gişe 1 vb.
            }
        }

        private void LiveProcessTimer_Tick(object sender, EventArgs e)
        {
            _currentProcessTime = _currentProcessTime.Add(TimeSpan.FromSeconds(1));
            txtProcessTime.Text = _currentProcessTime.ToString(@"mm\:ss");
        }

        private async Task RefreshQueueListAsync()
        {
            try
            {
                var waitingList = await _httpClient.GetFromJsonAsync<List<QueueTicket>>("api/QueueTickets/waiting");
                if (waitingList != null)
                {
                    lstWaitingCustomers.ItemsSource = waitingList;
                    txtTotalWaiting.Text = waitingList.Count.ToString();

                    // "Sonraki Müşteri" alanını güncelle
                    if (waitingList.Count > 0)
                        txtNextTicket.Text = waitingList[0].TicketNumber.ToString();
                    else
                        txtNextTicket.Text = "---";
                }
            }
            catch { }
        }

        private async void BtnCallNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/QueueTickets/call/{_currentCounterId}", null);
                if (response.IsSuccessStatusCode)
                {
                    _currentTicket = await response.Content.ReadFromJsonAsync<QueueTicket>();

                    // Aktif Bilet Ekranını Doldur
                    txtCurrentTicket.Text = _currentTicket.TicketNumber.ToString();
                    txtActiveStartTime.Text = DateTime.Now.ToString("HH:mm");

                    // Şimdilik görsel zenginlik için statik, veritabanına eklendiğinde dinamikleştirilebilir
                    txtActiveService.Text = "Hizmet: Bireysel";
                    txtActivePriority.Text = "Öncelik: Normal";

                    // İstatistikleri Güncelle
                    _totalCalledToday++;
                    txtTotalCalled.Text = _totalCalledToday.ToString();

                    // Sayacı Başlat
                    _currentProcessTime = TimeSpan.Zero;
                    txtProcessTime.Text = "00:00";
                    _liveProcessTimer.Start();

                    BtnComplete.IsEnabled = true;
                    await RefreshQueueListAsync();
                }
                else
                {
                    MessageBox.Show("Sırada bekleyen müşteri yok.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch { }
        }

        private async void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket == null) return;

            try
            {
                var response = await _httpClient.PostAsync($"api/QueueTickets/complete/{_currentTicket.Id}", null);
                if (response.IsSuccessStatusCode)
                {
                    UpdateLastAction("✅ Tamamlandı", "#10B981");
                    CalculateStatistics();
                    ResetDisplay();
                }
            }
            catch { }
        }

        private void BtnRecall_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket != null)
                MessageBox.Show($"{_currentTicket.TicketNumber} anons ediliyor...", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnHold_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket == null) return;
            try
            {
                // await _httpClient.PostAsync($"api/QueueTickets/hold/{_currentTicket.Id}", null);
                UpdateLastAction("⏸️ Beklemeye Alındı", "#F59E0B");
                CalculateStatistics();
                ResetDisplay();
            }
            catch { }
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket == null) return;

            if (MessageBox.Show("İptal etmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // await _httpClient.PostAsync($"api/QueueTickets/cancel/{_currentTicket.Id}", null);
                    UpdateLastAction("❌ İptal Edildi", "#EF4444");
                    CalculateStatistics();
                    ResetDisplay();
                }
                catch { }
            }
        }

        private void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket != null)
            {
                UpdateLastAction("↗️ Transfer Edildi", "#3B82F6");
                CalculateStatistics();
                ResetDisplay();
            }
        }

        // İstatistikleri (Toplam Süre ve Ortalama Süre) hesaplayan yardımcı metod
        private void CalculateStatistics()
        {
            // Bu işlemin süresini toplam süreye ekle
            _totalProcessTimeToday = _totalProcessTimeToday.Add(_currentProcessTime);

            // Toplam Süreyi Ekrana Yaz (Örn: 01:15:30)
            txtTotalProcessTime.Text = $"{(int)_totalProcessTimeToday.TotalHours:00}:{_totalProcessTimeToday.Minutes:00}:{_totalProcessTimeToday.Seconds:00}";

            // Ortalamayı Hesapla ve Ekrana Yaz (Örn: 02:45)
            if (_totalCalledToday > 0)
            {
                double averageSeconds = _totalProcessTimeToday.TotalSeconds / _totalCalledToday;
                TimeSpan avgTime = TimeSpan.FromSeconds(averageSeconds);
                txtAvgProcessTime.Text = $"{avgTime.Minutes:00}:{avgTime.Seconds:00}";
            }
        }

        // Son İşlem Kartını güncelleyen yardımcı metod
        private void UpdateLastAction(string statusText, string colorHex)
        {
            txtLastTicket.Text = _currentTicket.TicketNumber.ToString();
            txtLastTicketStatus.Text = statusText;

            var converter = new System.Windows.Media.BrushConverter();
            txtLastTicketStatus.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(colorHex);

            txtLastTicketEndTime.Text = $"Bitiş: {DateTime.Now.ToString("HH:mm")}";
        }

        private void ResetDisplay()
        {
            _liveProcessTimer.Stop();
            txtCurrentTicket.Text = "BEKLENİYOR";
            txtProcessTime.Text = "00:00";
            txtActiveStartTime.Text = "--:--";
            txtActiveService.Text = "Hizmet: ---";
            txtActivePriority.Text = "Öncelik: ---";
            BtnComplete.IsEnabled = false;
            _currentTicket = null;
        }
    }
}