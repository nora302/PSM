using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using PSM.Application.Interfaces;

namespace PSM.Infrastructure.SpeechToText;

public class AzureSpeechToTextService : ISpeechToTextService
{
    private readonly string _speechKey;
    private readonly string _speechRegion;

    public AzureSpeechToTextService(IConfiguration configuration)
    {
        _speechKey = configuration["AzureSpeech:Key"]
            ?? throw new InvalidOperationException(
                "AzureSpeech:Key wurde nicht konfiguriert.");

        _speechRegion = configuration["AzureSpeech:Region"]
            ?? throw new InvalidOperationException(
                "AzureSpeech:Region wurde nicht konfiguriert.");
    }

    public async Task<string> TranskribierenAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        var speechConfig = SpeechConfig.FromSubscription(
            _speechKey,
            _speechRegion);

        speechConfig.SpeechRecognitionLanguage = "de-DE";

        using var pushStream = AudioInputStream.CreatePushStream();

        var buffer = new byte[4096];

        int bytesRead;

        while ((bytesRead = await audioStream.ReadAsync(
                   buffer,
                   cancellationToken)) > 0)
        {
            pushStream.Write(buffer, bytesRead);
        }

        pushStream.Close();

        using var audioConfig =
            AudioConfig.FromStreamInput(pushStream);

        using var recognizer =
            new SpeechRecognizer(
                speechConfig,
                audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            return result.Text;
        }

        if (result.Reason == ResultReason.NoMatch)
        {
            throw new InvalidOperationException(
                "Keine Sprache erkannt.");
        }

        if (result.Reason == ResultReason.Canceled)
        {
            var cancellation =
                CancellationDetails.FromResult(result);

            throw new InvalidOperationException(
                $"Azure Speech Fehler: {cancellation.ErrorDetails}");
        }

        throw new InvalidOperationException(
            "Die Spracherkennung ist fehlgeschlagen.");
    }
}