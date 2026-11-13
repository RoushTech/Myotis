namespace RoushTech.Myotis.Website.Binders;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using RoushTech.Myotis.Data;

public class EntityModelBinder<T>(MyotisDbContext db) : IModelBinder where T : class
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;
        var value = bindingContext.ValueProvider.GetValue(modelName);
        if (value == ValueProviderResult.None)
        {
            return;
        }

        bindingContext.ModelState.SetModelValue(modelName, value);
        if (!Guid.TryParse(value.FirstValue, out var id) || id == Guid.Empty)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return;
        }

        var model = await db.Set<T>().FindAsync([id], bindingContext.HttpContext.RequestAborted);
        bindingContext.Result = ModelBindingResult.Success(model);
        if (model is not null)
        {
            bindingContext.ValidationState[model] = new() { Key = modelName, SuppressValidation = true };
        }
    }
}
