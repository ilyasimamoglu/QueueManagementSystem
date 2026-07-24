using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace QueueManagementSystem.Kiosk
{
    public partial class MainWindow : Window
    {
        // Sistemi yormamak için tek bir HttpClient nesnesi oluşturuyoruz
        private static readonly HttpClient _httpClient = new HttpClient();

        // Senin Swagger'dan aldığın API adresimiz
        private readonly string _apiUrl = "https://localhost:7146/api/QueueTickets/take";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnBireysel_Click(object sender, RoutedEventArgs e)
        {
            await ProcessTicketAsync("Bireysel");
        }

        private async void BtnKurumsal_Click(object sender, RoutedEventArgs e)
        {
            await ProcessTicketAsync("Kurumsal");
        }

        private async Task ProcessTicketAsync(string serviceType)
        {
            try
            {
                // 1. API'ye gönderilecek veriyi hazırlıyoruz. 
                var requestData = new { Type = serviceType };

                // 2. API'ye POST isteği atıyoruz
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_apiUrl, requestData);

                if (response.IsSuccessStatusCode)
                {
                    // 3. API'den dönen cevabı okuyoruz.
                    var result = await response.Content.ReadFromJsonAsync<TicketResponse>();

                    // DÜZELTME BURADA YAPILDI: int olarak gelen değeri ekrana basabilmek için .ToString() ekledik.
                    string realTicketNumber = result?.TicketNumber.ToString() ?? "Hata";

                    // 4. Gelen gerçek bilet numarasını ekranda göster
                    await ShowTicketOnScreenAsync(realTicketNumber);
                }
                else
                {
                    MessageBox.Show("Sistem şu an bilet veremiyor. Lütfen banko görevlisine danışın.", "Sistem Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucuyla bağlantı kurulamadı: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Bilet numarasını ekrana basıp 3 saniye sonra başa saran metot
        private async Task ShowTicketOnScreenAsync(string ticketNumber)
        {
            // Seçim ekranını gizle, Bilet ekranını göster
            pnlSelection.Visibility = Visibility.Collapsed;
            txtTicketNumber.Text = ticketNumber;
            pnlTicket.Visibility = Visibility.Visible;

            // Müşterinin numarasını görmesi için 3 saniye bekle
            await Task.Delay(3000);

            // Ekranı sıfırla ve yeni müşteri için başa dön
            pnlTicket.Visibility = Visibility.Collapsed;
            pnlSelection.Visibility = Visibility.Visible;
        }
    }

    // API'den dönen JSON verisini C# nesnesine dönüştürmek için kullandığımız yapı
    public class TicketResponse
    {
        // DÜZELTME BURADA YAPILDI: string yerine int olarak güncellendi.
        public int TicketNumber { get; set; }
    }
}