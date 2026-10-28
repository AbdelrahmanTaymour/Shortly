using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using MaxMind.Db;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shortly.Infrastructure.ClickTracking;

internal sealed class GeoDatabaseUpdaterService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeoLite2Options> options,
    MaxMindGeoLocationService geoLocationService,
    ILogger<GeoDatabaseUpdaterService> logger)
    : BackgroundService
{
    private const int MaxRetryDelayMinutes = 60;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromDays(
            options.Value.UpdateIntervalDays);

        var retryDelay = TimeSpan.FromMinutes(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            var updated = await TryUpdateIfNeededAsync(stoppingToken);

            var delay = updated ? interval : retryDelay;

            if (updated)
            {
                retryDelay = TimeSpan.FromMinutes(1);
            }
            else
            {
                retryDelay = TimeSpan.FromMinutes(
                    Math.Min(retryDelay.TotalMinutes * 2,
                        MaxRetryDelayMinutes));
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<bool> TryUpdateIfNeededAsync(
        CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(options.Value.DatabasePath);
        var interval = TimeSpan.FromDays(
            options.Value.UpdateIntervalDays);

        var stale = !File.Exists(path)
            || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) >= interval;

        if (!stale)
        {
            return true;
        }

        try
        {
            await DownloadAndReplaceAsync(cancellationToken);

            // Reload only after the new database has been validated
            // and moved into its destination.
            geoLocationService.ReloadDatabase();

            logger.LogInformation(
                "GeoLite2 database updated successfully.");

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "GeoLite2 database update failed. The current reader will be retained.");

            return false;
        }
    }

    private async Task DownloadAndReplaceAsync(
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var destinationPath = Path.GetFullPath(settings.DatabasePath);
        var directory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException(
                "The GeoLite2 database path has no parent directory.");

        Directory.CreateDirectory(directory);

        // Keep temporary files on the same filesystem as the destination.
        var tempPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await DownloadDatabaseAsync(tempPath, cancellationToken);

            // Validate the downloaded database before replacing the
            // existing one. Opening the reader checks that it is readable.
            using (var validationReader = new DatabaseReader(tempPath, FileAccessMode.Memory))

            {
                // Successful construction validates the database format.
            }

            cancellationToken.ThrowIfCancellationRequested();

            // On the same filesystem, replacement is atomic on normal
            // local filesystems. Existing readers remain usable.
            File.Move(tempPath, destinationPath, overwrite: true);
        }
        finally
        {
            // Do not leave incomplete downloads behind.
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private async Task DownloadDatabaseAsync(
        string tempPath,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var client = httpClientFactory.CreateClient("MaxMind");

        var url =
            $"https://download.maxmind.com/geoip/databases/" +
            $"{Uri.EscapeDataString(settings.EditionId)}/download?suffix=tar.gz";

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{settings.AccountId}:{settings.LicenseKey}"));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        await using var gzipStream = new GZipStream(
            responseStream,
            CompressionMode.Decompress);

        using var tarReader = new TarReader(gzipStream);

        var foundDatabase = false;

        while (await tarReader.GetNextEntryAsync(
                   cancellationToken: cancellationToken) is { } entry)
        {
            // MaxMind archives contain a versioned directory with the
            // database file. Only extract the .mmdb payload.
            if (!entry.Name.EndsWith(
                    ".mmdb",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (foundDatabase)
            {
                throw new InvalidDataException(
                    "The MaxMind archive contains multiple database files.");
            }

            if (entry.DataStream is null)
            {
                throw new InvalidDataException(
                    "The MaxMind database archive entry has no data stream.");
            }

            await using var fileStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous
                    | FileOptions.SequentialScan);

            await entry.DataStream.CopyToAsync(
                fileStream,
                cancellationToken);

            await fileStream.FlushAsync(cancellationToken);

            foundDatabase = true;
        }

        if (!foundDatabase)
        {
            throw new InvalidDataException(
                "The MaxMind archive did not contain an .mmdb file.");
        }
    }
}