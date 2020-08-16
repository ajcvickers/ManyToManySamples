using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

// This is not a many-to-many example, but it illustrates using indexer properties, which
// are used by property bag entity types, which are in turn used by many-to-many

// This is a simple Product/Category example where some property values of `Product` are stored in
// a dictionary, and EF reads and writes them through the indexer property.

public class Product
{
    private readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();

    public int Id { get; set; }
    public string Name { get; set; }

    // This is the indexer; it reads and writes name/value pairs
    public object this[string propertyName]
    {
        get => _propertyValues[propertyName];
        set => _propertyValues[propertyName] = value;
    }

    public Category Category { get; set; }
}

public class Category
{

    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public ICollection<Product> Products { get; set; }
}

public class ProductsContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            .LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted })
            .EnableSensitiveDataLogging()
            .UseSqlite("Data Source = test.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.Property(e => e.Id).ValueGeneratedNever();

            // Indexer properties are defined with a name and type
            b.IndexerProperty<string>("Description");
            b.IndexerProperty<decimal>("Price");
            b.IndexerProperty<int>("CategoryId"); // Can put FKs here
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.Property(e => e.Id).ValueGeneratedNever();
        });
    }
}

public static class Program
{
    public static void Main()
    {
        using (var context = new ProductsContext())
        {
            // Create a clean database each time
            // Notice that indexer properties result in columns in the database, like normal properties
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var beverages = new Category
            {
                Id = 1,
                Name = "Beverages",
                Description = "Stuff to sip on"
            };

            var foods = new Category
            {
                Id = 2,
                Name = "Food",
                Description = "Stuff to nibble on"
            };

            var marmite = new Product
            {
                Id = 1,
                Name = "Marmite",
                ["Description"] = "Yeasty stuff", // Values set using the indexer
                ["Price"] = 5.99m,
                ["CategoryId"] = 1
            };

            var bovril = new Product
            {
                Id = 2,
                Name = "Marmite",
                ["Description"] = "Beefy stuff",
                ["Price"] = 4.99m,
                ["CategoryId"] = 1
            };

            context.AddRange(beverages, foods, marmite, bovril);

            context.SaveChanges();
        }

        using (var context = new ProductsContext())
        {
            var products = context.Products.ToList();

            // Show what we loaded
            Console.WriteLine();
            Console.WriteLine();
            foreach (var product in products)
            {
                var category = context.Categories.Find(product["CategoryId"]);
                Console.WriteLine($"Product \"{product.Name}\" belongs to category {category.Name}");
            }
        }
    }
}
