using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueueManagementSystem.Business;
using Microsoft.AspNetCore.SignalR;
using QueueManagementSystem.API.Hubs;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using QueueManagementSystem.Entities;

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

        // --- 1. KIOSK EKRANI İÇİN (Detaylı Bilet Alma) ---
        [HttpPost("take")]
        public IActionResult TakeTicket([FromQuery] string serviceType = "Bireysel", [FromQuery] string priority = "Normal")
        {
            var ticket = _queueTicketService.TakeTicket(serviceType, priority);

            // Sadece bu hizmet türünü (serviceType) bekleyenleri çek
            var waitingTickets = _queueTicketService.GetWaitingTickets(serviceType) as IEnumerable<QueueTicket>;

            int waitingCount = 0;
            if (waitingTickets != null)
            {
                waitingCount = waitingTickets.Count(t => t.Status == 0);
            }

            return Ok(new
            {
                TicketNumber = ((QueueTicket)ticket).TicketNumber,
                ServiceType = ((QueueTicket)ticket).ServiceType, // Kiosk arayüzüne bunu da dönüyoruz
                WaitingCount = waitingCount
            });
        }

        // --- 2. GİŞE EKRANI İÇİN (Sıradaki Bileti Çağır & SignalR Tetikle) ---
        [HttpPost("call/{counterId}")]
        public async Task<IActionResult> CallNextTicket(int counterId, [FromQuery] string serviceType = null)
        {
            var ticket = _queueTicketService.CallNextTicket(counterId, serviceType);

            if (ticket == null)
            {
                return NotFound($"{serviceType ?? "Bu gişe"} için sırada bekleyen bilet bulunmamaktadır.");
            }

            // API'nin SignalR ile Kiosk'a ve Arduino donanımına seslendiği yer
            await _hubContext.Clients.All.SendAsync("ReceiveNewCall", ticket.TicketNumber, counterId);
            return Ok(ticket);
        }

        // --- 3. GİŞE EKRANI İÇİN (Tekrar Çağır / Recall) ---
        [HttpPost("recall/{ticketNumber}/{counterId}")]
        public async Task<IActionResult> RecallTicket(int ticketNumber, int counterId)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNewCall", ticketNumber, counterId);
            return Ok(new { Message = $"{ticketNumber} numaralı bilet tekrar anons edildi." });
        }

        // --- 4. GİŞE EKRANI İÇİN (İşlemi Tamamla) ---
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

        // --- 5. GİŞE EKRANI İÇİN (Beklemeye Al) ---
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

        // --- 6. GİŞE EKRANI İÇİN (İptal Et) ---
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

        // --- 7. GİŞE EKRANI İÇİN (Transfer Et) ---
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

        // --- 8. BİLGİ LİSTELEME UÇ NOKTALARI (Admin & TV Ekranı İçin) ---
        [HttpGet("waiting")]
        public IActionResult GetWaitingTickets([FromQuery] string serviceType = null)
        {
            var tickets = _queueTicketService.GetWaitingTickets(serviceType);
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