using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Printing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;

namespace QueueManagementSystem.Kiosk
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // YENİ EKLENDİ: Kiosk kendi bilgisayarındaki (localhost) 5000 portuna bağlanır
        private string _apiUrl = "http://localhost:5000/";

        private string _printerName = "POSPrinter POS80";
        private bool _isPrintingEnabled = true;
        private readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadSystemSettings();
        }

        private void LoadSystemSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);

                    if (config != null)
                    {
                        if (!string.IsNullOrWhiteSpace(config.ApiUrl))
                            _apiUrl = config.ApiUrl.EndsWith("/") ? config.ApiUrl : config.ApiUrl + "/";

                        if (!string.IsNullOrWhiteSpace(config.PrinterName))
                            _printerName = config.PrinterName;

                        _isPrintingEnabled = config.IsPrintingEnabled;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private async void BtnBireysel_Click(object sender, RoutedEventArgs e)
        {
            await ProcessTicketAsync("Öğrenci", "Normal");
        }

        private async void BtnKurumsal_Click(object sender, RoutedEventArgs e)
        {
            await ProcessTicketAsync("E-Kampüs", "Normal");
        }

        private async Task ProcessTicketAsync(string serviceType, string priority)
        {
            try
            {
                pnlSelection.IsEnabled = false;

                string requestUrl = $"{_apiUrl}api/QueueTickets/take?serviceType={serviceType}&priority={priority}";
                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TicketResponse>();
                    string realTicketNumber = result?.TicketNumber.ToString() ?? "Hata";

                    int waitingCount = result?.WaitingCount ?? 0;
                    string serviceNameHeader = $"{serviceType.ToUpper()} İŞLEMLERİ";

                    if (_isPrintingEnabled)
                    {
                        PrintTermalTicket(realTicketNumber, DateTime.Now, serviceNameHeader, waitingCount);
                    }

                    await ShowTicketOnScreenAsync(realTicketNumber);
                }
                else
                {
                    MessageBox.Show("Sistem şu an bilet veremiyor.", "Sistem Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucuyla bağlantı kurulamadı: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                pnlSelection.IsEnabled = true;
            }
        }

        private async Task ShowTicketOnScreenAsync(string ticketNumber)
        {
            txtTicketNumber.Text = ticketNumber;
            pnlSelection.Visibility = Visibility.Collapsed;
            pnlTicket.Visibility = Visibility.Visible;

            await Task.Delay(3000);

            pnlTicket.Visibility = Visibility.Collapsed;
            pnlSelection.Visibility = Visibility.Visible;
        }

        private void PrintTermalTicket(string ticketNumber, DateTime date, string serviceName, int waitingCount)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                printDialog.PrintQueue = new LocalPrintServer().GetPrintQueue(_printerName);

                FlowDocument doc = new FlowDocument();
                doc.PageWidth = 300;
                doc.PagePadding = new Thickness(0);
                doc.FontFamily = new System.Windows.Media.FontFamily("Consolas");

                Grid headerGrid = new Grid();
                headerGrid.Margin = new Thickness(0, 0, 0, 2);
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (File.Exists(logoPath))
                {
                    System.Windows.Controls.Image logoImage = new System.Windows.Controls.Image();
                    logoImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(logoPath));
                    logoImage.Width = 55;
                    logoImage.VerticalAlignment = VerticalAlignment.Top;
                    logoImage.HorizontalAlignment = HorizontalAlignment.Left;
                    logoImage.Margin = new Thickness(5, 5, 8, 0);

                    Grid.SetColumn(logoImage, 0);
                    headerGrid.Children.Add(logoImage);
                }

                StackPanel textPanel = new StackPanel();
                textPanel.VerticalAlignment = VerticalAlignment.Center;
                textPanel.HorizontalAlignment = HorizontalAlignment.Left;
                textPanel.Margin = new Thickness(0, 0, 5, 0);
                Grid.SetColumn(textPanel, 1);

                TextBlock txtUni = new TextBlock()
                {
                    Text = "ERCİYES ÜNİVERSİTESİ",
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                TextBlock txtDept = new TextBlock()
                {
                    Text = "BİLGİ İŞLEM DAİRE BŞK.",
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                textPanel.Children.Add(txtUni);
                textPanel.Children.Add(txtDept);
                headerGrid.Children.Add(textPanel);

                doc.Blocks.Add(new BlockUIContainer(headerGrid));

                Paragraph welcome = new Paragraph(new Run("HOŞGELDİNİZ"))
                {
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                doc.Blocks.Add(welcome);

                Border serviceBorder = new Border()
                {
                    Background = System.Windows.Media.Brushes.Black,
                    Padding = new Thickness(2),
                    Margin = new Thickness(10, 0, 10, 3)
                };
                TextBlock serviceText = new TextBlock()
                {
                    Text = serviceName,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center
                };
                serviceBorder.Child = serviceText;
                doc.Blocks.Add(new BlockUIContainer(serviceBorder));

                Paragraph ticketNo = new Paragraph(new Run(ticketNumber))
                {
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 60,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                doc.Blocks.Add(ticketNo);

                Paragraph waitingInfo = new Paragraph(new Run($"Bekleyen: {waitingCount} kişi"))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                doc.Blocks.Add(waitingInfo);

                Border line = new Border()
                {
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Margin = new Thickness(20, 0, 20, 2)
                };
                doc.Blocks.Add(new BlockUIContainer(line));

                var trCulture = new System.Globalization.CultureInfo("tr-TR");
                string formattedDate = date.ToString("dd.MM.yyyy dddd HH:mm", trCulture);

                Paragraph dateInfo = new Paragraph(new Run(formattedDate))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                doc.Blocks.Add(dateInfo);

                Paragraph footerNote = new Paragraph(new Run("Lütfen biletlerinizi yere atmayınız."))
                {
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Margin = new Thickness(0, 10, 0, 15)
                };
                doc.Blocks.Add(footerNote);

                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, $"Bilet - {ticketNumber}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bilet yazdırılamadı. Hata: {ex.Message}", "Yazıcı Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class AppConfig
    {
        public string ApiUrl { get; set; }
        public string PrinterName { get; set; }
        public bool IsPrintingEnabled { get; set; } = true;
    }

    public class TicketResponse
    {
        public int TicketNumber { get; set; }
        public int WaitingCount { get; set; }
    }
}