using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.IncludeFields = true)
    .ConfigureApiBehaviorOptions(o =>
    {
        // when validation fails (or the json is broken), return our error format instead of the default one
        // details = each field name with its error messages
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var details = ctx.ModelState
                .Where(field => field.Value!.Errors.Count > 0)
                .ToDictionary(
                    field => field.Key,
                    field => field.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                error = "VALIDATION_ERROR",
                message = "One or more fields are invalid.",
                details
            });
        };
    });
builder.Services.AddSingleton<IAccountService, AccountService>();

var app = builder.Build();


app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        var (status, code) = ex switch
        {
            NotFoundException => (404, "NOT_FOUND"),
            ConflictException => (409, "CONFLICT"),
            BusinessRuleException b => (422, b.Code),
            _ => (500, "INTERNAL_ERROR")
        };

        // the real error is only written to the server log, the client gets a generic message
        if (status == 500)
            app.Logger.LogError(ex, "Unexpected error");

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = code,
            message = status == 500 ? "An unexpected error occurred." : ex.Message,
            details = (object?)null
        });
    }
});

// errors that have no body (e.g. a url that doesn't exist) also get the same json format
app.UseStatusCodePages(async sc =>
{
    var response = sc.HttpContext.Response;
    bool notFound = response.StatusCode == 404;
    await response.WriteAsJsonAsync(new
    {
        error = notFound ? "NOT_FOUND" : "BAD_REQUEST",
        message = notFound ? "The requested resource does not exist." : "The request could not be completed.",
        details = (object?)null
    });
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
