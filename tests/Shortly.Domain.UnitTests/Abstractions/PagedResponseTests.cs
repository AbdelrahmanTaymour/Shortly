using Shortly.Domain.Abstractions;

namespace Shortly.Domain.UnitTests.Abstractions;

public class PagedResponseTests
{
    [Fact]
    public void PagedResponse_Should_CarryTheItemsAndPagingInformation()
    {
        // Act
        var page = new PagedResponse<int>
        {
            Items = [1, 2, 3], Page = 2, PageSize = 3, TotalCount = 8, TotalPages = 3
        };

        // Assert
        page.Items.Should().Equal(1, 2, 3);
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(3);
        page.TotalCount.Should().Be(8);
        page.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PagedResponses_Should_BeEqual_ByReferenceToTheSameItems()
    {
        // Arrange
        IReadOnlyList<int> items = [1, 2];
        var first = new PagedResponse<int> { Items = items, Page = 1, PageSize = 2, TotalCount = 2, TotalPages = 1 };
        var second = first with { };

        // Assert
        second.Should().Be(first);
    }

    [Fact]
    public void BulkOperationResponse_Should_ExposeTheCounters()
    {
        // Act
        var response = new BulkOperationResponse(10, 7, 3);

        // Assert
        response.TotalProcessed.Should().Be(10);
        response.SuccessCount.Should().Be(7);
        response.FailureCount.Should().Be(3);
    }
}
