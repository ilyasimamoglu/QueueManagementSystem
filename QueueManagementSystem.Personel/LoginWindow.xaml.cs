using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using QueueManagementSystem.Counter.Models;

namespace QueueManagementSystem.Counter
{
    public partial class LoginWindow : Window
    {
        private readonly HttpClient _httpClient;

        public LoginWindow()
        {
            InitializeComponent();

            // API bağlantısı için ayarlar (Portunu kontrol etmeyi unutma)
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7146/");
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifre giriniz.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var loginData = new
            {
                Username = username,
                Password = password
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Users/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var userJson = await response.Content.ReadAsStringAsync();
                    var loggedInUser = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // GİRİŞ YAPAN KULLANICIYI HAFIZAYA ALIYORUZ
                    // Bu sayede CounterWindow açıldığında hangi gişe personelinin işlem yaptığını bileceğiz
                    Application.Current.Properties["LoggedInUserId"] = loggedInUser.Id;
                    Application.Current.Properties["LoggedInUserName"] = loggedInUser.FullName ?? loggedInUser.Username;

                    if (loggedInUser.Role != null && loggedInUser.Role.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        AdminWindow adminScreen = new AdminWindow();
                        adminScreen.Show();
                    }
                    else
                    {
                        CounterWindow counterScreen = new CounterWindow();
                        counterScreen.Show();
                    }

                    this.Close();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show("Hatalı kullanıcı adı veya şifre girdiniz.", "Giriş Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
                else
                {
                    MessageBox.Show($"Sunucudan beklenmeyen bir yanıt geldi: {response.StatusCode}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("API sunucusuna ulaşılamadı. Arka planda API projesinin çalıştığından emin olun.\nDetay: " + ex.Message, "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}