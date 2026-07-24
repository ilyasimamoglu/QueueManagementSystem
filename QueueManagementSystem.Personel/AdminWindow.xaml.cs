using System.Windows;

namespace QueueManagementSystem.Counter
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Login ekranına geri dönüş
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}