using System;
using System.IO.Ports;
using System.Diagnostics;

namespace QueueManagementSystem.Counter.Services
{
    public class ArduinoService
    {
        private SerialPort _serialPort;

        public ArduinoService(string portName = "COM5")
        {
            try
            {
                _serialPort = new SerialPort(portName, 9600);
                _serialPort.NewLine = "\r\n";
                _serialPort.Open();
                Debug.WriteLine($"{portName} portu başarıyla açıldı.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Arduino bağlantı hatası ({portName}): " + ex.Message);
            }
        }

        // Türkçe karakterleri İngilizce'ye çeviren yardımcı metot
        private string ReplaceTurkishCharacters(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return text.Replace("ğ", "g").Replace("Ğ", "G")
                       .Replace("ü", "u").Replace("Ü", "U")
                       .Replace("ş", "s").Replace("Ş", "S")
                       .Replace("ı", "i").Replace("İ", "I")
                       .Replace("ö", "o").Replace("Ö", "O")
                       .Replace("ç", "c").Replace("Ç", "C");
        }

        public void SendTicketToLcd(string counterName, string ticketNumber)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // Önce metinleri birleştiriyoruz
                    string message = $"{counterName} - {ticketNumber}";

                    // Ardından Arduino'ya yollamadan hemen önce Türkçe harfleri temizliyoruz
                    string safeMessage = ReplaceTurkishCharacters(message);

                    // Temizlenmiş veriyi (Örn: "Gise 3 - A124") Arduino'ya gönder
                    _serialPort.WriteLine(safeMessage);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Arduino'ya veri gönderme hatası: " + ex.Message);
                }
            }
        }

        public void CloseConnection()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
        }
    }
}