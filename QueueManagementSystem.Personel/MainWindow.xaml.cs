using System.Windows;

namespace QueueManagementSystem.Counter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadMockData(); // Ekran boş durmasın diye sahte verileri yüklüyoruz
        }

        private void LoadMockData()
        {
            txtTotalCalled.Text = "42";
            txtTotalWaiting.Text = "5";
            txtProcessTime.Text = "03:15";
            txtCurrentTicket.Text = "BEKLENİYOR"; // Siyah blokları çözecek yazı

            cmbCounterId.Items.Add("Gişe 1");
            cmbCounterId.Items.Add("Gişe 2");
            cmbCounterId.SelectedIndex = 0;
        }

        private void BtnCallNext_Click(object sender, RoutedEventArgs e)
        {
            txtCurrentTicket.Text = "A001";
            BtnComplete.IsEnabled = true;
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            txtCurrentTicket.Text = "BEKLENİYOR";
            BtnComplete.IsEnabled = false;
            MessageBox.Show("Müşteri işlemi başarıyla tamamlandı.", "İşlem Bitti", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}