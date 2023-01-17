using Shouldly;

namespace tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var number1 = 1;
        number1.ShouldBe(1);
    }
}