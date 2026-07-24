using Microsoft.EntityFrameworkCore;
using QueueManagementSystem.API.Hubs;
using QueueManagementSystem.Business;
using QueueManagementSystem.DataAccess;
using QueueManagementSystem.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. DÜZELTME: Hata almamak için MigrationsAssembly ayarı eklendi
builder.Services.AddDbContext<QueueDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("QueueManagementSystem.DataAccess")
    ));

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSignalR(); // SignalR ekleniyor, böylece gerçek zamanlı iletişim mümkün olacak

// 2. DÜZELTME: Bilet (QueueTicket) işlemleri için gerekli servis kayıtları eklendi
builder.Services.AddScoped<ICounterService, CounterManager>();
builder.Services.AddScoped<IQueueTicketDal, EfQueueTicketDal>();
builder.Services.AddScoped<IQueueTicketService, QueueTicketManager>();

// Swagger / OpenAPI konfigürasyonları (.NET 8 standartları)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapHub<QueueHub>("/queueHub"); // SignalR Hub'ı "/queueHub" endpoint'ine bağlanacak şekilde ayarlanıyor 


app.Run();