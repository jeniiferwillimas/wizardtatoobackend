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
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<SavedItem> SavedItems => Set<SavedItem>();
    public DbSet<ArtistSubscription> ArtistSubscriptions => Set<ArtistSubscription>();

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

        builder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Artist).WithMany().HasForeignKey(b => b.ArtistId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Conversation>(e =>
        {
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Artist).WithMany().HasForeignKey(c => c.ArtistId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.UserId, c.ArtistId }).IsUnique();
        });

        builder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Review>(e =>
        {
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Artist).WithMany().HasForeignKey(r => r.ArtistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SavedItem>(e =>
        {
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Artist).WithMany().HasForeignKey(s => s.ArtistId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.PortfolioImage).WithMany().HasForeignKey(s => s.PortfolioImageId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ArtistSubscription>(e =>
        {
            e.HasOne(s => s.ArtistProfile).WithMany().HasForeignKey(s => s.ArtistProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.ArtistProfileId).IsUnique();
        });
    }
}
