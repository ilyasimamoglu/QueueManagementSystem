using Microsoft.AspNetCore.SignalR;

namespace QueueManagementSystem.API.Hubs
{
       public class QueueHub : Hub
        {
            // Masaüstü uygulamamız (TV veya Kiosk) bu Hub üzerinden API'ye bağlanacak.
            // Şimdilik içini boş bırakıyoruz, çünkü ilk etapta sadece API'den uygulamaya mesaj fırlatacağız.
        }

}
