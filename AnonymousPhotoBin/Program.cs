using AnonymousPhotoBin;
using AnonymousPhotoBin.Data;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<PhotoBinDbContext>(options =>
    options.UseCosmos(builder.Configuration.GetConnectionString("CosmosDB")!, "AnonymousPhotoBin"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<PhotoBinDbContext>();

builder.Services.AddRazorPages(options => {
    options.Conventions.AuthorizePage("/List", "SingletonAdmin");
});

builder.Services.AddSingleton<IAdminPasswordProvider>(
    new AdminPasswordProvider(builder.Configuration["AdminPassword"]!));

builder.Services.AddAzureClients(azure => {
    azure.AddBlobServiceClient(builder.Configuration["ConnectionStrings:AzureStorageConnectionString"]);
});

builder.Services.AddControllers();

builder.Services.AddAuthorization(options => {
    options.AddPolicy("SingletonAdmin", policy =>
        policy.Requirements.Add(
            new ClaimsAuthorizationRequirement(
                ClaimTypes.NameIdentifier,
                new[] { "81976842-8543-4dac-9729-dde8117b994f" })));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
} else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints => {
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.MapRazorPages();

app.Run();
