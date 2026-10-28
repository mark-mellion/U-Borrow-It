using U_Borrow_It.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UBorrowIt.Data;
using U_Borrow_It.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Radzen;
using U_Borrow_It.Core.Interfaces;
using U_Borrow_It.Repository;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationContextConnection")
    ?? throw new InvalidOperationException("Connection string 'ApplicationContextConnection' not found.");

builder.Services.AddDbContext<ApplicationContext>(
    options => options.UseSqlServer(connectionString)
    );

builder.Services.AddDefaultIdentity<ApplicationUser>(
    options => options.SignIn.RequireConfirmedAccount = true
    ).AddEntityFrameworkStores<ApplicationContext>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<ILentItemRepository>(_ => new LentItemRepository(connectionString));
builder.Services.AddScoped<IAccountRepository>(_ => new AccountRepository(connectionString));
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
