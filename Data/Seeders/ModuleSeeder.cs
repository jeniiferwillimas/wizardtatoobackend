using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data.Seeders;

public static class ModuleSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedModuleTypes(db);
        await RemoveOldModules(db);
        await SeedModules(db);
        await SeedAuthorizations(db);
    }

    private static async Task RemoveOldModules(AppDbContext db)
    {
        var obsolete = new[] { "find-artists", "browse-styles", "by-location" };
        var oldAuths = await db.ModuleRoleAuthorizations
            .Where(a => obsolete.Contains(a.Subject))
            .ToListAsync();
        db.ModuleRoleAuthorizations.RemoveRange(oldAuths);

        var oldModules = await db.Modules
            .Where(m => obsolete.Contains(m.TextId))
            .ToListAsync();
        db.Modules.RemoveRange(oldModules);

        await db.SaveChangesAsync();
    }

    private static async Task SeedModuleTypes(AppDbContext db)
    {
        var types = new[] { "General", "Configurations" };

        foreach (var textId in types)
        {
            if (!await db.ModuleTypes.AnyAsync(t => t.TextId == textId))
                db.ModuleTypes.Add(new ModuleType { TextId = textId });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedModules(AppDbContext db)
    {
        var general = await db.ModuleTypes.FirstAsync(t => t.TextId == "General");
        var config = await db.ModuleTypes.FirstAsync(t => t.TextId == "Configurations");

        var definitions = new List<ModuleDef>
        {
            new("Dashboard", "dashboard", "Main", general.Id, "/dashboard", "LayoutDashboard", 0),
            new("My Portfolio", "my-portfolio", "Artist", general.Id, "/dashboard/portfolio", "Image", 1),
            new("My Bookings", "artist-bookings", "Artist", general.Id, "/dashboard/bookings", "CalendarDays", 2),
            new("Reviews", "artist-reviews", "Artist", general.Id, "/dashboard/reviews", "Star", 3),
            new("Messages", "artist-messages", "Artist", general.Id, "/dashboard/messages", "MessageSquare", 4),
            new("Find By", "find-by", "Browse", general.Id, null, "Search", 5),
            new("My Bookmarks", "my-bookmarks", "Browse", general.Id, "/dashboard/bookmarks", "Bookmark", 8),
            new("My Appointments", "my-appointments", "Browse", general.Id, "/dashboard/appointments", "CalendarDays", 9),
            new("User Management", "user-management", "Administration", config.Id, null, "Users", 10),
            new("Artists Management", "artists-management", "Administration", config.Id, "/dashboard/admin/artists", "UserCircle", 11),
            new("Analytics", "analytics", "Administration", config.Id, "/dashboard/admin/analytics", "BarChart3", 12),
            new("Settings", "settings", "Administration", config.Id, "/dashboard/admin/settings", "Settings", 13),
        };

        var childDefinitions = new List<ChildModuleDef>
        {
            new("By Location", "by-location", "Browse", general.Id, "/dashboard/browse/location", "find-by", 1),
            new("By Style", "by-style", "Browse", general.Id, "/dashboard/browse/style", "find-by", 2),
            new("Browse Studios", "browse-studios", "Browse", general.Id, "/dashboard/browse/studios", "find-by", 3),
            new("All Users", "all-users", "Administration", config.Id, "/dashboard/admin/users", "user-management", 1),
            new("Role Management", "role-management", "Administration", config.Id, "/dashboard/admin/roles", "user-management", 2),
        };

        foreach (var def in definitions)
        {
            var existing = await db.Modules.FirstOrDefaultAsync(m => m.TextId == def.TextId);
            if (existing != null)
            {
                existing.Name = def.Name;
                existing.Header = def.Header;
                existing.ModuleTypeId = def.ModuleTypeId;
                existing.UrlPath = def.UrlPath;
                existing.Icon = def.Icon;
                existing.OrderNo = def.OrderNo;
            }
            else
            {
                db.Modules.Add(new Module
                {
                    Name = def.Name,
                    TextId = def.TextId,
                    Header = def.Header,
                    ModuleTypeId = def.ModuleTypeId,
                    UrlPath = def.UrlPath,
                    Icon = def.Icon,
                    OrderNo = def.OrderNo,
                });
            }
        }

        await db.SaveChangesAsync();

        foreach (var def in childDefinitions)
        {
            var parent = await db.Modules.FirstAsync(m => m.TextId == def.ParentTextId);
            var existing = await db.Modules.FirstOrDefaultAsync(m => m.TextId == def.TextId);
            if (existing != null)
            {
                existing.Name = def.Name;
                existing.Header = def.Header;
                existing.ModuleTypeId = def.ModuleTypeId;
                existing.UrlPath = def.UrlPath;
                existing.ParentId = parent.Id;
                existing.OrderNo = def.OrderNo;
            }
            else
            {
                db.Modules.Add(new Module
                {
                    Name = def.Name,
                    TextId = def.TextId,
                    Header = def.Header,
                    ModuleTypeId = def.ModuleTypeId,
                    UrlPath = def.UrlPath,
                    ParentId = parent.Id,
                    OrderNo = def.OrderNo,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAuthorizations(AppDbContext db)
    {
        var allTextIds = await db.Modules.Select(m => m.TextId).ToListAsync();

        var roleModuleMap = new Dictionary<string, string[]>
        {
            ["SuperAdmin"] = allTextIds.ToArray(),
            ["Artist"] = ["dashboard", "my-portfolio", "artist-bookings", "artist-reviews", "artist-messages"],
            ["NormalUser"] = ["dashboard", "find-by", "by-location", "by-style", "browse-studios", "my-bookmarks", "my-appointments"],
        };

        foreach (var (roleName, subjects) in roleModuleMap)
        {
            foreach (var subject in subjects)
            {
                var exists = await db.ModuleRoleAuthorizations
                    .AnyAsync(a => a.Subject == subject && a.RoleName == roleName);

                if (!exists)
                {
                    db.ModuleRoleAuthorizations.Add(new ModuleRoleAuthorization
                    {
                        Subject = subject,
                        RoleName = roleName,
                        IsGranted = true,
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private record ModuleDef(string Name, string TextId, string Header, int ModuleTypeId, string? UrlPath, string? Icon, int OrderNo);
    private record ChildModuleDef(string Name, string TextId, string Header, int ModuleTypeId, string? UrlPath, string ParentTextId, int OrderNo);
}
