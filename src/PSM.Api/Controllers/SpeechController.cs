using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSM.Application.Interfaces;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/speech")]
[Authorize]
public class SpeechController : ControllerBase
{
    private readonly ISpeechToTextService _speechToTextService;

    public SpeechController(
        ISpeechToTextService speechToTextService)
    {
        _speechToTextService = speechToTextService;
    }

    [HttpPost("transcribe")]
    public async Task<IActionResult> Transcribe(
        IFormFile audio,
        CancellationToken cancellationToken)
    {
        if (audio == null || audio.Length == 0)
        {
            return BadRequest(new
            {
                message = "Keine Audiodatei wurde angegeben."
            });
        }

        await using var stream = audio.OpenReadStream();

        var text = await _speechToTextService.TranskribierenAsync(
            stream,
            cancellationToken);

        return Ok(new
        {
            text
        });
    }
}