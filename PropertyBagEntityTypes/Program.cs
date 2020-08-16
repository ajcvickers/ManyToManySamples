using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

// This is not a many-to-many example, but it illustrates using property bag entity types, which are the
// default backing types for many-to-many join tables

// Demonstrates property bags end-to-end
// Notice that the entities do not have CLR types--they represented as Dictionary "property bags".

public class ProductsContext : DbContext
{
    public DbSet<Dictionary<string, object>> Products => Set<Dictionary<string, object>>("Product");
    public DbSet<Dictionary<string, object>> Categories => Set<Dictionary<string, object>>("Category");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            .LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted })
            .EnableSensitiveDataLogging()
            .UseSqlite("Data Source = test.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SharedTypeEntity<Dictionary<string, object>>("Category", b =>
        {
            b.IndexerProperty<int>("Id").ValueGeneratedNever();
            b.IndexerProperty<string>("Name").IsRequired();
            b.IndexerProperty<string>("Description");
        });

        modelBuilder.SharedTypeEntity<Dictionary<string, object>>("Product", b =>
        {
            b.IndexerProperty<int>("Id").ValueGeneratedNever();
            b.IndexerProperty<int>("CategoryId");
            b.IndexerProperty<string>("Name").IsRequired(); // Can configure indexer properties like normal properties
            b.IndexerProperty<string>("Description");
            b.IndexerProperty<decimal>("Price");
            b.HasOne("Category", null).WithMany();
        });
    }
}

public static class Program
{
    public static void Main()
    {
        using (var context = new ProductsContext())
        {
            // Show the EF model
            // Notice that tables are created for Products and Categories, just as for normal entity types
            Console.WriteLine();
            Console.WriteLine("EF model is:");
            Console.WriteLine(context.Model.ToDebugString(MetadataDebugStringOptions.ShortDefault));
            Console.WriteLine();

            // Create a clean database each time
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var beverages = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Beverages" },
                { "Description", "Stuff to sip on" }
            };

            var foods = new Dictionary<string, object>
            {
                { "Id", 2 },
                { "Name", "Food" },
                { "Description", "Stuff to nibble on" }
            };

            var marmite = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "CategoryId", 1 },
                { "Name", "Marmite" },
                { "Description", "Yeasty stuff" },
                { "Price", 5.99m }
            };

            var bovril = new Dictionary<string, object>
            {
                { "Id", 2 },
                { "CategoryId", 1 },
                { "Name", "Bovril" },
                { "Description", "Beefy stuff" },
                { "Price", 4.99m }
            };

            context.Categories.AddRange(beverages, foods); // Must add to the DbSet not just AddRange on the context
            context.Products.AddRange(marmite, bovril);
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
                Console.WriteLine($"Product \"{product["Name"]}\" belongs to category {category["Name"]}");
            }
        }

        using (var context = new ProductsContext())
        {
            // We can query for property bags in the normal way--we use EF.Property here, just like for shadow props
            var marmite = context.Products.Single(p => EF.Property<string>(p, "Name") == "Marmite");
            var food = context.Categories.Single(p => EF.Property<string>(p, "Name") == "Food");

            // Can update properties, including changing relationships by FK (Navigations not supported yet.)
            marmite["CategoryId"] = food["Id"];
            marmite["Description"] = "Yummy yeasty stuff for Toast!";

            // Show what the state manager is tracking
            Console.WriteLine();
            Console.WriteLine("DbContext is tracking:");
            Console.WriteLine(context.ChangeTracker.ToDebugString(ChangeTrackerDebugStringOptions.LongDefault, 2));

            context.SaveChanges();
        }
    }
}
