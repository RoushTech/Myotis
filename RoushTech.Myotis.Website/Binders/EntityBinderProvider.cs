namespace RoushTech.Myotis.Website.Binders;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using RoushTech.Myotis.Data.Models;

public class EntityBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.ModelType.IsAssignableTo(typeof(IEntityBinding))
            ? new BinderTypeModelBinder(typeof(EntityModelBinder<>).MakeGenericType(context.Metadata.ModelType))
            : null;
}
