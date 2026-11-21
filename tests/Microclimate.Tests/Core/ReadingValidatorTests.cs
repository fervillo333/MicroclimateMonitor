using Microclimate.Core.Validation;
using Microclimate.Tests.Helpers;

namespace Microclimate.Tests.Core;

public class ReadingValidatorTests
{
    [Fact]
    public void Validate_ValidReading_ReturnsNoErrors()
    {
        Assert.Empty(ReadingValidator.Validate(TestData.Make()));
        Assert.True(ReadingValidator.IsValid(TestData.Make()));
    }

    [Theory]
    [InlineData(double.NaN, 45, 600)]
    [InlineData(double.PositiveInfinity, 45, 600)]
    [InlineData(200, 45, 600)]
    [InlineData(-41, 45, 600)]
    [InlineData(22, 101, 600)]
    [InlineData(22, -1, 600)]
    [InlineData(22, double.NaN, 600)]
    [InlineData(22, 45, -5)]
    [InlineData(22, 45, 20000)]
    [InlineData(22, 45, double.NaN)]
    public void Validate_InvalidValues_ReturnsErrors(double t, double h, double co2)
    {
        Assert.False(ReadingValidator.IsValid(TestData.Make(t, h, co2)));
    }

    [Theory]
    [InlineData(-40, 0, 0)]
    [InlineData(85, 100, 10000)]
    public void Validate_BoundaryValues_AreValid(double t, double h, double co2)
    {
        Assert.True(ReadingValidator.IsValid(TestData.Make(t, h, co2)));
    }

    [Fact]
    public void Validate_DefaultTimestamp_ReturnsError()
    {
        var reading = new Microclimate.Core.Models.Reading(default, 22, 45, 600);
        Assert.Contains(ReadingValidator.Validate(reading), e => e.Contains("Timestamp"));
    }

    [Fact]
    public void Validate_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ReadingValidator.Validate(null!));

    [Fact]
    public void Validate_LocalTimestamp_ReturnsError()
    {
        var reading = new Microclimate.Core.Models.Reading(
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local), 22, 45, 600);
        Assert.Contains(ReadingValidator.Validate(reading), e => e.Contains("UTC"));
    }
}
