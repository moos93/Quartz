using Quartz;
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UserMvc.Models;
using Microsoft.Extensions.Options;
using UserMvc.Jobs;

public class EmailJob : IJob
{
    private readonly ILogger<EmailJob> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmtpOptions _smtpOptions;

    public EmailJob(ILogger<EmailJob> logger, IHttpClientFactory httpClientFactory, IOptions<SmtpOptions> smtpOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _smtpOptions = smtpOptions.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting EmailJob execution...");

            // Call the API to fetch users
            var users = await FetchUsersFromApi();

            if (users != null && users.Count > 0)
            {
                foreach (var user in users)
                {
                    // Check if the user has already received the email (assumed logic here)
                    if (!user.HasReceivedEmail)
                    {
                        await SendEmail(user.Email);

                        // Update the API to mark the user as having received the email
                        await UpdateUserEmailStatus(user);
                    }
                }
                JobStatus.IsJobCompleted = true; // Mark the job as completed

            }

            _logger.LogInformation("EmailJob completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing EmailJob.");
            throw new JobExecutionException(ex, false) { RefireImmediately = false };
        }
    }

    private async Task<List<User>> FetchUsersFromApi()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync("https://localhost:7013/api/User");

        if (response.IsSuccessStatusCode)
        {
            var users = await response.Content.ReadAsAsync<List<User>>();
            return users;
        }

        _logger.LogWarning("Failed to fetch users from API.");
        return new List<User>();
    }

    private async Task SendEmail(string emailAddress)
    {
        // Configure SMTP settings from your appsettings
        using (var smtpClient = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port))
        {
            smtpClient.EnableSsl = true; // Enable SSL for secure connection
            smtpClient.Credentials = new System.Net.NetworkCredential(_smtpOptions.Username, _smtpOptions.Password); // Provide credentials

            var mailMessage = new MailMessage(_smtpOptions.FromEmail, emailAddress)
            {
                Subject = "Test Email",
                Body = "This is a test email sent by Quartz job."
            };

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {emailAddress}");
            }
            catch (SmtpException ex)
            {
                _logger.LogError($"SMTP Exception: {ex.Message}");
                throw;
            }
        }
    }


    private async Task UpdateUserEmailStatus(User user)
    {
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.PutAsJsonAsync($"https://localhost:7013/api/User/{user.Id}/mark-email-sent", client);

            if (response.IsSuccessStatusCode)
            {
                user.HasReceivedEmail = true;
                _logger.LogInformation($"Marked email as sent for user {user.Id}, and updated HasReceivedEmail to true in the database.");
            }
            else
            {
                _logger.LogWarning($"Failed to update email status for user {user.Id}");
            }
        }
    }
}
