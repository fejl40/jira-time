using JiraTime.Components;

namespace JiraTime.Tests;

public class MotionTests
{
    [Theory]
    [InlineData(0, "--motion-order: 0")]
    [InlineData(5, "--motion-order: 5")]
    [InlineData(-2, "--motion-order: 0")]
    public void Order_staggers_from_zero(int order, string style) => Assert.Equal(style, Motion.Order(order));

    [Theory]
    [InlineData(1, "motion-slide-next")]
    [InlineData(31, "motion-slide-next")]
    [InlineData(-1, "motion-slide-previous")]
    [InlineData(0, "")]
    public void Slide_follows_the_direction(int direction, string css) => Assert.Equal(css, Motion.Slide(direction));
}
