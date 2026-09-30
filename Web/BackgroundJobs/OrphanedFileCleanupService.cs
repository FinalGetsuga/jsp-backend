using Domain.Models;
using Repository.Interface;
using Service.Interface;

namespace Web.BackgroundJobs;

public class OrphanedFileCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrphanedFileCleanupService> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan MinimumAge = TimeSpan.FromHours(1);

    public OrphanedFileCleanupService(IServiceScopeFactory scopeFactory, ILogger<OrphanedFileCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOrphanedFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orphaned file cleanup run failed");
            }
            
            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task CleanupOrphanedFilesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IRepository<ApplicationDocument>>();
        var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        
        _logger.LogInformation("Starting orphaned file cleanup...");

        var dbKeys = await documentRepository.GetAllAsync(
            selector: x => x.StorageKey,
            asNoTracking: true);
        var dbKeySet = dbKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var storageObjects = await fileStorageService.ListAllObjectsAsync("applications/", ct);
        var cutoff = DateTime.UtcNow.Subtract(MinimumAge);

        var orphanedKeys = storageObjects
            .Where(x => x.LastModified < cutoff && !dbKeySet.Contains(x.Key))
            .Select(x => x.Key)
            .ToList();

        foreach (var key in orphanedKeys)
        {
            try
            {
                await fileStorageService.DeleteAsync(key, ct);
                _logger.LogInformation("Deleted orphaned file: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete orphaned file: {Key}", key);
            }
        }
        
        _logger.LogInformation("Orphaned file cleanup finished. {Count} orphaned files removed.", orphanedKeys.Count);
    }
}