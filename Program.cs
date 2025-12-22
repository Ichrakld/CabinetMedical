using Microsoft.EntityFrameworkCore;
using GestionCabinetMedical.Models; // Ajustez le namespace
using Microsoft.AspNetCore.Identity;
using GestionCabinetMedical.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuration DbContext
builder.Services.AddDbContext<BdCabinetMedicalContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CabinetMedicalConnection")));

// Configuration Identity (sera ajouté plus tard)
builder.Services.AddDbContext<CabinetMedicalIdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CabinetMedicalConnection")));

builder.Services.AddIdentity<Userper, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<CabinetMedicalIdentityContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();
builder.Services.AddRazorPages();

var app = builder.Build();
// Création automatique des rôles et utilisateurs Admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Userper>>();

    string[] roles = { "ADMIN", "MEDECIN", "SECRETAIRE", "PATIENT", "USER" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Créer un admin par défaut
    var adminEmail = "admin@cabinetmedical.ma";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new Userper
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nom = "Administrateur",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "ADMIN");
    }

    // Créer un médecin par défaut
    var medecinEmail = "medecin@cabinetmedical.ma";
    var medecinUser = await userManager.FindByEmailAsync(medecinEmail);

    if (medecinUser == null)
    {
        medecinUser = new Userper
        {
            UserName = medecinEmail,
            Email = medecinEmail,
            Nom = "Dr. Bennani",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(medecinUser, "Medecin@123");
        await userManager.AddToRoleAsync(medecinUser, "MEDECIN");
    }

    // Créer une secrétaire par défaut
    var secretaireEmail = "secretaire@cabinetmedical.ma";
    var secretaireUser = await userManager.FindByEmailAsync(secretaireEmail);

    if (secretaireUser == null)
    {
        secretaireUser = new Userper
        {
            UserName = secretaireEmail,
            Email = secretaireEmail,
            Nom = "El Amrani",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(secretaireUser, "Secretaire@123");
        await userManager.AddToRoleAsync(secretaireUser, "SECRETAIRE");
    }
}
// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();