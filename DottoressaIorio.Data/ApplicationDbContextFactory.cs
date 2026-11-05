using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DottoressaIorio.Data;

/// <summary>
///     To ensure that the dotnet ef tool can create an instance of your ApplicationDbContext, you can implement a
///     design-time factory.
///     This is especially useful if your ApplicationDbContext has complex configuration.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite("Data Source=app.db");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}