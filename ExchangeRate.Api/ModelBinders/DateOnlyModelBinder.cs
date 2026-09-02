using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace ExchangeRate.Api.ModelBinders;

public class DateOnlyModelBinder : IModelBinder
{
    private static readonly string[] Formats = { "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy" };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Invalid date format. Supported formats are: {string.Join(", ", Formats)}");
        }

        return Task.CompletedTask;
    }
}
