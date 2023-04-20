using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Excel.Hubs;
using RabbitMQ.Excel.Models;
using RabbitMQ.Excel.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton(sp => new ConnectionFactory()
{ Uri = new Uri(builder.Configuration.GetConnectionString("RAbbitMQ")), DispatchConsumersAsync = true });

builder.Services.AddSingleton<RabbitMQClientService>();
builder.Services.AddSingleton<RabbitMQPublisher>();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
{
    opt.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    appDbContext.Database.Migrate();
    if (!appDbContext.Users.Any())
    {
        userManager.CreateAsync(new IdentityUser() { UserName = "deneme", Email = "deneme@gmail.com" }, "Password12*").Wait();
        userManager.CreateAsync(new IdentityUser() { UserName = "deneme2", Email = "deneme2@gmail.com" }, "Password12*").Wait();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<MyHub>("/MyHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();