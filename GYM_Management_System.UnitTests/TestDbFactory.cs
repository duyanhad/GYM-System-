using System;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.UnitTests;

/// <summary>
/// Tạo DbContext in-memory riêng cho từng test (mỗi test dùng 1 database độc lập).
/// </summary>
public static class TestDbFactory
{
    public static GymDbContext Create()
    {
        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseInMemoryDatabase($"gym-tests-{Guid.NewGuid()}")
            .Options;

        var context = new GymDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
