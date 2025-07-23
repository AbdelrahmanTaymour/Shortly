using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShortUrlController(IShortUrlsService shortUrlsService) : ControllerBase
{
    private readonly IShortUrlsService _shortUrlsService = shortUrlsService;

    /// <summary>
    /// Retrieves a list of all short URLs stored in the system.
    /// </summary>
    /// <returns>A list of <see cref="ShortUrlResponse"/> containing details of all short URLs.</returns>
    [HttpGet("getAll",Name = "GetAll")]
    [ProducesResponseType(typeof(List<ShortUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var stopwatch = Stopwatch.StartNew();
        var shortUrls = await _shortUrlsService.GetAllAsync();
        stopwatch.Stop();
        Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
        return Ok(shortUrls);
    }
    
    
    
    /// <summary>
    /// Retrieve the original URL from a short URL
    /// </summary>
    /// <param name="shortCode">The short code of the URL to retrieve.</param>
    /// <returns> <see cref="ShortUrlResponse"/> that contains the original URL and shortened URL details.</returns>
    /// <remarks>
    /// Using RedirectPermanent (301) causes the browser to cache the redirect,
    /// so future requests skip the server entirely and go straight to the target URL.
    /// This results in the access counter increasing only once per browser.
    /// </remarks>
    [HttpGet("{shortCode}", Name = "GetByShortCode")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByShortCode(string shortCode)
    {
        var shortUrlResponse = await _shortUrlsService.GetByShortCodeAsync(shortCode);
        if (shortUrlResponse is null)
        {
            return NotFound();
        }

        // Using RedirectPermanent (301) causes the browser to cache the redirect,
        // so future requests skip the server entirely and go straight to the target URL.
        // This results in the access counter increasing only once per browser.
        return RedirectPermanent(shortUrlResponse.OriginalUrl);
    }
    
    
    
    /// <summary>
    /// Creates a new shortened URL.
    /// </summary>
    /// <param name="shortUrlRequest">The Short Url DTO that contains the original url to be shortened.</param>
    /// <returns>The newly created <see cref="ShortUrlResponse"/> for the shortened URL.</returns>
    [HttpPost(Name = "CreateShortUrl")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShortUrl([FromBody] ShortUrlRequest shortUrlRequest)
    {
        var shortUrlResponse = await _shortUrlsService.CreateShortUrlAsync(shortUrlRequest);
        return CreatedAtAction(nameof(GetByShortCode), new { shortCode = shortUrlResponse.ShortCode }, shortUrlResponse);
    }
    
    
    
    /// <summary>
    /// Update an existing URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to update.</param>
    /// <param name="updatedShortUrl">The updated URL information.</param>
    /// <returns><see cref="ShortUrlResponse"/> the updated shortened URL.</returns>
    [HttpPut("shortcode/{shortCode}", Name = "UpdateShortUrlByShortCode")]
    [ProducesResponseType(typeof(ShortUrlRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateShortUrlByShortCode(string shortCode, [FromBody] ShortUrlRequest updatedShortUrl)
    {
        var shortUrlResponse = await _shortUrlsService.UpdateShortUrlAsync(shortCode, updatedShortUrl);
        if (shortUrlResponse == null)
        {
            return NotFound();
        }
        return Ok(shortUrlResponse);
    }
    
    
    
    /// <summary>
    /// Delete an existing short URL by its short code.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to delete.</param>
    /// <returns>No content (204) if successful. Otherwise, Not Found (404)</returns>
    [HttpDelete("shortcode/{shortCode}", Name = "DeleteShortUrlByShortCode")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShortUrlByShortCode(string shortCode)
    {
        bool isDeleted = await _shortUrlsService.DeleteShortUrlAsync(shortCode);
        if (!isDeleted)
        {
            return NotFound();
        }
        return NoContent();
    }
    
    
    
    /// <summary>
    /// Retrieve statistics for a short URL.
    /// </summary>
    /// <param name="shortCode">The short code of the URL to retrieve.</param>
    /// <returns><see cref="StatusShortUrlResponse"/> that contains statistics of the shortened URL.</returns>
    [HttpGet("{shortCode}/stats", Name = "GetStatistics")]
    [ProducesResponseType(typeof(StatusShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(string shortCode)
    {
        var shortUrlStatistics = await _shortUrlsService.GetStatisticsAsync(shortCode);
        if (shortUrlStatistics is null)
        {
            return NotFound();
        }
        return Ok(shortUrlStatistics);
    }
}