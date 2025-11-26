var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- EKLEME 1: KÝMLÝK DOÐRULAMA (AUTHENTICATION) SERVÝSÝ ---
// Sisteme diyoruz ki: "Biz giriþ iþlemlerini 'Cookie' (Çerez) ile yapacaðýz."
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Account/Login"; // Eðer kullanýcý giriþ yapmadýysa, onu zorla buraya at.
        config.ExpireTimeSpan = TimeSpan.FromHours(8); // Oturum 8 saat boyunca açýk kalsýn.
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- EKLEME 2: KÝMLÝK KONTROLÜ (MIDDLEWARE) ---
// ?? DÝKKAT: Bu satýr MUTLAKA 'UseAuthorization' satýrýndan ÖNCE gelmelidir.
// Mantýk þudur: Önce "Sen kimsin?" (Authentication), sonra "Yetkin var mý?" (Authorization).
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Giriþ yapýnca Home/Index'e gidecek

app.Run();