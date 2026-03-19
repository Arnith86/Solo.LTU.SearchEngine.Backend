using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Infrastructure;

/// <summary>
/// Handles domain validation using settings defined in <see cref="CrawlerSettings"/>.
/// This validator ensures that the crawler stays within authorized domains and their sub-domains.
/// </summary>
public class DomainValidator : IDomainValidator
{
    private readonly CrawlerSettings _settings;

    /// <summary>Initializes a new instance of the <see cref="DomainValidator"/> class.</summary>
    /// <param name="settings">The crawler configuration containing the authorized domains (WhiteList).</param>
    public DomainValidator(CrawlerSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc/>
    public bool IsWhitelisted(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        string host = uri.Host;

        return _settings.WhiteList.Any(allowedDomain => 
            host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||  // Checks exact match
            host.EndsWith("."+allowedDomain, StringComparison.OrdinalIgnoreCase) // Checks that if sub-domain "*.ltu.se"
        );
    }
}

    
