using Microsoft.EntityFrameworkCore;
using GestionCabinetMedical.Models;
using Microsoft.AspNetCore.Identity;
using GestionCabinetMedical.Areas.Identity.Data;
using GestionCabinetMedical.Services;
using GestionCabinetMedical.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuration DbContext
builder.Services.AddDbContext<BdCabinetMedicalContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CabinetMedicalConnection")));

// Configuration Identity
builder.Services.AddDbContext<CabinetMedicalIdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CabinetMedicalConnection")));

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICrudNotificationHelper, CrudNotificationHelper>();

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
    var context = scope.ServiceProvider.GetRequiredService<BdCabinetMedicalContext>();

    string[] roles = { "ADMIN", "MEDECIN", "SECRETAIRE", "PATIENT", "USER" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ============================================================
    // ADMIN PAR DÉFAUT
    // ============================================================
    var adminEmail = "admin@cabinetmedical.ma";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    var existingAdminUtilisateur = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == adminEmail);

    if (adminUser == null)
    {
        // Créer dans Identity
        adminUser = new Userper
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nom = "Administrateur Système",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, "Admin@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "ADMIN");

            // Créer dans la table Utilisateur si n'existe pas
            if (existingAdminUtilisateur == null)
            {
                var utilisateurAdmin = new Utilisateur
                {
                    Nom = "Administrateur",
                    Prenom = "Système",
                    Email = adminEmail,
                    MotDePasse = "Admin@123",
                    Telephone = "0600000000",
                    EstActif = true
                };
                context.Utilisateurs.Add(utilisateurAdmin);
                await context.SaveChangesAsync();

                // Créer l'entrée Admin
                var admin = new Admin
                {
                    Id = utilisateurAdmin.Id,
                    NiveauAcces = 1
                };
                context.Admins.Add(admin);
                await context.SaveChangesAsync();
            }
        }
    }
    else if (existingAdminUtilisateur == null)
    {
        // L'utilisateur existe dans Identity mais pas dans Utilisateur
        var utilisateurAdmin = new Utilisateur
        {
            Nom = "Administrateur",
            Prenom = "Système",
            Email = adminEmail,
            MotDePasse = "Admin@123",
            Telephone = "0600000000",
            EstActif = true
        };
        context.Utilisateurs.Add(utilisateurAdmin);
        await context.SaveChangesAsync();

        // Vérifier si l'entrée Admin existe
        var existingAdmin = await context.Admins.FirstOrDefaultAsync(a => a.Id == utilisateurAdmin.Id);
        if (existingAdmin == null)
        {
            var admin = new Admin
            {
                Id = utilisateurAdmin.Id,
                NiveauAcces = 1
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // MÉDECIN PAR DÉFAUT
    // ============================================================
    var medecinEmail = "medecin@cabinetmedical.ma";
    var medecinUser = await userManager.FindByEmailAsync(medecinEmail);
    var existingMedecinUtilisateur = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == medecinEmail);

    if (medecinUser == null)
    {
        medecinUser = new Userper
        {
            UserName = medecinEmail,
            Email = medecinEmail,
            Nom = "Dr. Bennani Ahmed",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(medecinUser, "Medecin@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(medecinUser, "MEDECIN");

            if (existingMedecinUtilisateur == null)
            {
                var utilisateurMedecin = new Utilisateur
                {
                    Nom = "Bennani",
                    Prenom = "Ahmed",
                    Email = medecinEmail,
                    MotDePasse = "Medecin@123",
                    Telephone = "0611111111",
                    EstActif = true
                };
                context.Utilisateurs.Add(utilisateurMedecin);
                await context.SaveChangesAsync();

                var medecin = new Medecin
                {
                    Id = utilisateurMedecin.Id,
                    Specialite = "Médecine Générale"
                };
                context.Medecins.Add(medecin);
                await context.SaveChangesAsync();
            }
        }
    }
    else if (existingMedecinUtilisateur == null)
    {
        var utilisateurMedecin = new Utilisateur
        {
            Nom = "Bennani",
            Prenom = "Ahmed",
            Email = medecinEmail,
            MotDePasse = "Medecin@123",
            Telephone = "0611111111",
            EstActif = true
        };
        context.Utilisateurs.Add(utilisateurMedecin);
        await context.SaveChangesAsync();

        var existingMedecin = await context.Medecins.FirstOrDefaultAsync(m => m.Id == utilisateurMedecin.Id);
        if (existingMedecin == null)
        {
            var medecin = new Medecin
            {
                Id = utilisateurMedecin.Id,
                Specialite = "Médecine Générale"
            };
            context.Medecins.Add(medecin);
            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // SECRÉTAIRE PAR DÉFAUT
    // ============================================================
    var secretaireEmail = "secretaire@cabinetmedical.ma";
    var secretaireUser = await userManager.FindByEmailAsync(secretaireEmail);
    var existingSecretaireUtilisateur = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == secretaireEmail);

    if (secretaireUser == null)
    {
        secretaireUser = new Userper
        {
            UserName = secretaireEmail,
            Email = secretaireEmail,
            Nom = "El Amrani Fatima",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(secretaireUser, "Secretaire@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(secretaireUser, "SECRETAIRE");

            if (existingSecretaireUtilisateur == null)
            {
                var utilisateurSecretaire = new Utilisateur
                {
                    Nom = "El Amrani",
                    Prenom = "Fatima",
                    Email = secretaireEmail,
                    MotDePasse = "Secretaire@123",
                    Telephone = "0622222222",
                    EstActif = true
                };
                context.Utilisateurs.Add(utilisateurSecretaire);
                await context.SaveChangesAsync();

                var secretaire = new Secretaire
                {
                    Id = utilisateurSecretaire.Id,
                    Service = "Accueil"
                };
                context.Secretaires.Add(secretaire);
                await context.SaveChangesAsync();
            }
        }
    }
    else if (existingSecretaireUtilisateur == null)
    {
        var utilisateurSecretaire = new Utilisateur
        {
            Nom = "El Amrani",
            Prenom = "Fatima",
            Email = secretaireEmail,
            MotDePasse = "Secretaire@123",
            Telephone = "0622222222",
            EstActif = true
        };
        context.Utilisateurs.Add(utilisateurSecretaire);
        await context.SaveChangesAsync();

        var existingSecretaire = await context.Secretaires.FirstOrDefaultAsync(s => s.Id == utilisateurSecretaire.Id);
        if (existingSecretaire == null)
        {
            var secretaire = new Secretaire
            {
                Id = utilisateurSecretaire.Id,
                Service = "Accueil"
            };
            context.Secretaires.Add(secretaire);
            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // PATIENT PAR DÉFAUT (pour les tests)
    // ============================================================
    var patientEmail = "patient@cabinetmedical.ma";
    var patientUser = await userManager.FindByEmailAsync(patientEmail);
    var existingPatientUtilisateur = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == patientEmail);

    if (patientUser == null)
    {
        patientUser = new Userper
        {
            UserName = patientEmail,
            Email = patientEmail,
            Nom = "Alami Mohammed",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(patientUser, "Patient@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(patientUser, "PATIENT");

            if (existingPatientUtilisateur == null)
            {
                var utilisateurPatient = new Utilisateur
                {
                    Nom = "Alami",
                    Prenom = "Mohammed",
                    Email = patientEmail,
                    MotDePasse = "Patient@123",
                    Telephone = "0633333333",
                    EstActif = true
                };
                context.Utilisateurs.Add(utilisateurPatient);
                await context.SaveChangesAsync();

                var patient = new Patient
                {
                    Id = utilisateurPatient.Id,
                    NumSecuriteSociale = "123456789012345",
                    DateNaissance = new DateTime(1990, 5, 15)
                };
                context.Patients.Add(patient);
                await context.SaveChangesAsync();
            }
        }
    }
    else if (existingPatientUtilisateur == null)
    {
        var utilisateurPatient = new Utilisateur
        {
            Nom = "Alami",
            Prenom = "Mohammed",
            Email = patientEmail,
            MotDePasse = "Patient@123",
            Telephone = "0633333333",
            EstActif = true
        };
        context.Utilisateurs.Add(utilisateurPatient);
        await context.SaveChangesAsync();

        var existingPatient = await context.Patients.FirstOrDefaultAsync(p => p.Id == utilisateurPatient.Id);
        if (existingPatient == null)
        {
            var patient = new Patient
            {
                Id = utilisateurPatient.Id,
                NumSecuriteSociale = "123456789012345",
                DateNaissance = new DateTime(1990, 5, 15)
            };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }
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