using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.IncludeFields = true);
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
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = code,
            message = status == 500 ? "An unexpected error occurred." : ex.Message,
            details = (object?)null
        });
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();