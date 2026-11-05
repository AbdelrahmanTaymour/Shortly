using Shortly.Domain.Abstractions;

namespace Shortly.Domain.ClickEvents;

public static class ClickEventErrors
{
    public static readonly Error InvalidShortUrl = Error.Validation("ClickEvent.InvalidShortUrl", "A click must belong to a short URL.");
    public static readonly Error SessionRequired = Error.Validation("ClickEvent.SessionRequired", "A click needs a session id.");
}
