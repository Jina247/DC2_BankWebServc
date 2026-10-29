var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.IncludeFields = true);

builder.Services.AddSingleton<BankMvc.Services.BankApiClient>();

var app = builder.Build();

// resilience: never show the developer exception page / stack trace to the user, even in Development
// any unhandled exception -> friendly /Home/Error page (the real error is written to the log)
app.UseExceptionHandler("/Home/Error");

// 403 / 404 etc. with no page of their own -> friendly /Home/Status/{code} page
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseStaticFiles();
app.UseRouting();
// Task Group 1: app.UseAuthentication(); goes here, before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
