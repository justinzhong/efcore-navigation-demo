using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        public class Category
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public Category ParentCategory { get; set; }

            public IEnumerable<Category> ChildCategories { get; set; }
        }

        public class BusinessListing
        {
            public int Id { get; set; }

            public Category Category { get; set; }

            public string Title { get; set; }

            public DateTime CreateDate { get; set; }

            public DateTime UpdateDate { get; set; }
        }

        public class ListingDbContext : DbContext
        {
            public DbSet<BusinessListing> Listings { get; set; }

            public DbSet<Category> Categories { get; set; }

            public ListingDbContext(DbContextOptions<ListingDbContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<BusinessListing>(BuildBusinessListingTable);
                modelBuilder.Entity<Category>(BuildCategoryTable);
            }

            private void BuildBusinessListingTable(EntityTypeBuilder<BusinessListing> builder)
            {
                // Table Name "Listings"
                builder.ToTable("Listings");

                // Record Id
                builder.HasKey(row => row.Id);
                builder.Property(row => row.Id)
                    .UseSqlServerIdentityColumn();

                builder.Property(ci => ci.CreateDate)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAdd()
                    .IsRequired();

                // Category table relationship
                builder.Property<int>("CategoryId");

                builder.HasOne(row => row.Category)
                    .WithOne()
                    .HasForeignKey<BusinessListing>("CategoryId");
            }

            private void BuildCategoryTable(EntityTypeBuilder<Category> builder)
            {
                // Record Id
                builder.HasKey(row => row.Id);
                builder.Property(row => row.Id)
                    .UseSqlServerIdentityColumn();

                builder.Property<int?>("ParentId");

                builder.HasOne(row => row.ParentCategory)
                    .WithMany(row => row.ChildCategories)
                    .HasForeignKey("ParentId");
            }
        }

        static void Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ListingDbContext>();
            optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=testdb;persist security info=True;Integrated Security=true;");

            var context = new ListingDbContext(optionsBuilder.Options);

            var fromDate = DateTime.Parse("2019-01-01 00:00:00");
            var toDate = fromDate.AddMonths(1);

            // #1 Include() only
            var includeOnly = context.Listings
                .Include(listing => listing.Category);

            var includeOnlyResults = includeOnly.OrderBy(listing => listing.UpdateDate)
                .Where(listing => listing.UpdateDate > fromDate && listing.UpdateDate < toDate)
                .Skip(0).Take(100)
                .ToArray();

            Console.WriteLine($"#Scenario One - Include() only");
            Console.WriteLine($"total # of listings: {includeOnlyResults.Length}, total # of category navigation property loaded: {includeOnlyResults.Count(l => l.Category != null)}");
            Console.WriteLine($"missing categories: {includeOnlyResults.Count(l => l.Category == null)}");

            // #2 Include() with AsNoTracking()
            var asNoTracking = context.Listings
                .AsNoTracking()
                .Include(listing => listing.Category);

            var asNoTrackingResults = asNoTracking.OrderBy(listing => listing.UpdateDate)
                .Where(listing => listing.UpdateDate > fromDate && listing.UpdateDate < toDate)
                .Skip(0).Take(100)
                .ToArray();

            Console.WriteLine();
            Console.WriteLine($"#Scenario Two - with AsNoTracking()");
            Console.WriteLine($"total # of listings: {asNoTrackingResults.Length}, total # of category navigation property loaded: {asNoTrackingResults.Count(l => l.Category != null)}");
            Console.WriteLine($"missing categories: {asNoTrackingResults.Count(l => l.Category == null)}");

            Console.ReadLine();
        }
    }
}
