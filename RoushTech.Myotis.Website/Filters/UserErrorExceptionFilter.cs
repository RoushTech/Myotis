namespace RoushTech.Myotis.Website.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class UserErrorExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is UserErrorException ex)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status400BadRequest });
            context.ExceptionHandled = true;
        }
    }
}
