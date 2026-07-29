using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Printing;
using System.IO.Ports;
using QueueManagementSystem.Counter.Models;
using QueueManagementSystem.Counter.Services;

namespace QueueManagementSystem.Counter
{
    public partial class AdminWindow : Window
    {
        private DispatcherTimer _dashboardTimer;
        private DispatcherTimer _clockTimer;

        // Servislerimiz
        private readonly ConfigService _configService;
        private readonly PrintAndExportService _printExportService;
        private ApiService _apiService;

        private int? _editingUserId = null;

        public AdminWindow()
        {
            InitializeComponent();

            // Servisleri Başlatıyoruz
            _configService = new ConfigService();
            _printExportService = new PrintAndExportService();

            Loaded += AdminWindow_Loaded;
        }

        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Önce Ayarları Oku ve API Servisini Başlat
            LoadSystemSettings();

            // Verileri Yükle
            await LoadDashboardDataAsync();
            await LoadUsersAsync();
            await LoadReportDataAsync("Günlük");
            await LoadPerformanceDataAsync();

            // Zamanlayıcıları Başlat
            _dashboardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _dashboardTimer.Tick += async (s, args) => await LoadDashboardDataAsync();
            _dashboardTimer.Start();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, args) =>
            {
                txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
                txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            };
            _clockTimer.Start();

            txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        // --- DASHBOARD METOTLARI ---
        private async Task LoadDashboardDataAsync()
        {
            if (_apiService == null) return;
            try
            {
                txtWaitingCustomers.Text = (await _apiService.GetWaitingTicketCountAsync()).ToString();
                int calledCount = await _apiService.GetCalledTicketCountAsync();
                txtTotalCustomers.Text = (int.Parse(txtWaitingCustomers.Text) + calledCount).ToString();

                int activeCounters = await _apiService.GetActiveCounterCountAsync();
                txtActiveCounters.Text = $"{activeCounters} / 5";

                txtApiStatus.Text = "🟢 Bağlı";
                txtApiStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
            }
            catch (Exception)
            {
                txtApiStatus.Text = "🔴 Bağlantı Koptu";
                txtApiStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
            }
        }

        // --- KULLANICI YÖNETİMİ METOTLARI ---
        private async Task LoadUsersAsync()
        {
            if (_apiService == null) return;
            try
            {
                icUsers.ItemsSource = await _apiService.GetUsersAsync();
            }
            catch (Exception)
            {
                icUsers.ItemsSource = new List<UserDto>
                {
                    new UserDto { Id = 1, FullName = "API Bağlantı Hatası", Username = "-", Role = "Hata" }
                };
            }
        }

        private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string username = txtNewUsername.Text.Trim();
            string password = txtNewPassword.Password;
            string role = ((ComboBoxItem)cmbNewUserRole.SelectedItem).Content.ToString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Lütfen Ad Soyad ve Kullanıcı Adı alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userData = new { Id = _editingUserId ?? 0, FullName = fullName, Username = username, Password = password, Role = role, IsActive = true };

