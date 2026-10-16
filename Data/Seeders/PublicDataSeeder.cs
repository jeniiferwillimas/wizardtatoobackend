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
            ("artists", "286", "Artists", 0),
            ("tattoos", "1,430", "Tattoos", 1),
            ("styles", "12", "Styles", 2),
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
            // ── United States ──
            ("Bang Bang Tattoo", "bangbangnyc", "New York", "United States", 4.9, true,
                "Illustrious studio renowned for fineline, dotwork, and illustrative tattoo styles. Walk-ins and pre-scheduled appointments welcome.",
                "bangbangnyc", "2.5M", 540, 1200, 365, ["Studio", "Fine line", "Dotwork", "Illustrative"]),
            ("Romeo Lacoste", "romeolacoste", "Los Angeles", "United States", 4.8, false,
                "Celebrity tattoo artist known for fine line and dotwork tattoos with a massive social media following.",
                "romeolacoste", "2.1M", 400, 880, 280, ["Fine line", "Dotwork"]),
            ("Tatu Baby", "tatubaby", "Miami", "United States", 4.7, false,
                "Known from Ink Master, specializes in black and grey realism with stunning portrait work.",
                "tatubaby", "1.9M", 350, 760, 195, ["Black & grey", "Realism"]),
            ("Winston The Whale", "winstonthewhale", "Portland", "United States", 4.8, false,
                "Bold illustrative and traditional tattoo work with a modern twist.",
                "winstonthewhale", "245K", 300, 700, 142, ["Illustrative", "Traditional"]),
            ("Paul Acker", "paulackertattoo", "Philadelphia", "United States", 4.8, false,
                "Horror realism specialist creating incredibly detailed dark art tattoos.",
                "paulackertattoo", "410K", 400, 900, 175, ["Realism"]),
            ("Jesse Rix", "jesserix", "Keene", "United States", 4.9, false,
                "Famous for mind-bending optical illusion tattoos that look like windows into another world.",
                "jesserix", "580K", 500, 1100, 230, ["Realism", "Illustrative"]),
            ("Suflanda", "suflanda", "Brooklyn", "United States", 4.8, false,
                "Whimsical fine line illustrative tattoos with a storybook quality.",
                "suflanda", "520K", 300, 650, 155, ["Fine line", "Dotwork"]),
            ("Okan Uckun", "ofrfrn", "New York", "United States", 4.7, false,
                "Geometric and fine line specialist creating precise, architectural tattoo designs.",
                "ofrfrn", "290K", 350, 800, 120, ["Fine line", "Geometric"]),
            ("Nikko Hurtado", "nikkohurtado", "Los Angeles", "United States", 4.9, false,
                "World-renowned color realism artist. Known for vibrant, hyper-realistic portrait work.",
                "nikkohurtado", "1.8M", 500, 1500, 310, ["Realism"]),
            ("Dr. Woo", "drwoo", "Los Angeles", "United States", 4.9, false,
                "Celebrity fine line artist known for delicate single-needle work on A-list clientele.",
                "drwoo", "1.7M", 600, 2000, 250, ["Fine line", "Minimalist"]),
            ("Megan Massacre", "meganmassacre", "New York", "United States", 4.7, false,
                "Renowned for vibrant illustrative and new school tattoos. TV personality and artist.",
                "meganmassacre", "680K", 350, 800, 190, ["Illustrative", "Traditional"]),
            ("JonBoy", "jonboytattoo", "New York", "United States", 4.8, false,
                "Minimalist fine line artist tattooing celebrities with small, meaningful designs.",
                "jonboytattoo", "1.5M", 300, 600, 420, ["Fine line", "Minimalist"]),
            ("Amanda Wachob", "amandawachob", "New York", "United States", 4.8, false,
                "Abstract and painterly tattoo pioneer blurring the line between body art and fine art.",
                "amandawachob", "340K", 400, 1000, 130, ["Watercolor", "Illustrative"]),
            ("Kat Von D", "katvond", "Los Angeles", "United States", 4.6, false,
                "Iconic tattoo artist and entrepreneur known for detailed black and grey portrait work.",
                "katvond", "3.2M", 500, 1200, 280, ["Black & grey", "Realism"]),
            ("Pony Lawson", "ponylawson", "San Diego", "United States", 4.7, false,
                "Color realism specialist known for vivid, eye-popping tattoo portraits and characters.",
                "ponylawson", "380K", 350, 900, 165, ["Realism"]),
            ("Roxx", "roxx_____", "San Francisco", "United States", 4.8, false,
                "Geometric blackwork artist creating bold, symmetrical body art compositions.",
                "roxx_____", "420K", 400, 1000, 140, ["Geometric", "Dotwork"]),
            ("Horizon Tattoo", "horizontattoo", "Chicago", "United States", 4.6, true,
                "Award-winning Chicago studio with artists covering every style from traditional to realism.",
                "horizontattoo", "75K", 150, 500, 520, ["Studio", "Traditional", "Realism", "Illustrative"]),
            ("Inked Souls Studio", "inkedsouls", "Austin", "United States", 4.5, true,
                "Vibrant Austin studio specializing in neo-traditional and watercolor tattoos.",
                "inkedsouls", "42K", 180, 450, 380, ["Studio", "Watercolor", "Illustrative"]),

            // ── Europe ──
            ("Duda Lozano", "dudalozano", "Piacenza", "Italy", 4.7, false,
                "Italian artist blending illustrative and traditional styles with European flair.",
                "dudalozano", "180K", 250, 600, 98, ["Illustrative", "Traditional"]),
            ("Samuele Briganti", "samuelebriganti", "Florence", "Italy", 4.9, false,
                "Master of traditional tattooing based in the heart of Tuscany. Bold lines and rich colors.",
                "samuelebriganti", "320K", 280, 650, 210, ["Traditional"]),
            ("Black Tattoo Alicante", "blacktattooalicante", "Alicante", "Spain", 4.6, true,
                "Premier tattoo studio in Alicante specializing in black and grey realism work.",
                "blacktattooalicante", "95K", 200, 500, 320, ["Studio", "Black & grey", "Realism"]),
            ("Oozy", "oozy_tattoo", "Seoul", "South Korea", 4.9, false,
                "Fine line artist creating intricate nature and botanical designs with stunning precision.",
                "oozy_tattoo", "690K", 350, 800, 200, ["Fine line", "Minimalist"]),
            ("Sasha Unisex", "sashaunisex", "Saint Petersburg", "Russia", 4.8, false,
                "Geometric watercolor pioneer creating vibrant, low-poly animal tattoos.",
                "sashaunisex", "1.1M", 300, 700, 175, ["Watercolor", "Geometric"]),
            ("Robert Pho", "robertpho", "Los Angeles", "United States", 4.7, false,
                "Black and grey realism master specializing in portraits and religious iconography.",
                "robertpho", "220K", 350, 850, 190, ["Black & grey", "Realism"]),
            ("Victor Octaviano", "victoroctaviano", "Sao Paulo", "Brazil", 4.8, false,
                "Brazilian artist known for large-scale dotwork and geometric compositions.",
                "victoroctaviano", "450K", 200, 500, 160, ["Dotwork", "Geometric"]),
            ("Mira Mariah", "girlknewyork", "New York", "United States", 4.7, false,
                "Known for delicate fine line tattoos, botanical imagery and feminist-themed designs.",
                "girlknewyork", "360K", 250, 550, 170, ["Fine line", "Minimalist"]),
            ("Jay Freestyle", "jayfreestyle", "Amsterdam", "Netherlands", 4.8, false,
                "Freehand tattoo artist creating abstract, flowing designs without stencils.",
                "jayfreestyle", "610K", 400, 1000, 145, ["Watercolor", "Illustrative"]),
            ("Curt Montgomery", "curtmontgomery", "Toronto", "Canada", 4.7, false,
                "Toronto-based artist known for illustrative and traditional tattoo work with witty themes.",
                "curtmontgomery", "280K", 250, 600, 165, ["Illustrative", "Traditional"]),
            ("Hongdam", "hongdam", "Seoul", "South Korea", 4.9, false,
                "Micro-realism fine line artist creating tiny, incredibly detailed tattoos.",
                "hongdam", "1.3M", 400, 900, 220, ["Fine line", "Minimalist", "Realism"]),
            ("Mr. K", "mr.k_tattoo", "New York", "United States", 4.8, false,
                "Bang Bang studio artist known for single-needle fine line micro tattoos.",
                "mr.k_tattoo", "1.2M", 350, 800, 280, ["Fine line", "Minimalist"]),
            ("Eva Krbdk", "evakrbdk", "Istanbul", "Turkey", 4.7, false,
                "Known for miniature circle tattoos featuring landscapes and scenes inside tiny frames.",
                "evakrbdk", "480K", 200, 500, 190, ["Minimalist", "Fine line"]),
            ("Jess Mascetti", "jessmascetti", "Melbourne", "Australia", 4.6, false,
                "Bold traditional and neo-traditional artist with a love for floral and animal motifs.",
                "jessmascetti", "165K", 200, 500, 135, ["Traditional", "Illustrative"]),
            ("Matty D Mooney", "mattydmooney", "London", "United Kingdom", 4.7, false,
                "Neo-traditional artist blending vintage illustration with modern tattoo technique.",
                "mattydmooney", "210K", 280, 650, 140, ["Illustrative"]),
            ("Mirko Sata", "mirkosata", "Milan", "Italy", 4.8, false,
                "Snake and serpent specialist creating bold blackwork designs with striking composition.",
                "mirkosata", "580K", 350, 800, 150, ["Illustrative", "Dotwork"]),
            ("Ichi Hatano", "ichi_hatano", "Tokyo", "Japan", 4.8, false,
                "Japanese irezumi master preserving traditional hand-poke (tebori) techniques.",
                "ichi_hatano", "190K", 500, 1200, 120, ["Japanese"]),
            ("Hanumantra", "hanumantra", "Berlin", "Germany", 4.7, false,
                "Sacred geometry and ornamental dotwork artist creating meditative body art.",
                "hanumantra", "310K", 300, 700, 155, ["Dotwork", "Geometric"]),
            ("Pis Saro", "pissaro_tattoo", "Sevastopol", "Russia", 4.8, false,
                "Botanical watercolor artist creating hyper-realistic flower and plant tattoos.",
                "pissaro_tattoo", "850K", 300, 700, 185, ["Watercolor", "Realism"]),
            ("Kaiyu Huang", "kaiyuhuang", "Taipei", "Taiwan", 4.7, false,
                "Fine line and minimalist artist known for ethereal, dream-like imagery.",
                "kaiyuhuang", "230K", 250, 600, 130, ["Fine line", "Watercolor"]),
            ("Manu Tattooer", "manutattooer", "Madrid", "Spain", 4.6, false,
                "Neo-traditional specialist with rich color palettes and bold, dynamic compositions.",
                "manutattooer", "175K", 200, 500, 160, ["Traditional"]),
            ("Aleksy Marcinow", "aleksymarcinow", "Warsaw", "Poland", 4.7, false,
                "Realism artist creating stunning black and grey portraits with incredible detail.",
                "aleksymarcinow", "145K", 200, 550, 120, ["Realism", "Black & grey"]),
            ("Cira Las Vegas", "ciralasvegastattoo", "Barcelona", "Spain", 4.8, false,
                "Vibrant illustrative artist known for kawaii and pop-culture inspired tattoos.",
                "ciralasvegastattoo", "390K", 250, 600, 175, ["Illustrative", "Anime"]),
            ("El Carlo", "elcarlo", "Berlin", "Germany", 4.6, false,
                "Black and grey realism master creating photorealistic portrait tattoos.",
                "elcarlo", "270K", 300, 750, 140, ["Realism", "Black & grey"]),
            ("Noel Badges Pugh", "noelbadgespugh", "Austin", "United States", 4.7, false,
                "Botanical illustrator-turned-tattoo artist creating delicate nature studies on skin.",
                "noelbadgespugh", "440K", 300, 700, 130, ["Fine line", "Illustrative"]),
            ("Marla Moon", "marla_moon", "Valencia", "Spain", 4.8, false,
                "Fine line and minimalist tattoo artist known for micro-realistic botanical designs.",
                "marla_moon", "620K", 200, 500, 195, ["Fine line", "Minimalist"]),
            ("Servadio", "servadio", "London", "United Kingdom", 4.7, false,
                "Hand-poke artist creating primitive and sacred blackwork designs without a machine.",
                "servadio", "320K", 300, 700, 110, ["Dotwork", "Geometric"]),
            ("Pony Reinhardt", "freeorgy", "Portland", "United States", 4.8, false,
                "Etching-style tattoos inspired by vintage scientific illustrations and occult imagery.",
                "freeorgy", "510K", 350, 850, 145, ["Illustrative", "Dotwork"]),
            ("Victor Portugal", "victorportugal", "Mexico City", "Mexico", 4.9, false,
                "Surrealist realism artist creating mind-bending morphed portrait tattoos.",
                "victorportugal", "470K", 400, 1000, 155, ["Realism"]),
            ("Momo", "maboroshinosakura", "Tokyo", "Japan", 4.8, false,
                "Delicate botanical and nature fine line artist creating ethereal flower compositions.",
                "maboroshinosakura", "380K", 350, 800, 140, ["Fine line", "Japanese"]),
            ("Tattoo Temple", "tattootemple", "Hong Kong", "China", 4.7, true,
                "Premier Hong Kong studio known for large-scale custom work across all styles.",
                "tattootemple", "120K", 300, 900, 450, ["Studio", "Japanese", "Realism", "Geometric"]),
            ("LTW Tattoo", "ltwtattoo", "Barcelona", "Spain", 4.8, true,
                "Barcelona collective with award-winning artists specializing in realism and fine line.",
                "ltwtattoo", "280K", 250, 700, 600, ["Studio", "Realism", "Fine line", "Dotwork"]),
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
                existing.ProfileImageUrl = $"/images/avatars/avatar-{(i % 30) + 1}.jpg";

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
                    ProfileImageUrl = $"/images/avatars/avatar-{(i % 30) + 1}.jpg",
                };
                foreach (var s in styles)
                    artist.ArtistStyles.Add(new ArtistStyle { StyleName = s });
                db.Artists.Add(artist);
            }
        }

        await db.SaveChangesAsync();

        await SeedFillerArtists(db);
        await SeedArtistPortfolioImages(db);
    }

    private static async Task SeedFillerArtists(AppDbContext db)
    {
        const int target = 50;
        var styleNames = new[]
        {
            "Fine line", "Traditional", "Black & grey", "Minimalist",
            "Neo Traditional", "Illustrative", "Geometric", "Watercolor",
            "Dotwork", "Anime", "Japanese"
        };

        var affinities = new Dictionary<string, string[]>
        {
            ["Fine line"] = ["Minimalist", "Dotwork", "Illustrative"],
            ["Traditional"] = ["Neo Traditional", "Japanese", "Illustrative"],
            ["Black & grey"] = ["Traditional", "Japanese", "Illustrative"],
            ["Minimalist"] = ["Fine line", "Geometric", "Dotwork"],
            ["Neo Traditional"] = ["Traditional", "Illustrative", "Japanese"],
            ["Illustrative"] = ["Fine line", "Watercolor", "Traditional"],
            ["Geometric"] = ["Dotwork", "Minimalist", "Fine line"],
            ["Watercolor"] = ["Illustrative", "Fine line", "Minimalist"],
            ["Dotwork"] = ["Geometric", "Minimalist", "Fine line"],
            ["Anime"] = ["Illustrative", "Watercolor", "Japanese"],
            ["Japanese"] = ["Traditional", "Black & grey", "Illustrative"],
        };

        var bios = new Dictionary<string, string>
        {
            ["Fine line"] = "Specializes in delicate single-needle work creating intricate, precise designs.",
            ["Traditional"] = "Master of bold lines and vivid colors rooted in classic tattooing traditions.",
            ["Black & grey"] = "Creates stunning monochromatic pieces with smooth gradients and deep contrast.",
            ["Minimalist"] = "Known for clean, understated designs that make powerful statements with minimal elements.",
            ["Neo Traditional"] = "Blends vintage tattoo tradition with modern detail, color depth, and artistic flair.",
            ["Illustrative"] = "Creates drawing-inspired body art merging realism with artistic illustration.",
            ["Geometric"] = "Designs precise mathematical patterns and sacred geometry compositions on skin.",
            ["Watercolor"] = "Paints flowing, vibrant designs that capture the beauty of watercolor on skin.",
            ["Dotwork"] = "Builds intricate patterns and images entirely from meticulously placed dots.",
            ["Anime"] = "Brings Japanese animation characters to life with bold colors and dynamic compositions.",
            ["Japanese"] = "Practices traditional irezumi with flowing compositions and rich cultural symbolism.",
        };

        var firstNames = new[]
        {
            "Alex", "Ariana", "Blake", "Bao", "Carlos", "Camille", "Dante", "Dina", "Elena", "Ethan",
            "Fabian", "Fiona", "Gabriel", "Grace", "Hiro", "Hannah", "Igor", "Ivy", "Jade", "Javier",
            "Kai", "Kira", "Leo", "Luna", "Marco", "Maya", "Nico", "Nadia", "Omar", "Olivia",
            "Pedro", "Priya", "Quinn", "Rafael", "Rosa", "Suki", "Siri", "Tomas", "Tara", "Uri",
            "Vera", "Vince", "Wren", "Xander", "Yuki", "Zara", "Aiden", "Bianca", "Cole", "Daria",
            "Eli", "Freya", "Gus", "Hana", "Iris", "Jin", "Koda", "Lena", "Malik", "Nova",
            "Orion", "Phoenix", "Raven", "Sky", "Thiago", "Uma", "Vega", "Wolf", "Yara", "Zen",
        };

        var lastNames = new[]
        {
            "Ink", "Stone", "Cross", "Rivers", "Moon", "Cruz", "Hart", "Wells", "Fox", "Knight",
            "Rhodes", "Storm", "Pierce", "Drake", "Silva", "Ortiz", "Lee", "Kim", "Chen", "Tanaka",
            "Park", "Wagner", "Berg", "Rossi", "Lopez", "Santos", "Liu", "Patel", "Sato", "Nakamura",
            "Cho", "Anders", "Bjork", "Larsen", "Costa", "Moreno", "Reyes", "Torres", "Vargas", "Mendez",
            "Weber", "Fischer", "Jung", "Hayashi", "Okafor", "Diaz", "Roth", "Crane", "Pine", "Ash",
            "Reed", "Frost", "Vale", "Marsh", "Wolfe", "Grant", "Shaw", "Bell", "Hayes", "Ward",
            "Ray", "Dunn", "West", "Nash", "Kirk", "Tate", "Lowe", "Page", "Blair", "Sage",
        };

        var locations = new (string City, string Country)[]
        {
            ("Miami", "United States"), ("Denver", "United States"), ("Seattle", "United States"),
            ("Nashville", "United States"), ("Atlanta", "United States"), ("Boston", "United States"),
            ("Dallas", "United States"), ("Detroit", "United States"), ("Honolulu", "United States"),
            ("Minneapolis", "United States"), ("Phoenix", "United States"), ("Charlotte", "United States"),
            ("London", "United Kingdom"), ("Manchester", "United Kingdom"), ("Edinburgh", "United Kingdom"),
            ("Paris", "France"), ("Lyon", "France"), ("Marseille", "France"),
            ("Berlin", "Germany"), ("Munich", "Germany"), ("Hamburg", "Germany"),
            ("Barcelona", "Spain"), ("Madrid", "Spain"), ("Seville", "Spain"),
            ("Milan", "Italy"), ("Rome", "Italy"), ("Naples", "Italy"),
            ("Tokyo", "Japan"), ("Osaka", "Japan"), ("Kyoto", "Japan"),
            ("Seoul", "South Korea"), ("Busan", "South Korea"),
            ("Taipei", "Taiwan"), ("Bangkok", "Thailand"),
            ("Melbourne", "Australia"), ("Sydney", "Australia"), ("Brisbane", "Australia"),
            ("Toronto", "Canada"), ("Montreal", "Canada"), ("Vancouver", "Canada"),
            ("Mexico City", "Mexico"), ("Guadalajara", "Mexico"),
            ("Sao Paulo", "Brazil"), ("Rio de Janeiro", "Brazil"),
            ("Buenos Aires", "Argentina"), ("Bogota", "Colombia"),
            ("Amsterdam", "Netherlands"), ("Copenhagen", "Denmark"), ("Stockholm", "Sweden"),
            ("Oslo", "Norway"), ("Lisbon", "Portugal"), ("Prague", "Czech Republic"),
            ("Budapest", "Hungary"), ("Athens", "Greece"), ("Istanbul", "Turkey"),
            ("Moscow", "Russia"), ("Dubai", "UAE"), ("Mumbai", "India"),
            ("Beijing", "China"), ("Shanghai", "China"), ("Singapore", "Singapore"),
        };

        var allStyleEntries = await db.ArtistStyles.ToListAsync();
        var counts = styleNames.ToDictionary(s => s, s => allStyleEntries.Count(a => a.StyleName == s));
        var existingSlugs = new HashSet<string>(await db.Artists.Select(a => a.Slug).ToListAsync());
        var maxOrder = await db.Artists.MaxAsync(a => (int?)a.OrderNo) ?? 0;

        var rng = new Random(777);
        var orderNo = maxOrder + 1;
        var nameIdx = 0;

        var sortedStyles = styleNames.OrderByDescending(s => target - counts[s]).ToArray();

        foreach (var style in sortedStyles)
        {
            while (counts[style] < target)
            {
                var secondary = affinities[style]
                    .Where(s => counts[s] < target)
                    .OrderByDescending(s => target - counts[s])
                    .FirstOrDefault() ?? affinities[style][0];

                var fn = firstNames[nameIdx % firstNames.Length];
                var ln = lastNames[(nameIdx / firstNames.Length + nameIdx) % lastNames.Length];
                var name = $"{fn} {ln}";
                var baseSlug = $"{fn.ToLower()}{ln.ToLower()}";
                var slug = baseSlug;
                var suffix = 1;
                while (existingSlugs.Contains(slug))
                {
                    slug = $"{baseSlug}{suffix}";
                    suffix++;
                }
                existingSlugs.Add(slug);

                var loc = locations[rng.Next(locations.Length)];
                var rating = Math.Round(4.2 + rng.NextDouble() * 0.7, 1);
                var priceMin = (100 + rng.Next(0, 350)) / 50 * 50;
                var priceMax = priceMin + (200 + rng.Next(0, 600)) / 50 * 50;
                var pieces = 50 + rng.Next(0, 350);
                var followersK = rng.Next(10, 800);

                var artist = new Artist
                {
                    Name = name, Slug = slug, City = loc.City, Country = loc.Country,
                    Rating = rating, IsStudio = false, OrderNo = orderNo++,
                    Bio = bios[style],
                    InstagramHandle = slug.Replace("-", ""),
                    Followers = $"{followersK}K",
                    PriceMin = priceMin,
                    PriceMax = priceMax,
                    PieceCount = pieces,
                    ProfileImageUrl = $"/images/avatars/avatar-{(nameIdx % 30) + 1}.jpg",
                };
                artist.ArtistStyles.Add(new ArtistStyle { StyleName = style });
                if (secondary != style)
                {
                    artist.ArtistStyles.Add(new ArtistStyle { StyleName = secondary });
                    counts[secondary]++;
                }
                db.Artists.Add(artist);

                counts[style]++;
                nameIdx++;
            }
        }

        await db.SaveChangesAsync();
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

        foreach (var artist in allArtists)
        {
            if (artist.PortfolioImages.Count > 0)
                db.ArtistPortfolioImages.RemoveRange(artist.PortfolioImages);
        }
        await db.SaveChangesAsync();

        var globalUsed = new HashSet<string>();

        for (int artistIdx = 0; artistIdx < allArtists.Count; artistIdx++)
        {
            var artist = allArtists[artistIdx];

            var artistStyleSlugs = artist.ArtistStyles
                .Where(s => s.StyleName != "Studio" && StyleNameToSlug.ContainsKey(s.StyleName))
                .Select(s => StyleNameToSlug[s.StyleName])
                .Distinct()
                .ToList();

            const int variantsPerStyle = 155;
            const int imageCount = 6;
            var chosen = new List<string>();
            var artistRng = new Random(artistIdx * 9973 + 7919);

            foreach (var slug in artistStyleSlugs)
            {
                var candidates = Enumerable.Range(1, variantsPerStyle)
                    .Select(n => $"/images/portfolio/{slug}-{n}.jpg")
                    .OrderBy(_ => artistRng.Next())
                    .ToList();

                foreach (var c in candidates)
                {
                    if (chosen.Count >= imageCount) break;
                    if (!globalUsed.Contains(c))
                    {
                        chosen.Add(c);
                        globalUsed.Add(c);
                    }
                }
            }

            if (chosen.Count < imageCount)
            {
                var otherSlugs = StyleSlugs.OrderBy(_ => artistRng.Next()).ToList();
                foreach (var slug in otherSlugs)
                {
                    if (chosen.Count >= imageCount) break;
                    var candidates = Enumerable.Range(1, variantsPerStyle)
                        .Select(n => $"/images/portfolio/{slug}-{n}.jpg")
                        .OrderBy(_ => artistRng.Next())
                        .ToList();
                    foreach (var c in candidates)
                    {
                        if (chosen.Count >= imageCount) break;
                        if (!globalUsed.Contains(c))
                        {
                            chosen.Add(c);
                            globalUsed.Add(c);
                        }
                    }
                }
            }

            // Fallback: if globalUsed exhausted all 600, pick unused-by-this-artist
            if (chosen.Count < imageCount)
            {
                var allImages = StyleSlugs
                    .SelectMany(s => Enumerable.Range(1, variantsPerStyle).Select(n => $"/images/portfolio/{s}-{n}.jpg"))
                    .OrderBy(_ => artistRng.Next())
                    .ToList();
                foreach (var img in allImages)
                {
                    if (chosen.Count >= imageCount) break;
                    if (!chosen.Contains(img))
                        chosen.Add(img);
                }
            }

            var basePrice = artist.PriceMin ?? 200;
            var maxPrice = artist.PriceMax ?? 800;

            for (int j = 0; j < chosen.Count; j++)
            {
                var price = basePrice + rng.Next(0, maxPrice - basePrice);
                price = (price / 10) * 10;

                artist.PortfolioImages.Add(new ArtistPortfolioImage
                {
                    ImageUrl = chosen[j],
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
