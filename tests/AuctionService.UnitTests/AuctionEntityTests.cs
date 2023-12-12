namespace AuctionService.UnitTests;
using AuctionService.Entities;

public class AuctionEntityTests
{
    [Fact]
    // Naming = Method_Scenario_Result
    public void HasReservedPrice_ReservePriceGreaterThanZero_True()
    {
        // arrange
        var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 10 };

        // act
        var result = auction.HasReservedPrice();

        // assert
        Assert.True(result);
    }

    [Fact]
    // Naming = Method_Scenario_Result
    public void HasReservedPrice_ReservePriceGreaterThanZero_False()
    {
        // arrange
        var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 0 };

        // act
        var result = auction.HasReservedPrice();

        // assert
        Assert.False(result);
    }
}