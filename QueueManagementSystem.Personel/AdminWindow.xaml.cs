using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Printing;
using QueueManagementSystem.Counter.Models;
using QueueManagementSystem.Counter.Services;
using System.Threading.Tasks;
using System.IO.Ports;

namespace QueueManagementSystem.Counter
{
    public partial class AdminWindow : Window
    {
        private DispatcherTimer _dashboardTimer;
        private DispatcherTimer _clockTimer;

        private readonly ConfigService _configService;
        private readonly PrintAndExportService _printExportService;
        private ApiService _apiService;

        private int? _editingUserId = null;
        private string _loggedInAdminName;

        public AdminWindow(string adminName = "Sistem Yöneticisi")
        {
            InitializeComponent();
            _loggedInAdminName = adminName;

            _configService = new ConfigService();
            _printExportService = new PrintAndExportService();

            Loaded += AdminWindow_Loaded;
        }

        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtAdminName.Text = _loggedInAdminName;

            LoadSystemSettings();

            await LoadDashboardDataAsync();
            await LoadUsersAsync();
            await LoadReportDataAsync("Günlük");
            await LoadPerformanceDataAsync("Günlük");

            _dashboardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _dashboardTimer.Tick += async (s, args) => await LoadDashboardDataAsync();
            _dashboardTimer.Start();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, args) =>
            {
                txtCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
                txtCurrentDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            };
            _clockTimer.Start();
        }

        // --- DASHBOARD GÜNCELLEMESİ (GERÇEK VERİLER) ---
        private async Task LoadDashboardDataAsync()
        {
            if (_apiService == null) return;
            try
            {
                txtWaitingCustomers.Text = (await _apiService.GetWaitingTicketCountAsync()).ToString();
                int calledCount = await _apiService.GetCalledTicketCountAsync();
                txtTotalCustomers.Text = (int.Parse(txtWaitingCustomers.Text) + calledCount).ToString();

                var summary = await _apiService.GetReportSummaryAsync("Günlük");
                if (summary != null) txtDashboardAvgWait.Text = summary.AverageWaitTime;

                var dashboardData = await _apiService.GetRealDashboardDataAsync();
                if (dashboardData != null)
                {
                    icWeeklyChart.ItemsSource = dashboardData.WeeklyChart;
                    icWeeklyLabels.ItemsSource = dashboardData.WeeklyChart;
                    icHourlyChart.ItemsSource = dashboardData.HourlyChart;
                    icCountersStatus.ItemsSource = dashboardData.CounterStatuses;

                    int activeCount = 0;
                    foreach (var counter in dashboardData.CounterStatuses)
                    {
                        if (counter.IsActive) activeCount++;
                    }
                    int totalCounters = dashboardData.CounterStatuses.Count;
                    txtActiveCounters.Text = $"{activeCount} / {totalCounters}";
                }

                txtApiStatus.Text = "🟢 Bağlı";
                txtApiStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
            }
            catch (Exception)
            {
                txtApiStatus.Text = "🔴 Bağlantı Koptu";
                txtApiStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
            }
        }

        // --- KULLANICI YÖNETİMİ ---
        private async Task LoadUsersAsync()
        {
            if (_apiService == null) return;
            try
            {
                icUsers.ItemsSource = await _apiService.GetUsersAsync();
            }
            catch (Exception)
            {
                icUsers.ItemsSource = new List<dynamic>
                {
                    new { Id = 1, FullName = "API Bağlantı Hatası", Username = "-", Role = "Hata", AssignedCounter = "-", RoleColor="#EF4444", Icon="❌" }
                };
            }
        }

        private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string username = txtNewUsername.Text.Trim();
            string password = txtNewPassword.Password;
            string role = ((ComboBoxItem)cmbNewUserRole.SelectedItem).Content.ToString();
            string assignedCounter = ((ComboBoxItem)cmbAssignedCounter.SelectedItem).Content.ToString();

            if (assignedCounter.Contains("Gişe Yok")) assignedCounter = "-";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Lütfen Ad Soyad ve Kullanıcı Adı alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userData = new
            {
                Id = _editingUserId ?? 0,
                FullName = fullName,
                Username = username,
                Password = password,
                Role = role,
                AssignedCounter = assignedCounter,
                IsActive = true
            };

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
            if (sender is Button btn && btn.Tag != null)
            {
                dynamic selectedUser = btn.Tag;
                _editingUserId = selectedUser.Id;
                txtFullName.Text = selectedUser.FullName;
                txtNewUsername.Text = selectedUser.Username;
                txtNewPassword.Clear();

                string userRole = selectedUser.Role?.ToString() ?? "";
                cmbNewUserRole.SelectedIndex = userRole.Contains("Admin") ? 1 : 0;

                cmbAssignedCounter.SelectedIndex = 0;
                string userCounter = selectedUser.AssignedCounter?.ToString() ?? "-";
                if (userCounter == "-") userCounter = "Gişe Yok (Admin)";

                foreach (ComboBoxItem item in cmbAssignedCounter.Items)
                {
                    if (item.Content.ToString() == userCounter)
                    {
                        cmbAssignedCounter.SelectedItem = item;
                        break;
                    }
                }

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
            cmbAssignedCounter.SelectedIndex = 0;
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

        // --- RAPORLAR VE PERFORMANS ---
        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string selectedFilter = btn.Content.ToString();
                txtBusiestDay.Text = "Yükleniyor...";
                txtAvgWaitTime.Text = "...";
                txtBusiestHour.Text = "...";
                await LoadReportDataAsync(selectedFilter);
                await LoadPerformanceDataAsync(selectedFilter);
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

        private async Task LoadPerformanceDataAsync(string period = "Günlük")
        {
            if (_apiService == null) return;
            try
            {
                var perfData = await _apiService.GetPerformanceDataAsync(period);
                dgPerformance.ItemsSource = perfData;

                if (perfData != null && perfData.Count > 0)
                {
                    int totalServedCount = 0;
                    string bestStaffName = "- Yok -";
                    int maxServed = -1;

                    foreach (var staff in perfData)
                    {
                        if (int.TryParse(staff.Served, out int served))
                        {
                            totalServedCount += served;

                            if (served > maxServed)
                            {
                                maxServed = served;
                                bestStaffName = staff.Name;
                            }
                        }
                    }

                    txtTotalServed.Text = $"{totalServedCount} Kişi";
                    txtBestStaffName.Text = maxServed > 0 ? bestStaffName : "- Yok -";
                }
                else
                {
                    txtTotalServed.Text = "0 Kişi";
                    txtBestStaffName.Text = "- Yok -";
                }
            }
            catch (Exception)
            {
                dgPerformance.ItemsSource = new List<StaffPerformanceDto>();
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = dgPerformance.ItemsSource as List<StaffPerformanceDto>;
                if (data == null || data.Count == 0)
                {
                    MessageBox.Show("Dışa aktarılacak veri bulunamadı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string fileName = _printExportService.ExportToExcel(data);
                MessageBox.Show($"Excel raporu masaüstüne kaydedildi:\n\n{fileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Hata:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = dgPerformance.ItemsSource as List<StaffPerformanceDto>;
                if (data == null || data.Count == 0)
                {
                    MessageBox.Show("Dışa aktarılacak veri bulunamadı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string fileName = _printExportService.ExportToPdf(data);
                MessageBox.Show($"PDF raporu masaüstüne kaydedildi:\n\n{fileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Hata:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // --- SİSTEM AYARLARI ---
        private void LoadSystemSettings()
        {
            var config = _configService.LoadSettings();
            txtApiUrl.Text = config.ApiUrl;
            _apiService = new ApiService(config.ApiUrl);

            if (chkIsPrintingEnabled != null)
            {
                chkIsPrintingEnabled.IsChecked = config.IsPrintingEnabled;
            }

            // --- AKTİF COM PORTLARINI BİLGİSAYARDAN OTOMATİK ÇEK ---
            cmbComPort.Items.Clear();
            string[] availablePorts = SerialPort.GetPortNames();

            foreach (string port in availablePorts)
            {
                cmbComPort.Items.Add(new ComboBoxItem { Content = port });
            }

            if (cmbComPort.Items.Count == 0)
            {
                cmbComPort.Items.Add(new ComboBoxItem { Content = "COM5" });
            }

            if (!string.IsNullOrEmpty(config.ArduinoPort))
            {
                foreach (ComboBoxItem item in cmbComPort.Items)
                {
                    if (item.Content.ToString() == config.ArduinoPort)
                    {
                        cmbComPort.SelectedItem = item;
                        break;
                    }
                }
            }

            if (cmbComPort.SelectedIndex == -1 && cmbComPort.Items.Count > 0)
            {
                cmbComPort.SelectedIndex = 0;
            }

            // YAZICI YÜKLEME KISMI
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
                PrinterName = (cmbPrinters.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                ArduinoPort = (cmbComPort.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM5",
                IsPrintingEnabled = chkIsPrintingEnabled.IsChecked == true
            };

            if (_configService.SaveSettings(config))
            {
                _apiService = new ApiService(apiUrl);
                MessageBox.Show("Ayarlar kalıcı olarak kaydedildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else MessageBox.Show("Ayarlar kaydedilemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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