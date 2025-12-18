using System.Net.Mail;
using FluentResults;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features;

internal interface IEmailSender
{
    /// <summary>
    /// Sends digest email with a link to the web UI, avoiding complex HTML formatting
    /// </summary>
    public Task<Result> SendDigest(DigestSummaryModel digest, CancellationToken ct);
}

internal sealed class EmailSender(ISettingsService settingsService, ILogger<EmailSender> logger)
    : IEmailSender
{
    /// <summary>
    /// Sends digest email with a link to the web UI, avoiding complex HTML formatting
    /// </summary>
    public async Task<Result> SendDigest(DigestSummaryModel digest, CancellationToken ct)
    {
        var settingsResult = await settingsService.LoadSettings(ct);
        if (settingsResult.IsFailed)
        {
            return Result.Fail(settingsResult.Errors);
        }

        var emailTo = settingsResult.Value.EmailRecipient;
        var smtpSettings = settingsResult.Value.SmtpSettings;

        try
        {
            using var client = new SmtpClient(smtpSettings.Host.HostPart, smtpSettings.Port);
            client.EnableSsl = smtpSettings.UseSsl;
            client.Credentials = new System.Net.NetworkCredential(
                smtpSettings.Username,
                smtpSettings.Password
            );

            var message = new MailMessage
            {
                From = new(smtpSettings.Username),
                Subject = $"RSSAIzer Digest - {digest.CreatedAt:d MMMM yyyy}",
                Body = CreateEmailBody(digest),
                IsBodyHtml = false,
            };
            message.To.Add(emailTo);

            await client.SendMailAsync(message, ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send digest email to {EmailTo}", emailTo);
            return Result.Fail(new Error("Email sending failed").CausedBy(ex));
        }
    }

    private static string CreateEmailBody(DigestSummaryModel digest) =>
        $"""
            Your RSSAIzer Digest for {digest.CreatedAt:d MMMM yyyy}

            {digest.Title}

            Posts: {digest.PostsCount}
            Average Importance: {digest.AverageImportance}/10

            View full digest: https://your-app-url/digest/{digest.DigestId}

            To unsubscribe or change settings, visit: https://your-app-url/settings
            """;
}
