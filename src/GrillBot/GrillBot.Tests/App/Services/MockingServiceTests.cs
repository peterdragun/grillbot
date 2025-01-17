﻿using GrillBot.App.Services;
using GrillBot.Tests.Infrastructure.Common;

namespace GrillBot.Tests.App.Services;

[TestClass]
public class MockingServiceTests : ServiceTest<MockingService>
{
    protected override MockingService CreateService()
    {
        return new MockingService(TestServices.Configuration.Value, TestServices.Randomization.Value);
    }

    [TestMethod]
    public void CreateMockingString()
    {
        var result = Service.CreateMockingString("This Is lest");

        Assert.IsTrue(result.StartsWith("<a:mocking"));
        Assert.IsTrue(result.EndsWith(">"));
    }

    [TestMethod]
    public void CreateMockingString_Mocked()
    {
        const string input = "ThisIsTest";

        var result = Service.CreateMockingString(input);
        var nextResult = Service.CreateMockingString(result);

        Assert.AreNotEqual(input, result);
        Assert.AreNotEqual(input, nextResult);
        Assert.AreEqual(result, nextResult);
    }
}
