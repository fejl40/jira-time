using JiraTime.Components;

namespace JiraTime.Tests;

public class MotionTests
{
    [Theory]
    [InlineData(0, "--motion-order: 0")]
    [InlineData(5, "--motion-order: 5")]
    [InlineData(-2, "--motion-order: 0")]
    public void Order_staggers_from_zero(int order, string style) => Assert.Equal(style, Motion.Order(order));
}
