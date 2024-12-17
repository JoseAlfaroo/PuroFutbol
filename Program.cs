using Ecommerce_2024_1_NJD.Interfaces;
using Ecommerce_2024_1_NJD.Models;

var builder = WebApplication.CreateBuilder(args);

// Agregar JSON a config
builder.Configuration.AddJsonFile("emailSettings.json", optional: false, reloadOnChange: true);

// Correoselo :v
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Add services to the container.
builder.Services.AddControllersWithViews();

//AÑADIR UN OBJETO SE SESSION A NIVEL DE PROYECTO
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromMinutes(90);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;

});



var app = builder.Build();

//----------
//HABILITAR EL ESTADO DE LA SESSION A NIVEL PROYECTO
app.UseSession();
//----------


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
