using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueueManagementSystem.Business;

namespace QueueManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueTicketsController : ControllerBase
    {
        // Controller'ın İş Katmanıyla (Business) iletişim kurması için arayüzü tanımlıyoruz
        private readonly IQueueTicketService _queueTicketService;

        // Dependency Injection ile sistemi bağlıyoruz
        public QueueTicketsController(IQueueTicketService queueTicketService)
        {
            _queueTicketService = queueTicketService;
        }

        // Kiosk cihazından yeni bilet almak için tetiklenecek uç nokta (Endpoint)
        [HttpPost("take")]
        public IActionResult TakeTicket()
        {
            // İş katmanındaki bilet alma metodunu çalıştır
            var ticket = _queueTicketService.TakeTicket();

            // Başarılı (200 OK) koduyla birlikte oluşturulan biletin bilgilerini geri dön
            return Ok(ticket);
        }

        // Gişe görevlisi butona basıp sıradaki kişiyi çağırdığında tetiklenecek uç nokta
        [HttpPost("call/{counterId}")]
        public IActionResult CallNextTicket(int counterId)
        {
            // İş katmanına "Şu ID'ye sahip gişe sıradaki bileti çağırıyor" komutunu gönder
            var ticket = _queueTicketService.CallNextTicket(counterId);

            // Eğer bekleyen bilet yoksa null dönecektir, bu durumu kontrol edelim
            if (ticket == null)
            {
                return NotFound("Şu anda sırada bekleyen bilet bulunmamaktadır."); // 404 Hatası dön
            }

            // Eğer bilet başarıyla çağrıldıysa bilgilerini geri dön
            return Ok(ticket);
        }
        [HttpPost("complete/{ticketId}")]
        public IActionResult CompleteTicket(int ticketId)
        {
            try
            {
                _queueTicketService.CompleteTicket(ticketId);
                return Ok(new { Message = $"{ticketId} numaralı biletin işlemi başarıyla tamamlandı." });
            }
            catch (Exception ex)
            {
                return BadRequest($"İşlem tamamlanırken bir hata oluştu: {ex.Message}");
            }
        }

        [HttpGet("waiting")]
        public IActionResult GetWaitingTickets()
        {
            var tickets = _queueTicketService.GetWaitingTickets();
            return Ok(tickets);
        }

        // TV Ekranı: Şu an gişelerde çağrılan/işlem gören biletler listesi
        [HttpGet("called")]
        public IActionResult GetCalledTickets()
        {
            var tickets = _queueTicketService.GetCalledTickets();
            return Ok(tickets);
        }

    }
}