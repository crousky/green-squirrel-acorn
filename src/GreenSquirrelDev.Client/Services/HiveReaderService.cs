using System.Net.Http.Json;
using GreenSquirrelDev.Shared.DTOs;
using GreenSquirrelDev.Shared.Models;

namespace GreenSquirrelDev.Client.Services;

public interface IHiveReaderService
{
    Task<string> GetKindleEmailAsync();
    Task<bool> UpdateKindleEmailAsync(string email);
}

public class HiveReaderService : IHiveReaderService
{
    private readonly HttpClient _httpClient;

    public HiveReaderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetKindleEmailAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<string>>("api/users/me/kindle-email");
            return response?.Success == true ? response.Data : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<bool> UpdateKindleEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/users/me/kindle-email", new UpdateKindleEmailRequest { Email = email });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                return result?.Success == true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
