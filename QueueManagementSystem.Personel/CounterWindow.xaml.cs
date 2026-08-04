using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QueueManagementSystem.Entities;
using QueueManagementSystem.Counter.Models;
using QueueManagementSystem.Counter.Services;

namespace QueueManagementSystem.Counter
{
    public partial class CounterWindow : Window
    {
        private readonly HttpClient _httpClient;
        private QueueTicket _currentTicket = null;
        private ArduinoService _arduinoService;

        // Giriş yapan aktif personelin nesnesi
        private UserDto _loggedInUser;

        // Zamanlayıcılar
        private DispatcherTimer _autoRefreshTimer;
        private DispatcherTimer _liveProcessTimer;
        private DispatcherTimer _clockTimer;
        private DispatcherTimer _breakTimer; // Mola süresi sayacı

        private TimeSpan _currentProcessTime;
        private TimeSpan _currentBreakTime; // O anki mola süresi

        // İstatistikler
        private int _totalCalledToday = 0;
        private TimeSpan _totalProcessTimeToday = TimeSpan.Zero;
        private TimeSpan _totalBreakTimeToday = TimeSpan.Zero; // Toplam mola süresi 

        private bool _isOnBreak = false;

        // Constructor'a giriş yapan kullanıcıyı (UserDto) zorunlu kılıyoruz
        public CounterWindow(UserDto loggedInUser)
        {
            InitializeComponent();

            _loggedInUser = loggedInUser;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://10.40.80.167:5000/");

            // --- DEĞİŞTİRİLEN KISIM: Ayarları okuyup dinamik port atıyoruz ---
            var configService = new ConfigService();
            var config = configService.LoadSettings();
            string portToUse = !string.IsNullOrEmpty(config.ArduinoPort) ? config.ArduinoPort : "COM5";

            _arduinoService = new ArduinoService(portToUse);
            // ------------------------------------------------------------------

            Loaded += CounterWindow_Loaded;
        }

        private async void CounterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // --- GİŞE TEKRARINI ÖNLEYEN DÜZENLEME ---
            string counterText = _loggedInUser.AssignedCounter?.ToString() ?? "1";
            if (counterText.Contains("Gişe", StringComparison.OrdinalIgnoreCase))
            {
                txtStaffInfo.Text = $"👤 {_loggedInUser.FullName} | {counterText}";
            }
            else
            {
                txtStaffInfo.Text = $"👤 {_loggedInUser.FullName} | Gişe {counterText}";
            }

            await RefreshQueueListAsync();

            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(3);
            _autoRefreshTimer.Tick += async (s, args) => await RefreshQueueListAsync();
            _autoRefreshTimer.Start();

            _liveProcessTimer = new DispatcherTimer();
            _liveProcessTimer.Interval = TimeSpan.FromSeconds(1);
            _liveProcessTimer.Tick += LiveProcessTimer_Tick;

            _breakTimer = new DispatcherTimer();
            _breakTimer.Interval = TimeSpan.FromSeconds(1);
            _breakTimer.Tick += BreakTimer_Tick;

            // Canlı Saat ve Tarih Sayaçları
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, args) =>
            {
                txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
                txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            };
            _clockTimer.Start();

            txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        private void LiveProcessTimer_Tick(object sender, EventArgs e)
        {
            _currentProcessTime = _currentProcessTime.Add(TimeSpan.FromSeconds(1));
            txtProcessTime.Text = _currentProcessTime.ToString(@"mm\:ss");
        }

        private void BreakTimer_Tick(object sender, EventArgs e)
        {
            _currentBreakTime = _currentBreakTime.Add(TimeSpan.FromSeconds(1));
        }

