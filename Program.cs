
using ClinicHub.Models;
using ClinicHub.Models.Clinic;
using ClinicHub.Services.Implementations;
using ClinicHub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// CORS
const string AllowEverythingCorsPolicy = "AllowEverything";

// Add services
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

// Session
builder.Services.AddSession();

// Database
builder.Services.AddDbContext<ClinicDbContext>(op =>
{
    op.UseSqlServer(
        builder.Configuration.GetConnectionString("ConnectionClinic")
    );
});

// Account Service
builder.Services.AddScoped<IAccountService, AccountService>();

// CORS Configuration
builder.Services.AddCors(op =>
{
    op.AddPolicy(AllowEverythingCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// =====================================
// INITIAL SUPER ADMIN SETUP
// =====================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ClinicDbContext>();

    // Check Super_admin role
    var role = db.Roles
        .FirstOrDefault(r => r.Name == "Super_admin");

    if (role == null)
    {
        role = new Role
        {
            Name = "Super_admin"
        };

        db.Roles.Add(role);
        db.SaveChanges();
    }

    // Initial Super Admin details
    string email = "superadmin@clinichub.com";
    string temporaryPassword = "Clinic@12";

    var existingUser = db.Users
        .FirstOrDefault(u => u.Email == email);

    if (existingUser == null)
    {
        // Create new Super Admin
        var user = new User
        {
            FullName = "Super Admin",
            Email = email,
            Phone = "9999999999",
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            IsActive = true,
            CreatedAt = DateTime.Now,
            RoleId = role.Id
        };

        db.Users.Add(user);
        db.SaveChanges();
    }
    else
    {
        // Update password only if temporary password is not valid
        bool passwordValid =
            BCrypt.Net.BCrypt.Verify(
                temporaryPassword,
                existingUser.PasswordHash
            );

        if (!passwordValid)
        {
            existingUser.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

            existingUser.IsActive = true;
            existingUser.RoleId = role.Id;

            db.SaveChanges();
        }
    }
}

// =====================================
// HTTP REQUEST PIPELINE
// =====================================

app.UseCors(AllowEverythingCorsPolicy);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
