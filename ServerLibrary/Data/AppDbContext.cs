using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;

namespace ServerLibrary.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<GeneralDepartment> GeneralDepartments => Set<GeneralDepartment>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Branch> Branchs => Set<Branch>();
        public DbSet<Town> Towns => Set<Town>();
        public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    }
}