        private async Task RefreshQueueListAsync()
        {
            if (_isOnBreak) return; // Moladaysa listeyi yenilemeye gerek yok

            try
            {
                var waitingList = await _httpClient.GetFromJsonAsync<List<QueueTicket>>("api/QueueTickets/waiting");
                if (waitingList != null)
                {
                    lstWaitingCustomers.ItemsSource = waitingList;
                    txtTotalWaiting.Text = waitingList.Count.ToString();

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
            if (_isOnBreak)
            {
                MessageBox.Show("Şu anda moladasınız. İşlem almak için molayı bitirin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string counterNumberStr = new String(_loggedInUser.AssignedCounter?.Where(Char.IsDigit).ToArray() ?? Array.Empty<char>());
                int counterIdForApi = int.TryParse(counterNumberStr, out int parsedVal) ? parsedVal : 1;

                var response = await _httpClient.PostAsync($"api/QueueTickets/call/{counterIdForApi}", null);

                if (response.IsSuccessStatusCode)
                {
                    _currentTicket = await response.Content.ReadFromJsonAsync<QueueTicket>();

                    // --- ARDUINO EKRANINA VERİ GÖNDERME ---
                    string displayCounter = _loggedInUser.AssignedCounter ?? "Gişe 1";
                    if (!displayCounter.Contains("Gişe", StringComparison.OrdinalIgnoreCase))
                        displayCounter = "Gişe " + displayCounter;

                    // Port üzerinden bileti ve gişe adını ekrana yolluyoruz
                    _arduinoService.SendTicketToLcd(displayCounter, _currentTicket.TicketNumber.ToString());
                    // --------------------------------------

                    txtCurrentTicket.Text = _currentTicket.TicketNumber.ToString();
                    txtActiveStartTime.Text = DateTime.Now.ToString("HH:mm");

                    txtActiveService.Text = "Hizmet: E-Kampüs / Öğrenci";
                    txtActivePriority.Text = "Öncelik: Normal";

                    _totalCalledToday++;
                    txtTotalCalled.Text = _totalCalledToday.ToString();

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
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucuya bağlanırken bir hata oluştu: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                    await RefreshQueueListAsync();
                }
                else
                {
                    MessageBox.Show("İşlem API tarafında tamamlanamadı. (Hata Kodu: " + response.StatusCode + ")", "Sunucu Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucuya bağlanırken bir hata oluştu: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRecall_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket != null)
            {
                // Tekrar çağır butonuna basıldığında LCD'ye aynı bileti tekrar gönderiyoruz
                string displayCounter = _loggedInUser.AssignedCounter ?? "Gişe 1";
                if (!displayCounter.Contains("Gişe", StringComparison.OrdinalIgnoreCase))
                    displayCounter = "Gişe " + displayCounter;

                _arduinoService.SendTicketToLcd(displayCounter, _currentTicket.TicketNumber.ToString());

                MessageBox.Show($"{_currentTicket.TicketNumber} anons ediliyor...", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnBreak_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket != null)
            {
                MessageBox.Show("Aktif bir işleminiz var. Lütfen önce işlemi tamamlayın, beklemeye alın veya iptal edin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isOnBreak)
            {
                // Molaya Çıkış
                _isOnBreak = true;
                _currentBreakTime = TimeSpan.Zero;
                _breakTimer.Start();

                if (sender is Button btn)
                {
                    if (btn.Content is StackPanel sp)
                    {
                        (sp.Children[0] as TextBlock).Text = "▶ ";
                        (sp.Children[1] as TextBlock).Text = "Molayı Bitir";
                    }
                    btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#10B981"); // Yeşil
                }

                txtCurrentTicket.Text = "MOLADA";
                txtProcessTime.Text = "00:00";
            }
            else
            {
                // Moladan Dönüş
                _isOnBreak = false;
                _breakTimer.Stop();

                // Mola süresini ekle
                _totalBreakTimeToday = _totalBreakTimeToday.Add(_currentBreakTime);

                // Mola süresini dakika olarak hesaplayıp (virgülden sonra 2 hane) UserDto'ya ekliyoruz
                double minutesToAdd = Math.Round(_currentBreakTime.TotalMinutes, 2);
                _loggedInUser.TotalBreakTimeInMinutes += minutesToAdd;

                if (sender is Button btn)
                {
                    if (btn.Content is StackPanel sp)
                    {
                        (sp.Children[0] as TextBlock).Text = "☕ ";
                        (sp.Children[1] as TextBlock).Text = "Mola Al";
                    }
                    btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#F59E0B"); // Turuncu
                }

                txtCurrentTicket.Text = "BEKLENİYOR";

                // --- MOLA VERİSİNİ VERİTABANINA GÖNDER ---
                await UpdateMolaSuresiAsync();

                await RefreshQueueListAsync();
            }
        }

        private async Task UpdateMolaSuresiAsync()
        {
            try
            {
                // Eklenen mola süresini virgülden sonra 2 hane olacak şekilde dakika bazında (double) gönderiyoruz
                double minutesToAdd = Math.Round(_currentBreakTime.TotalMinutes, 2);

                var response = await _httpClient.PutAsJsonAsync($"api/Users/{_loggedInUser.Id}/update-break", minutesToAdd);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Mola süresi kaydedilemedi!\n\nDurum Kodu: {response.StatusCode}\nDetay: {errorDetails}", "API Uyarısı Detayı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mola API Bağlantı Hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket != null)
            {
                MessageBox.Show("Lütfen önce aktif işleminizi sonlandırın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Gişe sisteminden çıkmak istediğinize emin misiniz?", "Güvenli Çıkış", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private async void BtnHold_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket == null) return;
            try
            {
                UpdateLastAction("⏸️ Beklemeye Alındı", "#F59E0B");
                CalculateStatistics();
                ResetDisplay();
                await RefreshQueueListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket == null) return;
            if (MessageBox.Show("İptal etmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    UpdateLastAction("❌ İptal Edildi", "#EF4444");
                    CalculateStatistics();
                    ResetDisplay();
                    await RefreshQueueListAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket == null) return;
            try
            {
                UpdateLastAction("↗️ Transfer Edildi", "#3B82F6");
                CalculateStatistics();
                ResetDisplay();
                await RefreshQueueListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateStatistics()
        {
            _totalProcessTimeToday = _totalProcessTimeToday.Add(_currentProcessTime);
            txtTotalProcessTime.Text = $"{(int)_totalProcessTimeToday.TotalHours:00}:{_totalProcessTimeToday.Minutes:00}:{_totalProcessTimeToday.Seconds:00}";

            if (_totalCalledToday > 0)
            {
                double averageSeconds = _totalProcessTimeToday.TotalSeconds / _totalCalledToday;
                TimeSpan avgTime = TimeSpan.FromSeconds(averageSeconds);
                txtAvgProcessTime.Text = $"{avgTime.Minutes:00}:{avgTime.Seconds:00}";
            }
        }

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

        // --- UYGULAMA KAPANIRKEN PORTU SERBEST BIRAK ---
        protected override void OnClosed(EventArgs e)
        {
            _arduinoService?.CloseConnection();
            base.OnClosed(e);
        }
    }
}