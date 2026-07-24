using System.Windows;
using System.Windows.Input;

namespace QueueManagementSystem.Counter
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim().ToLower();
            string password = txtPassword.Password;

            // 1. Admin Yetkisi Kontrolü
            if (username == "admin" && password == "1234")
            {
                // Admin ekranını (AdminWindow) aç
                AdminWindow adminScreen = new AdminWindow();
                adminScreen.Show();

                // Login ekranını kapat
                this.Close();
            }
            // 2. Gişe Personeli Kontrolü (Örn: gise1, gise2 ve şifre 0000)
            else if (username.StartsWith("gise") && password == "0000")
            {
                // Ana Gişe ekranını (MainWindow) aç
                MainWindow counterScreen = new MainWindow();
                counterScreen.Show();

                // Login ekranını kapat
                this.Close();
            }
            // Hatalı Giriş
            else
            {
                MessageBox.Show("Hatalı kullanıcı adı veya şifre girdiniz.", "Giriş Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        // Pencere standart Windows çubuğu kullanmadığı için sürükleme özelliği
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // X butonuna basıldığında uygulamayı tamamen kapatır
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}