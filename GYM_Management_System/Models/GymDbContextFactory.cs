using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GYM_Management_System.Models;

/// <summary>
/// Factory dùng cho EF Core CLI (dotnet ef migrations ...).
/// Luôn dùng PostgreSQL để migration luôn sinh ra đúng dialect, không phụ thuộc
/// vào DatabaseProvider đang cấu hình cho môi trường dev (SQLite).
/// </summary>
public class GymDbContextFactory : IDesignTimeDbContextFactory<GymDbContext>
{
    public GymDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GYM_DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=GYM Database;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<GymDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new GymDbContext(optionsBuilder.Options);
    }
}
