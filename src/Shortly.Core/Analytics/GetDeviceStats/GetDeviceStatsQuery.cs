using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Analytics.GetDeviceStats;

public sealed record GetDeviceStatsQuery(
    long? ShortUrlId, DateTime? From, DateTime? To) : IQuery<DeviceStatsResponse>;