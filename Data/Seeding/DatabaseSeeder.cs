using ECommerceApp.RyanW84.Data.Models;

namespace ECommerceApp.RyanW84.Data.Seeding;

/// <summary>
/// Handles database seeding with initial test data for products, categories, and sales.
/// Follows SRP by isolating seed data logic from DbContext.
/// </summary>
public static class DatabaseSeeder
{
    public static void SeedDatabase(ECommerceDbContext context)
    {
        if (HasExistingData(context))
        {
            ClearExistingData(context);
        }

        var categories = SeedCategories(context);
        var products = SeedProducts(context, categories);
        SeedSales(context, products);

        context.SaveChanges();
    }

    private static bool HasExistingData(ECommerceDbContext context)
    {
        return context.Categories.Any() || context.Products.Any() || context.Sales.Any();
    }

    private static void ClearExistingData(ECommerceDbContext context)
    {
        context.Sales.RemoveRange(context.Sales);
        context.SaleItems.RemoveRange(context.SaleItems);
        context.Products.RemoveRange(context.Products);
        context.Categories.RemoveRange(context.Categories);
        context.SaveChanges();
    }

    private static List<Category> SeedCategories(ECommerceDbContext context)
    {
        var categories = new[]
        {
            new Category { Name = "Electronics", Description = "Electronic devices and gadgets" },
            new Category { Name = "Clothing", Description = "Apparel and fashion items" },
            new Category { Name = "Books", Description = "Books and publications" },
            new Category
            {
                Name = "Home & Garden",
                Description = "Home improvement and gardening supplies",
            },
            new Category { Name = "Sports", Description = "Sports and outdoor equipment" },
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();
        return categories.ToList();
    }

    private static List<Product> SeedProducts(ECommerceDbContext context, List<Category> categories)
    {
        var electronics = categories[0];
        var clothing = categories[1];
        var books = categories[2];
        var homeAndGarden = categories[3];
        var sports = categories[4];

        var products = new[]
        {
            new Product
            {
                Name = "Laptop",
                Description = "High-performance laptop",
                Price = 999.99m,
                Stock = 10,
                IsActive = true,
                CategoryId = electronics.CategoryId,
            },
            new Product
            {
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = 29.99m,
                Stock = 50,
                IsActive = true,
                CategoryId = electronics.CategoryId,
            },
            new Product
            {
                Name = "Keyboard",
                Description = "Mechanical keyboard",
                Price = 79.99m,
                Stock = 30,
                IsActive = true,
                CategoryId = electronics.CategoryId,
            },
            new Product
            {
                Name = "Monitor",
                Description = "27-inch 4K monitor",
                Price = 399.99m,
                Stock = 15,
                IsActive = true,
                CategoryId = electronics.CategoryId,
            },
            new Product
            {
                Name = "T-Shirt",
                Description = "Cotton t-shirt",
                Price = 19.99m,
                Stock = 100,
                IsActive = true,
                CategoryId = clothing.CategoryId,
            },
            new Product
            {
                Name = "Jeans",
                Description = "Classic blue jeans",
                Price = 49.99m,
                Stock = 80,
                IsActive = true,
                CategoryId = clothing.CategoryId,
            },
            new Product
            {
                Name = "Jacket",
                Description = "Winter jacket",
                Price = 89.99m,
                Stock = 25,
                IsActive = true,
                CategoryId = clothing.CategoryId,
            },
            new Product
            {
                Name = "C# Programming",
                Description = "Advanced C# guide",
                Price = 39.99m,
                Stock = 40,
                IsActive = true,
                CategoryId = books.CategoryId,
            },
            new Product
            {
                Name = "Design Patterns",
                Description = "GOF design patterns",
                Price = 44.99m,
                Stock = 35,
                IsActive = true,
                CategoryId = books.CategoryId,
            },
            new Product
            {
                Name = "Clean Code",
                Description = "Writing clean code",
                Price = 34.99m,
                Stock = 50,
                IsActive = true,
                CategoryId = books.CategoryId,
            },
            new Product
            {
                Name = "Garden Hose",
                Description = "50ft expandable hose",
                Price = 34.99m,
                Stock = 20,
                IsActive = true,
                CategoryId = homeAndGarden.CategoryId,
            },
            new Product
            {
                Name = "Lawn Mower",
                Description = "Electric lawn mower",
                Price = 249.99m,
                Stock = 8,
                IsActive = true,
                CategoryId = homeAndGarden.CategoryId,
            },
            new Product
            {
                Name = "Paint Set",
                Description = "Interior paint set",
                Price = 79.99m,
                Stock = 15,
                IsActive = true,
                CategoryId = homeAndGarden.CategoryId,
            },
            new Product
            {
                Name = "Tool Set",
                Description = "150-piece tool set",
                Price = 89.99m,
                Stock = 12,
                IsActive = true,
                CategoryId = homeAndGarden.CategoryId,
            },
            new Product
            {
                Name = "Basketball",
                Description = "Official size basketball",
                Price = 29.99m,
                Stock = 25,
                IsActive = true,
                CategoryId = sports.CategoryId,
            },
            new Product
            {
                Name = "Yoga Mat",
                Description = "Non-slip yoga mat",
                Price = 24.99m,
                Stock = 40,
                IsActive = true,
                CategoryId = sports.CategoryId,
            },
            new Product
            {
                Name = "Running Shoes",
                Description = "Professional running shoes",
                Price = 119.99m,
                Stock = 30,
                IsActive = true,
                CategoryId = sports.CategoryId,
            },
            new Product
            {
                Name = "Tennis Racket",
                Description = "Carbon fiber racket",
                Price = 149.99m,
                Stock = 10,
                IsActive = true,
                CategoryId = sports.CategoryId,
            },
        };

        context.Products.AddRange(products);
        context.SaveChanges();
        return products.ToList();
    }

    private static void SeedSales(ECommerceDbContext context, List<Product> products)
    {
        var random = new Random(42);
        const int saleCount = 50;

        for (int i = 0; i < saleCount; i++)
        {
            var saleDate = DateTime.UtcNow.AddDays(-random.Next(0, 730));
            var saleProducts = products
                .OrderBy(_ => random.Next())
                .Take(random.Next(1, 6))
                .ToList();
            var saleItems = new List<SaleItem>();
            decimal total = 0;

            foreach (var product in saleProducts)
            {
                var quantity = random.Next(1, 4);
                var lineTotal = product.Price * quantity;
                total += lineTotal;

                saleItems.Add(
                    new SaleItem
                    {
                        ProductId = product.ProductId,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                    }
                );
            }

            var sale = new Sale
            {
                SaleDate = saleDate,
                CustomerName = $"Customer {i + 1}",
                CustomerEmail = $"customer{i + 1}@example.com",
                CustomerAddress = $"{i + 1} Main St",
                TotalAmount = total,
                SaleItems = saleItems,
            };

            context.Sales.Add(sale);
        }
    }
}
