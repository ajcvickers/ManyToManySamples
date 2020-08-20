using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

// All I did to make this TPT (as opposed to the default, TPH) is add the [Table()] attributes.
// (Could also have done this with `ToTable` in the ModelBuilder.)

[Table("Animals")]
public class Animal
{
    public int Id { get; set; }
    public string Species { get; set; }
}

[Table("Pets")]
public class Pet : Animal
{
    public string Name { get; set; }
}

[Table("Cats")]
public class Cat : Pet
{
    public string EdcuationLevel { get; set; }
}

[Table("Dogs")]
public class Dog : Pet
{
    public string FavoriteToy { get; set; }
}

public class CrittersContext : DbContext
{
    public DbSet<Animal> Animals { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Cat> Cats { get; set; }
    public DbSet<Dog> Dogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            .LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted })
            .EnableSensitiveDataLogging()
            .UseSqlite("Data Source = test.db");
}

public static class Program
{
    public static void Main()
    {
        using (var context = new CrittersContext())
        {
            context.Database.EnsureDeleted();
            // Look at the tables created.
            context.Database.EnsureCreated();

            context.AddRange(
                new Dog {Name = "Olive", FavoriteToy = "Mr. Hedgehog"},
                new Dog {Name = "Toast", FavoriteToy = "The snake!"},
                new Cat {Name = "Smokey"},
                new Cat {Name = "Baxter", EdcuationLevel = "Law degree"},
                new Cat {Name = "Alice", EdcuationLevel = "Business school"},
                new Cat {Name = "Macavity", EdcuationLevel = "Kindergarten"});

            context.SaveChanges();
        }

        using (var context = new CrittersContext())
        {
            // Look at the query in the logs
            var communities = context.Animals.OfType<Cat>().Where(e => e.EdcuationLevel == "Law degree").ToList();
        }
    }
}
