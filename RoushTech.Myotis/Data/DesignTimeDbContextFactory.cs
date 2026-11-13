namespace RoushTech.Myotis.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MyotisDbContext>
{
    public MyotisDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MyotisDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
