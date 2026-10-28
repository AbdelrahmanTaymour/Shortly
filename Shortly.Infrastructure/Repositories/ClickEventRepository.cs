using Shortly.Domain.ClickEvents;

namespace Shortly.Infrastructure.Repositories;

internal sealed class ClickEventRepository(ApplicationDbContext dbContext) : IClickEventRepository
{
    public void Add(ClickEvent clickEvent) => dbContext.Set<ClickEvent>().Add(clickEvent);
}