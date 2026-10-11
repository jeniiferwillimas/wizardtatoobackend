using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleType> ModuleTypes => Set<ModuleType>();
    public DbSet<ModuleRoleAuthorization> ModuleRoleAuthorizations => Set<ModuleRoleAuthorization>();
    public DbSet<TattooStyle> TattooStyles => Set<TattooStyle>();
    public DbSet<StyleRelation> StyleRelations => Set<StyleRelation>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<ArtistStyle> ArtistStyles => Set<ArtistStyle>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<PopularCity> PopularCities => Set<PopularCity>();
    public DbSet<SiteStat> SiteStats => Set<SiteStat>();
    public DbSet<ArtistProfile> ArtistProfiles => Set<ArtistProfile>();
    public DbSet<ArtistImage> ArtistImages => Set<ArtistImage>();
    public DbSet<ArtistProfileStyle> ArtistProfileStyles => Set<ArtistProfileStyle>();
    public DbSet<ArtistPortfolioImage> ArtistPortfolioImages => Set<ArtistPortfolioImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Module>(e =>
        {
            e.HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.ModuleType)
                .WithMany(mt => mt.Modules)
                .HasForeignKey(m => m.ModuleTypeId);
        });

        builder.Entity<ModuleRoleAuthorization>(e =>
        {
            e.HasIndex(a => new { a.Subject, a.RoleName }).IsUnique();
        });

        builder.Entity<TattooStyle>(e =>
        {
            e.HasIndex(s => s.Slug).IsUnique();
        });

        builder.Entity<StyleRelation>(e =>
        {
            e.HasOne(r => r.TattooStyle)
                .WithMany(s => s.RelatedStyles)
                .HasForeignKey(r => r.TattooStyleId);
        });

        builder.Entity<Artist>(e =>
        {
            e.HasIndex(a => a.Slug).IsUnique();
        });

        builder.Entity<ArtistStyle>(e =>
        {
            e.HasOne(a => a.Artist)
                .WithMany(a => a.ArtistStyles)
                .HasForeignKey(a => a.ArtistId);
        });

        builder.Entity<ArtistPortfolioImage>(e =>
        {
            e.HasOne(i => i.Artist)
                .WithMany(a => a.PortfolioImages)
                .HasForeignKey(i => i.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ArtistProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<ArtistProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ArtistImage>(e =>
        {
            e.HasOne(i => i.ArtistProfile)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ArtistProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ArtistProfileStyle>(e =>
        {
            e.HasOne(s => s.ArtistProfile)
                .WithMany(p => p.Styles)
                .HasForeignKey(s => s.ArtistProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
