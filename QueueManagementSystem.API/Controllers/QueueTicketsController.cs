using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueueManagementSystem.Business;
using Microsoft.AspNetCore.SignalR;
using QueueManagementSystem.API.Hubs;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // IEnumerable<T> için eklendi
using System.Linq; // Sayma işlemi (Count) için eklendi
using QueueManagementSystem.Entities; // QueueTicket sınıfına erişim için eklendi

namespace QueueManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueTicketsController : ControllerBase
    {
        private readonly IQueueTicketService _queueTicketService;
        private readonly IHubContext<QueueHub> _hubContext;

        public QueueTicketsController(IQueueTicketService queueTicketService, IHubContext<QueueHub> hubContext)
        {
            _queueTicketService = queueTicketService;
            _hubContext = hubContext;
        }

        // --- 1. KIOSK EKRANI İÇİN (Detaylı Bilet Alma ve Bekleyen Sayısı Hesaplama) ---
        [HttpPost("take")]
        public IActionResult TakeTicket([FromQuery] string serviceType = "Bireysel", [FromQuery] string priority = "Normal")
        {
            var ticket = _queueTicketService.TakeTicket(serviceType, priority);

            // Kuyruktaki mevcut biletleri getir ve QueueTicket listesine çevir
            var waitingTickets = _queueTicketService.GetWaitingTickets() as IEnumerable<QueueTicket>;
            int waitingCount = 0;

            if (waitingTickets != null)
            {
                // Sınıfta ServiceType olmadığı için, durumu 0 (Bekliyor) olan herkesi sayıyoruz.
                waitingCount = waitingTickets.Count(t => t.Status == 0);
            }

            // Hem bilet numarasını hem de hesaplanan bekleyen sayısını JSON olarak dön
            return Ok(new
            {
                TicketNumber = ((QueueTicket)ticket).TicketNumber,
                WaitingCount = waitingCount
            });
        }

        // --- 2. GİŞE EKRANI İÇİN (Sıradaki Bileti Çağır & SignalR Tetikle) ---
        [HttpPost("call/{counterId}")]
        public async Task<IActionResult> CallNextTicket(int counterId)
        {
            var ticket = _queueTicketService.CallNextTicket(counterId);

            if (ticket == null)
            {
                return NotFound("Şu anda sırada bekleyen bilet bulunmamaktadır.");
            }

            await _hubContext.Clients.All.SendAsync("ReceiveNewCall", ticket.TicketNumber, counterId);
            return Ok(ticket);
        }

        // --- 3. GİŞE EKRANI İÇİN (İşlemi Tamamla) ---
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

        // --- 4. GİŞE EKRANI İÇİN (Beklemeye Al) ---
        [HttpPost("hold/{ticketId}")]
        public IActionResult HoldTicket(int ticketId)
        {
            try
            {
                _queueTicketService.HoldTicket(ticketId);
                return Ok(new { Message = $"{ticketId} numaralı bilet beklemeye alındı." });
            }
            catch (Exception ex)
            {
                return BadRequest($"İşlem beklemeye alınırken bir hata oluştu: {ex.Message}");
            }
        }

        // --- 5. GİŞE EKRANI İÇİN (İptal Et) ---
        [HttpPost("cancel/{ticketId}")]
        public IActionResult CancelTicket(int ticketId)
        {
            try
            {
                _queueTicketService.CancelTicket(ticketId);
                return Ok(new { Message = $"{ticketId} numaralı bilet iptal edildi." });
            }
            catch (Exception ex)
            {
                return BadRequest($"İşlem iptal edilirken bir hata oluştu: {ex.Message}");
            }
        }

        // --- 6. GİŞE EKRANI İÇİN (Transfer Et) ---
        [HttpPost("transfer/{ticketId}")]
        public IActionResult TransferTicket(int ticketId)
        {
            try
            {
                _queueTicketService.TransferTicket(ticketId);
                return Ok(new { Message = $"{ticketId} numaralı bilet havuza geri gönderildi." });
            }
            catch (Exception ex)
            {
                return BadRequest($"Transfer işlemi sırasında bir hata oluştu: {ex.Message}");
            }
        }

        // --- 7. BİLGİ LİSTELEME UÇ NOKTALARI (Admin & TV Ekranı İçin) ---
        [HttpGet("waiting")]
        public IActionResult GetWaitingTickets()
        {
            var tickets = _queueTicketService.GetWaitingTickets();
            return Ok(tickets);
        }

        [HttpGet("called")]
        public IActionResult GetCalledTickets()
        {
            var tickets = _queueTicketService.GetCalledTickets();
            return Ok(tickets);
        }
    }
}