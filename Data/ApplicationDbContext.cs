using BasisBank.Identity.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BasisBank.Identity.Api.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int> {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) {
        }

        public DbSet<AuthTicket> AuthTickets { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            // ქართული ენის მხარდაჭერა და სორტირება
            builder.UseCollation("Georgian_Modern_Sort_CI_AS");

            // Identity ცხრილების სახელების მოდიფიკაცია
            builder.Entity<ApplicationUser>(entity => { entity.ToTable(name: "Users"); });
            builder.Entity<IdentityRole<int>>(entity => { entity.ToTable(name: "Roles"); });
            builder.Entity<IdentityUserRole<int>>(entity => { entity.ToTable("UserRoles"); });
            builder.Entity<IdentityUserClaim<int>>(entity => { entity.ToTable("UserClaims"); });
            builder.Entity<IdentityUserLogin<int>>(entity => { entity.ToTable("UserLogins"); });
            builder.Entity<IdentityRoleClaim<int>>(entity => { entity.ToTable("RoleClaims"); });
            builder.Entity<IdentityUserToken<int>>(entity => { entity.ToTable("UserTokens"); });
            builder.Entity<RefreshToken>(entity => {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.User)
                      .WithMany(p => p.RefreshTokens)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            // AuthTicket ცხრილის კონფიგურაცია
            builder.Entity<AuthTicket>(entity => {
                entity.ToTable("AuthTickets");
                entity.HasKey(e => e.Id);

                // HashedOtp-სთვის გამოვყოთ საკმარისი ადგილი (Base64 HMACSHA256-ისთვის)
                entity.Property(e => e.HashedOtp)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Type)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired();

                // ინდექსი UserId-ზე ძებნის დასაჩქარებლად
                entity.HasIndex(e => e.UserId);
            });
        }
    }
}