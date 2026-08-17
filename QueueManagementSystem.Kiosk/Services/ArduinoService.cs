using System;
using System.IO.Ports;
using System.Diagnostics;

namespace QueueManagementSystem.Kiosk.Services
{
    public class ArduinoService
    {
        private SerialPort _serialPort;

        public ArduinoService(string portName = "COM5")
        {
            try
            {
                _serialPort = new SerialPort(portName, 9600);
                // DÜZELTME: Arduino "readStringUntil('\n')" kullandığı için "\r\n" yerine sadece "\n" kullanıyoruz.
                _serialPort.NewLine = "\n";
                _serialPort.Open();

                // Port açıldığında içeride kalmış olabilecek eski verileri/çöpleri temizle
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                Debug.WriteLine($"{portName} portu başarıyla açıldı.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Arduino bağlantı hatası ({portName}): " + ex.Message);
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

        public void SendTicketToLcd(string counterName, string ticketNumber)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    // Arduino'nun rahatça bölebilmesi için tire ile birleştiriyoruz (Örn: "Gise 1-105")
                    string message = $"{counterName}-{ticketNumber}";

                    string safeMessage = ReplaceTurkishCharacters(message);

                    // WriteLine otomatik olarak sona _serialPort.NewLine ("\n") ekler
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