using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Therapy> Therapies { get; set; }
    public DbSet<TherapyTemplate> TherapyTemplates { get; set; }
}