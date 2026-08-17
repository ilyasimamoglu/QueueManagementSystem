using System;
using System.IO.Ports;
using System.Diagnostics;

namespace QueueManagementSystem.Counter.Services // Kendi projene göre değiştir
{
    public class ArduinoService
    {
        private SerialPort _serialPort;

        public ArduinoService(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, 9600);
                _serialPort.NewLine = "\n"; // Sadece \n kullanıyoruz
                _serialPort.Open();

                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                Debug.WriteLine($"Gişe Arduino portu ({portName}) başarıyla açıldı.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gişe Arduino bağlantı hatası ({portName}): " + ex.Message);
            }
        }

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

        public void SendToLcd(string message)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // Metindeki Türkçe karakterleri temizle ve Arduino'ya yolla
                    string safeMessage = ReplaceTurkishCharacters(message);
                    _serialPort.WriteLine(safeMessage);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Gişe Arduino'ya veri gönderme hatası: " + ex.Message);
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