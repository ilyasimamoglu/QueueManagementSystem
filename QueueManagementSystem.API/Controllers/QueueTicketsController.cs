using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueueManagementSystem.Business;
using Microsoft.AspNetCore.SignalR;
using QueueManagementSystem.API.Hubs;
using System.Threading.Tasks;
using System;

namespace QueueManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueTicketsController : ControllerBase
    {
        // Controller'ın İş Katmanıyla (Business) iletişim kurması için arayüzü tanımlıyoruz
        private readonly IQueueTicketService _queueTicketService;

        // SignalR üzerinden mesaj göndermek için Hub bağlamını ekliyoruz
        private readonly IHubContext<QueueHub> _hubContext;

        // Dependency Injection ile sistemi bağlıyoruz (HubContext eklendi)
        public QueueTicketsController(IQueueTicketService queueTicketService, IHubContext<QueueHub> hubContext)
        {
            _queueTicketService = queueTicketService;
            _hubContext = hubContext;
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
        // SignalR kullanacağımız için metodu 'async Task' haline getirdik
        [HttpPost("call/{counterId}")]
        public async Task<IActionResult> CallNextTicket(int counterId)
        {
            // İş katmanına "Şu ID'ye sahip gişe sıradaki bileti çağırıyor" komutunu gönder
            var ticket = _queueTicketService.CallNextTicket(counterId);

            // Eğer bekleyen bilet yoksa null dönecektir, bu durumu kontrol edelim
            if (ticket == null)
            {
                return NotFound("Şu anda sırada bekleyen bilet bulunmamaktadır."); // 404 Hatası dön
            }

            // Bilet başarıyla çağrıldıysa, masaüstü uygulamasına (ve oradan Arduino'ya) SignalR ile mesaj fırlatıyoruz!
            await _hubContext.Clients.All.SendAsync("ReceiveNewCall", ticket.TicketNumber, counterId);

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