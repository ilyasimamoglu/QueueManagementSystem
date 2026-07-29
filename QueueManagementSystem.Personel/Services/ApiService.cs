using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QueueManagementSystem.Counter.Models;

namespace QueueManagementSystem.Counter.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // Servis ilk çağrıldığında Base URL'i ayarlar
        public ApiService(string baseUrl)
        {
            _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
        }

        // --- DASHBOARD (ÖZET) İSTEKLERİ ---
        public async Task<int> GetWaitingTicketCountAsync()
        {
            var list = await _httpClient.GetFromJsonAsync<List<QueueTicketDto>>("api/QueueTickets/waiting");
            return list?.Count ?? 0;
        }

        public async Task<int> GetCalledTicketCountAsync()
        {
            var list = await _httpClient.GetFromJsonAsync<List<QueueTicketDto>>("api/QueueTickets/called");
            return list?.Count ?? 0;
        }

        public async Task<int> GetActiveCounterCountAsync()
        {
            var list = await _httpClient.GetFromJsonAsync<List<CounterDto>>("api/Counters");
            return list?.Count ?? 0;
        }

        // --- KULLANICI İSTEKLERİ ---
        public async Task<List<UserDto>> GetUsersAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<UserDto>>("api/Users");
        }

        public async Task<bool> CreateUserAsync(object userData)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Users", userData);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(int userId, object userData)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Users/{userId}", userData);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var response = await _httpClient.DeleteAsync($"api/Users/{userId}");
            return response.IsSuccessStatusCode;
        }

        // --- RAPOR İSTEKLERİ ---
        public async Task<ReportSummaryDto> GetReportSummaryAsync(string period)
        {
            return await _httpClient.GetFromJsonAsync<ReportSummaryDto>($"api/Reports/summary?period={period}");
        }

        public async Task<List<StaffPerformanceDto>> GetPerformanceDataAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<StaffPerformanceDto>>("api/Reports/performance");
        }
    }
}