using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionCabinetMedical.Areas.Identity.Data
{
    public class CabinetMedicalIdentityContext : IdentityDbContext<Userper>
    {
        public CabinetMedicalIdentityContext(DbContextOptions<CabinetMedicalIdentityContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}