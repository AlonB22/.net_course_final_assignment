using System.Text.Json;
using Game.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Game.Server.Pages;

public class StartGameModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TempData]
    public string? RegistrationResponseJson { get; set; }

    public Guid SessionId { get; private set; }

    public SessionRegistrationResponseDto? Registration { get; private set; }

    public void OnGet(Guid sessionId)
    {
        SessionId = sessionId;

        if (!string.IsNullOrWhiteSpace(RegistrationResponseJson))
        {
            Registration = JsonSerializer.Deserialize<SessionRegistrationResponseDto>(RegistrationResponseJson, JsonOptions);
        }
    }
}
