using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data.Seeders;

public static class PublicDataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedSiteStats(db);
        await SeedTattooStyles(db);
        await SeedArtists(db);
        await SeedTestimonials(db);
        await SeedPopularCities(db);
    }

    private static async Task SeedSiteStats(AppDbContext db)
    {
        var stats = new (string Key, string Value, string Label, int Order)[]
        {
            ("artists", "10,000", "Artists", 0),
            ("tattoos", "50,000", "Tattoos", 1),
            ("styles", "50", "Styles", 2),
        };

        foreach (var (key, value, label, order) in stats)
        {
            var existing = await db.SiteStats.FirstOrDefaultAsync(s => s.Key == key);
            if (existing != null)
            {
                existing.Value = value;
                existing.Label = label;
                existing.OrderNo = order;
            }
            else
            {
                db.SiteStats.Add(new SiteStat { Key = key, Value = value, Label = label, OrderNo = order });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedTattooStyles(AppDbContext db)
    {
        var styles = new (string Name, string Slug, string Desc, string Image, int Artists, int Studios, bool Featured, int Order)[]
        {
            ("Traditional", "traditional", "Bold lines, vivid colors, and timeless designs. The style that started it all.", "/images/styles/traditional.jpg", 2847, 872, true, 0),
            ("Realism", "realism", "Photorealistic tattoos that capture every detail with stunning precision.", "/images/styles/realism.jpg", 3796, 798, false, 1),
            ("Japanese", "japanese", "Rich cultural imagery with bold outlines, vibrant colors and flowing compositions.", "/images/styles/japanese.jpg", 645, 161, false, 2),
            ("Fine Line", "fine-line", "Delicate, precise linework creating elegant and understated designs.", "/images/styles/fine-line.jpg", 3170, 647, false, 3),
            ("Geometric", "geometric", "Mathematical precision meets artistic expression in structured patterns.", "/images/styles/geometric.jpg", 1234, 310, false, 4),
            ("Neo-traditional", "neo-traditional", "Modern evolution of traditional tattoos with richer detail and color palettes.", "/images/styles/neo-traditional.jpg", 1523, 380, false, 5),
            ("Watercolor", "watercolor", "Flowing, painterly tattoos that mimic the look of watercolor paintings.", "/images/styles/watercolor.jpg", 987, 245, false, 6),
            ("Dotwork", "dotwork", "Intricate patterns and shading created entirely from carefully placed dots.", "/images/styles/dotwork.jpg", 876, 220, false, 7),
            ("Black & Gray", "black-gray", "Classic monochromatic tattoos with smooth gradients and deep contrast.", "/images/styles/black-gray.jpg", 2156, 540, false, 8),
            ("Illustrative", "illustrative", "Drawing-inspired tattoos blending realism with artistic illustration.", "/images/styles/illustrative.jpg", 1345, 335, false, 9),
            ("Minimalist", "minimalist", "Simple, clean designs that make a statement with less.", "/images/styles/minimalist.jpg", 1567, 390, false, 10),
            ("Anime", "anime", "Japanese animation-inspired tattoos with bold colors and dynamic characters.", "/images/styles/anime.jpg", 743, 185, false, 11),
        };

        foreach (var (name, slug, desc, image, artists, studios, featured, order) in styles)
        {
            var existing = await db.TattooStyles.FirstOrDefaultAsync(s => s.Slug == slug);
            if (existing != null)
            {
                existing.Name = name;
                existing.Description = desc;
                existing.ImageUrl = image;
                existing.ArtistCount = artists;
                existing.StudioCount = studios;
                existing.IsFeatured = featured;
                existing.OrderNo = order;
            }
            else
            {
                db.TattooStyles.Add(new TattooStyle
                {
                    Name = name, Slug = slug, Description = desc, ImageUrl = image,
                    ArtistCount = artists, StudioCount = studios, IsFeatured = featured, OrderNo = order
                });
            }
        }

        await db.SaveChangesAsync();

        await SeedStyleRelations(db);
    }

    private static async Task SeedStyleRelations(AppDbContext db)
    {
        var relations = new Dictionary<string, (string Name, int Count)[]>
        {
            ["traditional"] = [
                ("Traditional", 3051), ("Illustrative", 1186), ("Neo-traditional", 691),
                ("Black & grey", 306), ("Dotwork", 270), ("Japanese", 266),
                ("Floral", 142), ("New school", 123), ("Geometric", 121),
            ],
            ["realism"] = [
                ("Realism", 3796), ("Black & grey", 2995), ("Trash polka", 441),
                ("New school", 403), ("Illustrative", 214), ("Neo-traditional", 215),
                ("Watercolor", 198), ("Cover-up", 169), ("Dotwork", 129),
            ],
            ["japanese"] = [
                ("Japanese", 644), ("Traditional", 266), ("Neo-traditional", 162),
                ("Illustrative", 130), ("Black & grey", 106), ("New school", 58),
                ("Geometric", 41), ("Tribal", 37), ("Floral", 27),
            ],
            ["fine-line"] = [
                ("Fine line", 3170), ("Floral", 959), ("Dotwork", 914),
                ("Minimalist", 600), ("Illustrative", 594), ("Sketch", 460),
                ("Geometric", 394), ("Black & grey", 374), ("Lettering", 245),
                ("Vegan", 231),
            ],
        };

        foreach (var (slug, related) in relations)
        {
            var style = await db.TattooStyles.FirstOrDefaultAsync(s => s.Slug == slug);
            if (style == null) continue;

            var existingRelations = await db.StyleRelations
                .Where(r => r.TattooStyleId == style.Id)
                .ToListAsync();
            db.StyleRelations.RemoveRange(existingRelations);
            await db.SaveChangesAsync();

            for (int i = 0; i < related.Length; i++)
            {
                db.StyleRelations.Add(new StyleRelation
                {
                    TattooStyleId = style.Id,
                    RelatedStyleName = related[i].Name,
                    Count = related[i].Count,
                    OrderNo = i,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedArtists(AppDbContext db)
    {
        var artists = new (string Name, string Slug, string City, string Country, double Rating, bool IsStudio, string? Bio, string? Instagram, string? Followers, int? PriceMin, int? PriceMax, int Pieces, string[] Styles)[]
        {
            ("Bang Bang Tattoo", "bangbangnyc", "New York", "United States", 4.9, true,
                "Bang Bang Tattoo, with locations on Broome and Grand Streets in the heart of New York City, is an illustrious studio renowned for its mastery in fineline, dotwork, and illustrative tattoo styles. With an impressive base of over 2.5 million followers on Instagram, they welcome both walk-ins and pre-scheduled appointments.",
                "bangbangnyc", "2.5M", 540, 1200, 365, ["Studio", "Fine line", "Dotwork", "Illustrative"]),
            ("Romeo Lacoste", "romeolacoste", "Los Angeles", "United States", 4.8, false,
                "Celebrity tattoo artist known for fine line and dotwork tattoos. Has tattooed numerous celebrities and built a massive social media following.",
                "romeolacoste", "2.1M", 400, 880, 280, ["Fine line", "Dotwork"]),
            ("Tatu Baby", "tatubaby", "Miami", "United States", 4.7, false,
                "Known from Ink Master, Tatu Baby specializes in black and grey realism with stunning portrait work.",
                "tatubaby", "1.9M", 350, 760, 195, ["Black & grey", "Realism"]),
            ("Winston The Whale", "winstonthewhale", "Portland", "United States", 4.8, false,
                "Known for bold illustrative and traditional tattoo work with a modern twist.",
                "winstonthewhale", "245K", 300, 700, 142, ["Illustrative", "Traditional"]),
            ("Duda Lozano", "dudalozano", "Piacenza", "Italy", 4.7, false,
                "Italian artist blending illustrative and traditional styles with European flair.",
                "dudalozano", "180K", 250, 600, 98, ["Illustrative", "Traditional"]),
            ("Samuele Briganti", "samuelebriganti", "Florence", "Italy", 4.9, false,
                "Master of traditional tattooing based in the heart of Tuscany. Bold lines and rich colors.",
                "samuelebriganti", "320K", 280, 650, 210, ["Traditional"]),
            ("Paul Acker", "paulackertattoo", "Bensalem", "United States", 4.8, false,
                "Horror realism specialist creating incredibly detailed dark art tattoos.",
                "paulackertattoo", "410K", 400, 900, 175, ["Realism"]),
            ("Jesse Rix", "jesserix", "Keene", "United States", 4.9, false,
                "Famous for mind-bending optical illusion tattoos that look like windows into another world.",
                "jesserix", "580K", 500, 1100, 230, ["Realism", "New school"]),
            ("Black Tattoo Alicante", "blacktattooalicante", "Alicante", "Spain", 4.6, true,
                "Premier tattoo studio in Alicante specializing in black and grey realism work.",
                "blacktattooalicante", "95K", 200, 500, 320, ["Studio", "Black & grey", "Realism"]),
            ("Gakkin", "gaboronegakkin", "Tokyo", "Japan", 4.9, false,
                "Legendary Japanese tattoo artist known for freehand tribal and Japanese-inspired work.",
                "gaboronegakkin", "750K", 600, 1500, 180, ["Tribal", "Japanese"]),
            ("Suflanda", "suflanda", "Brooklyn", "United States", 4.8, false,
                "Whimsical fine line illustrative tattoos with a storybook quality.",
                "suflanda", "520K", 300, 650, 155, ["Fine line", "Dotwork"]),
            ("Okan Uckun", "ofrfrn", "New York", "United States", 4.7, false,
                "Geometric and fine line specialist creating precise, architectural tattoo designs.",
                "ofrfrn", "290K", 350, 800, 120, ["Fine line", "Geometric"]),
        };

        for (int i = 0; i < artists.Length; i++)
        {
            var (name, slug, city, country, rating, isStudio, bio, instagram, followers, priceMin, priceMax, pieces, styles) = artists[i];
            var existing = await db.Artists.Include(a => a.ArtistStyles).FirstOrDefaultAsync(a => a.Slug == slug);

            if (existing != null)
            {
                existing.Name = name;
                existing.City = city;
                existing.Country = country;
                existing.Rating = rating;
                existing.IsStudio = isStudio;
                existing.OrderNo = i;
                existing.Bio = bio;
                existing.InstagramHandle = instagram;
                existing.Followers = followers;
                existing.PriceMin = priceMin;
                existing.PriceMax = priceMax;
                existing.PieceCount = pieces;

                db.ArtistStyles.RemoveRange(existing.ArtistStyles);
                foreach (var s in styles)
                    existing.ArtistStyles.Add(new ArtistStyle { StyleName = s });
            }
            else
            {
                var artist = new Artist
                {
                    Name = name, Slug = slug, City = city, Country = country,
                    Rating = rating, IsStudio = isStudio, OrderNo = i,
                    Bio = bio, InstagramHandle = instagram, Followers = followers,
                    PriceMin = priceMin, PriceMax = priceMax, PieceCount = pieces,
                };
                foreach (var s in styles)
                    artist.ArtistStyles.Add(new ArtistStyle { StyleName = s });
                db.Artists.Add(artist);
            }
        }

        await db.SaveChangesAsync();

        await SeedArtistPortfolioImages(db);
    }

    private static readonly string[] StyleSlugs = [
        "traditional", "realism", "japanese", "fine-line", "dotwork",
        "illustrative", "black-gray", "geometric", "watercolor",
        "neo-traditional", "minimalist", "anime",
    ];

    private static readonly Dictionary<string, string> StyleNameToSlug = new()
    {
        ["Fine line"] = "fine-line",
        ["Dotwork"] = "dotwork",
        ["Illustrative"] = "illustrative",
        ["Traditional"] = "traditional",
        ["Realism"] = "realism",
        ["Japanese"] = "japanese",
        ["Black & grey"] = "black-gray",
        ["Geometric"] = "geometric",
        ["Watercolor"] = "watercolor",
        ["Neo Traditional"] = "neo-traditional",
        ["Minimalist"] = "minimalist",
        ["Anime"] = "anime",
        ["Tribal"] = "japanese",
        ["New school"] = "illustrative",
    };

    private static async Task SeedArtistPortfolioImages(AppDbContext db)
    {
        var allArtists = await db.Artists.Include(a => a.PortfolioImages).Include(a => a.ArtistStyles).ToListAsync();
        var rng = new Random(42);

        // Clear existing to re-seed with unique images
        foreach (var artist in allArtists)
        {
            if (artist.PortfolioImages.Count > 0)
                db.ArtistPortfolioImages.RemoveRange(artist.PortfolioImages);
        }
        await db.SaveChangesAsync();

        for (int artistIdx = 0; artistIdx < allArtists.Count; artistIdx++)
        {
            var artist = allArtists[artistIdx];

            // Get style slugs this artist specializes in
            var artistStyleSlugs = artist.ArtistStyles
                .Where(s => s.StyleName != "Studio" && StyleNameToSlug.ContainsKey(s.StyleName))
                .Select(s => StyleNameToSlug[s.StyleName])
                .Distinct()
                .ToList();

            // Build a unique image pool: artist's styles first (with variant based on artist index), then others
            var pool = new List<string>();

            // Each artist gets a different variant (1-4) of their own style images
            foreach (var slug in artistStyleSlugs)
            {
                var variant = (artistIdx % 4) + 1;
                pool.Add($"/images/portfolio/{slug}-{variant}.jpg");
            }

            // Fill remaining with other style variants, offset by artist index
            var otherSlugs = StyleSlugs.Where(s => !artistStyleSlugs.Contains(s)).ToList();
            for (int i = 0; i < otherSlugs.Count; i++)
            {
                var variant = ((artistIdx + i) % 4) + 1;
                pool.Add($"/images/portfolio/{otherSlugs[i]}-{variant}.jpg");
            }

            var imageCount = Math.Min(12, Math.Max(6, artist.PieceCount / 20));
            var basePrice = artist.PriceMin ?? 200;
            var maxPrice = artist.PriceMax ?? 800;

            for (int j = 0; j < imageCount; j++)
            {
                var price = basePrice + rng.Next(0, maxPrice - basePrice);
                price = (price / 10) * 10;

                artist.PortfolioImages.Add(new ArtistPortfolioImage
                {
                    ImageUrl = pool[j % pool.Count],
                    Price = price,
                    OrderNo = j,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedTestimonials(AppDbContext db)
    {
        var testimonials = new (string Quote, string Name, string Initials, string City, bool Verified)[]
        {
            ("Found an amazing blackwork artist through the portfolio search. The booking was seamless and my sleeve turned out perfect.", "Sarah M.", "SM", "New York", true),
            ("Was skeptical at first, but the verified reviews gave me confidence. My artist exceeded all expectations — the detail work is incredible.", "Mike T.", "MT", "Los Angeles", false),
            ("First tattoo and I was nervous. Found a gentle artist who specialized in fine line work. Couldn't be happier with the result!", "Emma L.", "EL", "London", false),
        };

        for (int i = 0; i < testimonials.Length; i++)
        {
            var (quote, name, initials, city, verified) = testimonials[i];
            var existing = await db.Testimonials.FirstOrDefaultAsync(t => t.AuthorInitials == initials);

            if (existing != null)
            {
                existing.Quote = quote;
                existing.AuthorName = name;
                existing.City = city;
                existing.IsVerified = verified;
                existing.OrderNo = i;
            }
            else
            {
                db.Testimonials.Add(new Testimonial
                {
                    Quote = quote, AuthorName = name, AuthorInitials = initials,
                    City = city, IsVerified = verified, OrderNo = i,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedPopularCities(AppDbContext db)
    {
        var cities = new[] { "New York", "Los Angeles", "London", "Paris", "Berlin", "Tokyo", "Sydney", "Toronto", "Miami", "Barcelona" };

        for (int i = 0; i < cities.Length; i++)
        {
            var existing = await db.PopularCities.FirstOrDefaultAsync(c => c.Name == cities[i]);
            if (existing != null)
            {
                existing.OrderNo = i;
            }
            else
            {
                db.PopularCities.Add(new PopularCity { Name = cities[i], OrderNo = i });
            }
        }

        await db.SaveChangesAsync();
    }
}