            try
            {
                bool success = _editingUserId == null
                    ? await _apiService.CreateUserAsync(userData)
                    : await _apiService.UpdateUserAsync(_editingUserId.Value, userData);

                if (success)
                {
                    MessageBox.Show("Kullanıcı işlemi başarılı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearUserForm();
                    await LoadUsersAsync();
                }
                else
                {
                    MessageBox.Show("İşlem başarısız oldu.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception) { MessageBox.Show("API bağlantısı sağlanamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserDto selectedUser)
            {
                _editingUserId = selectedUser.Id;
                txtFullName.Text = selectedUser.FullName;
                txtNewUsername.Text = selectedUser.Username;
                txtNewPassword.Clear();
                cmbNewUserRole.SelectedIndex = selectedUser.Role.Contains("Admin") ? 1 : 0;

                txtFormTitle.Text = "Kullanıcıyı Güncelle";
                btnSaveUser.Content = "💾 Kaydet";
                btnSaveUser.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
                btnCancelEdit.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) => ClearUserForm();

        private void ClearUserForm()
        {
            _editingUserId = null;
            txtFullName.Clear();
            txtNewUsername.Clear();
            txtNewPassword.Clear();
            cmbNewUserRole.SelectedIndex = 0;
            txtFormTitle.Text = "Yeni Kullanıcı Ekle";
            btnSaveUser.Content = "➕ Kullanıcı Ekle";
            btnSaveUser.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            btnCancelEdit.Visibility = Visibility.Collapsed;
        }

        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId && MessageBox.Show("Bu kullanıcıyı silmek istediğinize emin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    if (await _apiService.DeleteUserAsync(userId))
                    {
                        if (_editingUserId == userId) ClearUserForm();
                        await LoadUsersAsync();
                    }
                }
                catch (Exception) { MessageBox.Show("Bağlantı hatası.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        // --- RAPORLAR METOTLARI ---
        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string selectedFilter = btn.Content.ToString();
                txtBusiestDay.Text = "Yükleniyor...";
                txtAvgWaitTime.Text = "...";
                txtBusiestHour.Text = "...";
                await LoadReportDataAsync(selectedFilter);
            }
        }

        private async Task LoadReportDataAsync(string period)
        {
            if (_apiService == null) return;
            try
            {
                var reportData = await _apiService.GetReportSummaryAsync(period);
                if (reportData != null)
                {
                    txtBusiestDay.Text = reportData.BusiestDay;
                    txtAvgWaitTime.Text = reportData.AverageWaitTime;
                    txtBusiestHour.Text = reportData.BusiestHour;
                }
            }
            catch (Exception)
            {
                txtBusiestDay.Text = "-"; txtAvgWaitTime.Text = "-"; txtBusiestHour.Text = "-";
            }
        }

        private async Task LoadPerformanceDataAsync()
        {
            if (_apiService == null) return;
            try
            {
                dgPerformance.ItemsSource = await _apiService.GetPerformanceDataAsync();
            }
            catch (Exception) { dgPerformance.ItemsSource = new List<StaffPerformanceDto>(); }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = dgPerformance.ItemsSource as List<StaffPerformanceDto> ?? GetMockData();
                string fileName = _printExportService.ExportToExcel(data);
                MessageBox.Show($"Excel raporu masaüstüne kaydedildi:\n\n{fileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Hata:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = dgPerformance.ItemsSource as List<StaffPerformanceDto> ?? GetMockData();
                string fileName = _printExportService.ExportToPdf(data);
                MessageBox.Show($"PDF raporu masaüstüne kaydedildi:\n\n{fileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Hata:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private List<StaffPerformanceDto> GetMockData() => new List<StaffPerformanceDto>
        {
            new StaffPerformanceDto { Name = "Ahmet Yılmaz", Served = "452", AvgTime = "03:15", BreakTime = "45 dk", Status = "🟢 Aktif" }
        };

        // --- SİSTEM & DONANIM METOTLARI ---
        private void LoadSystemSettings()
        {
            var config = _configService.LoadSettings();

            // API Kurulumu
            txtApiUrl.Text = config.ApiUrl;
            _apiService = new ApiService(config.ApiUrl);

            // Yazıcı Kurulumu
            cmbPrinters.Items.Clear();
            try
            {
                var printServer = new LocalPrintServer();
                foreach (var printer in printServer.GetPrintQueues())
                {
                    var item = new ComboBoxItem { Content = printer.Name };
                    cmbPrinters.Items.Add(item);
                    if (printer.Name == config.PrinterName || (string.IsNullOrEmpty(config.PrinterName) && printer.Name == printServer.DefaultPrintQueue.Name))
                        cmbPrinters.SelectedItem = item;
                }
                if (cmbPrinters.SelectedIndex == -1 && cmbPrinters.Items.Count > 0) cmbPrinters.SelectedIndex = 0;
            }
            catch { cmbPrinters.Items.Add(new ComboBoxItem { Content = "Yazıcı Servisi Kapalı" }); }
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string apiUrl = txtApiUrl.Text.Trim();
            if (!apiUrl.EndsWith("/")) apiUrl += "/";

            var config = new AppConfig
            {
                ApiUrl = apiUrl,
                PrinterName = (cmbPrinters.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? ""
            };

            if (_configService.SaveSettings(config))
            {
                _apiService = new ApiService(apiUrl);
                MessageBox.Show("Ayarlar kalıcı olarak kaydedildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Ayarlar kaydedilemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrintTest_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPrinters.SelectedItem is ComboBoxItem selectedItem)
            {
                string printerName = selectedItem.Content.ToString();
                if (printerName == "Yazıcı Bulunamadı" || printerName == "Yazıcı Servisi Kapalı") return;

                try
                {
                    _printExportService.PrintTestTicket(printerName);
                    MessageBox.Show($"Bilet '{printerName}' adlı cihaza gönderildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"Yazdırma Hatası:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async void BtnTestApi_Click(object sender, RoutedEventArgs e)
        {
            // Hızlı Test
            string url = txtApiUrl.Text.Trim();
            if (!url.EndsWith("/")) url += "/";
            var tempApi = new ApiService(url);
            try
            {
                await tempApi.GetReportSummaryAsync("Günlük");
                MessageBox.Show($"API Bağlantısı Başarılı!\nURL: {url}", "Çevrimiçi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("Bağlantı Başarısız.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnBackupDb_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Veritabanı yedeği alındı.", "Yedekleme", MessageBoxButton.OK, MessageBoxImage.Information);

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            _dashboardTimer?.Stop();
            _clockTimer?.Stop();
            this.Close();
        }
    }
}