using Microsoft.EntityFrameworkCore;
using QueueManagementSystem.API.Hubs;
using QueueManagementSystem.Business;
using QueueManagementSystem.DataAccess;
using QueueManagementSystem.DataAccess.Concrete;

var builder = WebApplication.CreateBuilder(args);

// API'nin tüm ağ arayüzlerinden (0.0.0.0) ve 5000 portundan dinlemesini sağlar
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// MigrationsAssembly ayarı
builder.Services.AddDbContext<QueueDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("QueueManagementSystem.DataAccess")
    ));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Servis kayıtları (DI)
builder.Services.AddScoped<ICounterService, CounterManager>();
builder.Services.AddScoped<IQueueTicketDal, EfQueueTicketDal>();
builder.Services.AddScoped<IQueueTicketService, QueueTicketManager>();
builder.Services.AddScoped<IUserDal, EfUserDal>();
builder.Services.AddScoped<IUserService, UserManager>();

// Swagger / OpenAPI konfigürasyonları
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Not: HTTP üzerinden port yönlendirmesi yapıldığı için HTTPS yönlendirmesi 
// yerel ağdaki istemci bağlantılarında kopmaya sebep olmaması adına devre dışı bırakılmıştır.
// app.UseHttpsRedirection();

app.UseAuthorization();

// SignalR Hub eşlemesi her zaman controller'lardan önce veya düzenli bir sırada olmalıdır
app.MapHub<QueueHub>("/queueHub");
app.MapControllers();

app.Run();