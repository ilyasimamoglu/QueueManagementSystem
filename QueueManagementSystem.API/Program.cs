using Microsoft.EntityFrameworkCore;
using QueueManagementSystem.API.Hubs;
using QueueManagementSystem.Business;
using QueueManagementSystem.DataAccess;
using QueueManagementSystem.DataAccess.Concrete;

var builder = WebApplication.CreateBuilder(args);

// API'nin tüm ağ arayüzlerinden (0.0.0.0) ve 5000 portundan dinlemesini sağlar
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// 1. DÜZELTME: Hata almamak için MigrationsAssembly ayarı eklendi
builder.Services.AddDbContext<QueueDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("QueueManagementSystem.DataAccess")
    ));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();

// 2. DÜZELTME: Bilet (QueueTicket) işlemleri için gerekli servis kayıtları eklendi
builder.Services.AddScoped<ICounterService, CounterManager>();
builder.Services.AddScoped<IQueueTicketDal, EfQueueTicketDal>();
builder.Services.AddScoped<IQueueTicketService, QueueTicketManager>();

// 3. EKLENTİ: Kullanıcı (User) işlemleri için gerekli servis kayıtları eklendi
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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<QueueHub>("/queueHub");

app.Run();