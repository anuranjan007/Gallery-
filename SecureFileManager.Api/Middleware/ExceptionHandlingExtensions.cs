using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace SecureFileManager.Api.Middleware
{
    public static class ExceptionHandlingExtensions
    {
        public static void UseApiExceptionHandling(this WebApplication app)
        {
            app.UseExceptionHandler(appErr =>
            {
                appErr.Run(async context =>
                {
                    context.Response.ContentType = "application/problem+json";
                    var exFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var ex = exFeature?.Error;
                    var problem = new ProblemDetails
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Title = "An error occurred while processing your request.",
                        Detail = app.Environment.IsDevelopment() ? ex?.Message : null
                    };
                    context.Response.StatusCode = problem.Status.Value;
                    await context.Response.WriteAsJsonAsync(problem);
                });
            });
        }
    }
}
