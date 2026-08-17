using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

        private ArduinoService _localArduinoService;
        private AppConfig _appConfig;

        private UserDto _loggedInUser;

        private DispatcherTimer _autoRefreshTimer;
        private DispatcherTimer _liveProcessTimer;
        private DispatcherTimer _clockTimer;
        private DispatcherTimer _breakTimer;

        private TimeSpan _currentProcessTime;
        private TimeSpan _currentBreakTime;

        private int _totalCalledToday = 0;
        private TimeSpan _totalProcessTimeToday = TimeSpan.Zero;
        private TimeSpan _totalBreakTimeToday = TimeSpan.Zero;

        private bool _isOnBreak = false;

        // Seçilen hizmet türünü tutacağımız değişken
        private string _selectedServiceType = null;

        public CounterWindow(UserDto loggedInUser)
        {
            InitializeComponent();
            _loggedInUser = loggedInUser;

            _appConfig = new AppConfig();
            try
            {
                string configPath = "config.json";
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    _appConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Config dosyası okunamadı, varsayılan ayarlar kullanılacak.\nHata: {ex.Message}", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            _httpClient = new HttpClient();

            string apiUrl = string.IsNullOrEmpty(_appConfig.ApiUrl) ? "http://10.40.80.167:5000/" : _appConfig.ApiUrl;
            _httpClient.BaseAddress = new Uri(apiUrl);

            if (_appConfig.IsArduinoConnected && !string.IsNullOrEmpty(_appConfig.ArduinoPort))
            {
                _localArduinoService = new ArduinoService(_appConfig.ArduinoPort);
            }

            Loaded += CounterWindow_Loaded;
        }

        private async void CounterWindow_Loaded(object sender, RoutedEventArgs e)
        {
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

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, args) =>
            {
                txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
                txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            };
            _clockTimer.Start();
        }

        // COMBOBOX SEÇİMİ DEĞİŞTİĞİNDE ÇALIŞIR
        private async void CmbServiceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbServiceType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selection = selectedItem.Content.ToString();

                if (selection == "Tüm İşlemler (Karışık)")
                {
                    _selectedServiceType = null; // API'ye null giderse hepsini getirir/çağırır
                }
                else
                {
                    _selectedServiceType = selection; // Örn: "Öğrenci İşleri" veya "Donanım Destek"
                }

                // Seçim değiştiği anda sağdaki bekleyenler listesini de güncelle
                if (_httpClient != null)
                {
                    await RefreshQueueListAsync();
                }
            }
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
            if (_isOnBreak) return;

            try
            {
                // Seçilen hizmet türünü URL'ye querystring olarak ekliyoruz
                string requestUrl = "api/QueueTickets/waiting";
                if (!string.IsNullOrEmpty(_selectedServiceType))
                {
                    requestUrl += $"?serviceType={Uri.EscapeDataString(_selectedServiceType)}";
                }

                var waitingList = await _httpClient.GetFromJsonAsync<List<QueueTicket>>(requestUrl);
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

                // Seçili hizmet türünü API'ye gönderiyoruz (Örn: /api/QueueTickets/call/1?serviceType=Öğrenci+İşleri)
                string requestUrl = $"api/QueueTickets/call/{counterIdForApi}";
                if (!string.IsNullOrEmpty(_selectedServiceType))
                {
                    requestUrl += $"?serviceType={Uri.EscapeDataString(_selectedServiceType)}";
                }

                var response = await _httpClient.PostAsync(requestUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    _currentTicket = await response.Content.ReadFromJsonAsync<QueueTicket>();

                    txtCurrentTicket.Text = _currentTicket.TicketNumber.ToString();
                    txtActiveStartTime.Text = DateTime.Now.ToString("HH:mm");

                    // Artık aktif hizmetin adını veritabanından gelen veriyle dinamik yazıyoruz!
                    string serviceName = string.IsNullOrEmpty(_currentTicket.ServiceType) ? "Standart İşlem" : _currentTicket.ServiceType;
                    txtActiveService.Text = $"Hizmet: {serviceName}";
                    txtActivePriority.Text = "Öncelik: Normal";

                    _totalCalledToday++;
                    txtTotalCalled.Text = _totalCalledToday.ToString();

                    _currentProcessTime = TimeSpan.Zero;
                    txtProcessTime.Text = "00:00";
                    _liveProcessTimer.Start();

                    BtnComplete.IsEnabled = true;

                    // Arduino'ya veri gönder
                    if (_localArduinoService != null && _appConfig.IsArduinoConnected)
                    {
                        string giseMesaji = $"GISE {counterIdForApi} - {_currentTicket.TicketNumber}";
                        _localArduinoService.SendToLcd(giseMesaji);
                    }

                    await RefreshQueueListAsync();
                }
                else
                {
                    string uyariMesaji = string.IsNullOrEmpty(_selectedServiceType)
                        ? "Sırada bekleyen müşteri yok."
                        : $"'{_selectedServiceType}' için sırada bekleyen müşteri yok.";
                    MessageBox.Show(uyariMesaji, "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void BtnRecall_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTicket != null)
            {
                if (_localArduinoService != null && _appConfig.IsArduinoConnected)
                {
                    string counterNumberStr = new String(_loggedInUser.AssignedCounter?.Where(Char.IsDigit).ToArray() ?? Array.Empty<char>());
                    int counterIdForApi = int.TryParse(counterNumberStr, out int parsedVal) ? parsedVal : 1;

                    string giseMesaji = $"GISE {counterIdForApi} - {_currentTicket.TicketNumber}";
                    _localArduinoService.SendToLcd(giseMesaji);
                }

                MessageBox.Show($"{_currentTicket.TicketNumber} numaralı bilet tekrar anons ediliyor...", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#10B981");
                }

                txtCurrentTicket.Text = "MOLADA";
                txtProcessTime.Text = "00:00";

                if (_appConfig.IsArduinoConnected)
                    _localArduinoService?.SendToLcd("    MOLADA      ");
            }
            else
            {
                _isOnBreak = false;
                _breakTimer.Stop();

                _totalBreakTimeToday = _totalBreakTimeToday.Add(_currentBreakTime);
                double minutesToAdd = Math.Round(_currentBreakTime.TotalMinutes, 2);
                _loggedInUser.TotalBreakTimeInMinutes += minutesToAdd;

                if (sender is Button btn)
                {
                    if (btn.Content is StackPanel sp)
                    {
                        (sp.Children[0] as TextBlock).Text = "☕ ";
                        (sp.Children[1] as TextBlock).Text = "Mola Al";
                    }
                    btn.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#F59E0B");
                }

                txtCurrentTicket.Text = "BEKLENİYOR";

                if (_appConfig.IsArduinoConnected)
                    _localArduinoService?.SendToLcd(" BEKLENIYOR...  ");

                await UpdateMolaSuresiAsync();
                await RefreshQueueListAsync();
            }
        }

        private async Task UpdateMolaSuresiAsync()
        {
            try
            {
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

            if (_appConfig.IsArduinoConnected)
                _localArduinoService?.SendToLcd(" BEKLENIYOR...  ");
        }

        protected override void OnClosed(EventArgs e)
        {
            _localArduinoService?.CloseConnection();
            base.OnClosed(e);
        }
    }
}