namespace PSM.Application.Interfaces;

public interface ISpeechToTextService
{
    Task<string> TranskribierenAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default);
}