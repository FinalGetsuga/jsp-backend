using Domain.Enums;
using Domain.Identity;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Repository.Interface;
using Service.Interface;

namespace Web.BackgroundJobs;

public class RenewalReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RenewalReminderService> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    public RenewalReminderService(IServiceScopeFactory scopeFactory, ILogger<RenewalReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var inReminderWindow = now.Month is 1 or 2 or 3;

            if (inReminderWindow)
            {
                try
                {
                    await SendRemindersAsync(now.Year, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Renewal reminder run failed");
                }
            }
            
            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task SendRemindersAsync(int currentYear, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IRepository<Application>>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var approvedUserIds = (await applicationRepository.GetAllAsync(
            selector: x => x.UserId,
            predicate: x => x.RenewalYear == currentYear && x.Status == ApplicationStatus.Approved,
            asNoTracking: true)).ToHashSet();

        var students = await userManager.GetUsersInRoleAsync("Student");

        var needingReminder = students
            .Where(x => !approvedUserIds.Contains(x.Id) && x.LastRenewalReminderYear != currentYear)
            .ToList();
        
        _logger.LogInformation("Sending {Count} renewal reminders for {Year}", needingReminder.Count, currentYear);

        foreach (var student in needingReminder)
        {
            try
            {
                await emailSender.SendEmailAsync(
                    student.Email!,
                    $"Renew your student bus card for {currentYear}",
                    $"<p>Hi {student.FirstName},</p><p>Don't forget to submit your bus card renewal application for {currentYear}.</p>",
                    ct);

                student.LastRenewalReminderYear = currentYear;
                await userManager.UpdateAsync(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send renewal reminder to {UserId}", student.Id);
            }
        }
    }
}