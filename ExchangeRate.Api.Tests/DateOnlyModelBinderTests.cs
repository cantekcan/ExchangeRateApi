using ExchangeRate.Api.ModelBinders;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Api.Tests;

public class DateOnlyModelBinderTests
{
    private readonly DateOnlyModelBinder _sut = new();

    [Fact]
    public async Task BindModelAsync_ShouldDoNothing_WhenValueProviderReturnsNone()
    {
        // Arrange
        var bindingContext = Substitute.For<ModelBindingContext>();
        var valueProvider = Substitute.For<IValueProvider>();

        bindingContext.ModelName.Returns("date");
        bindingContext.ValueProvider.Returns(valueProvider);
        valueProvider.GetValue("date").Returns(ValueProviderResult.None);

        // Act
        await _sut.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
    }

    [Fact]
    public async Task BindModelAsync_ShouldReturnSuccessWithNull_WhenValueIsNullOrEmpty()
    {
        // Arrange
        var valueProvider = Substitute.For<IValueProvider>();
        valueProvider.GetValue("date").Returns(new ValueProviderResult(new Microsoft.Extensions.Primitives.StringValues("")));

        var bindingContext = new DefaultModelBindingContext
        {
            ModelName = "date",
            ModelState = new ModelStateDictionary(),
            ValueProvider = valueProvider
        };

        // Act
        await _sut.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Theory]
    [InlineData("2026-08-28", 2026, 8, 28)]
    [InlineData("28.08.2026", 2026, 8, 28)]
    [InlineData("28/08/2026", 2026, 8, 28)]
    public async Task BindModelAsync_ShouldReturnSuccessWithDate_WhenValidFormatProvided(
        string inputDate, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var valueProvider = Substitute.For<IValueProvider>();
        valueProvider.GetValue("date").Returns(new ValueProviderResult(new Microsoft.Extensions.Primitives.StringValues(inputDate)));

        var bindingContext = new DefaultModelBindingContext
        {
            ModelName = "date",
            ModelState = new ModelStateDictionary(),
            ValueProvider = valueProvider
        };

        // Act
        await _sut.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), bindingContext.Result.Model);
        Assert.Equal(0, bindingContext.ModelState.ErrorCount);
    }

    [Fact]
    public async Task BindModelAsync_ShouldAddModelStateError_WhenInvalidFormatProvided()
    {
        // Arrange
        var valueProvider = Substitute.For<IValueProvider>();
        valueProvider.GetValue("date").Returns(new ValueProviderResult(new Microsoft.Extensions.Primitives.StringValues("invalid-date-string")));

        var bindingContext = new DefaultModelBindingContext
        {
            ModelName = "date",
            ModelState = new ModelStateDictionary(),
            ValueProvider = valueProvider
        };

        // Act
        await _sut.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.False(bindingContext.ModelState.IsValid);
        Assert.True(bindingContext.ModelState.ContainsKey("date"));
        var error = Assert.Single(bindingContext.ModelState["date"]!.Errors);
        Assert.Contains("Invalid date format", error.ErrorMessage);
    }

    [Theory]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(DateOnly?))]
    public void DateOnlyModelBinderProvider_GetBinder_ShouldReturnBinder_WhenModelTypeIsDateOnlyOrNullableDateOnly(Type modelType)
    {
        // Arrange
        var provider = new DateOnlyModelBinderProvider();
        var context = Substitute.For<ModelBinderProviderContext>();
        var metadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(modelType));

        context.Metadata.Returns(metadata);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<BinderTypeModelBinder>(result);
    }

    [Fact]
    public void DateOnlyModelBinderProvider_GetBinder_ShouldReturnNull_WhenModelTypeIsNotDateOnly()
    {
        // Arrange
        var provider = new DateOnlyModelBinderProvider();
        var context = Substitute.For<ModelBinderProviderContext>();
        var metadata = Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(string)));

        context.Metadata.Returns(metadata);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }
}
