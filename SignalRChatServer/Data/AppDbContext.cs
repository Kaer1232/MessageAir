using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Models;
using System;

namespace SignalRChatServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PrivateMessageModel> PrivateMessages { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<MessageModel> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<MessageModel>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<PrivateMessageModel>(entity =>
            {
                entity.HasOne(m => m.FromUser)
                    .WithMany()
                    .HasForeignKey(m => m.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.ToUser)
                    .WithMany()
                    .HasForeignKey(m => m.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(m => m.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(m => m.FileData)
                    .HasColumnType("varbinary(max)");
            });


            modelBuilder.Entity<PrivateMessageModel>(entity =>
            {
                entity.HasOne(m => m.FromUser)
                    .WithMany()
                    .HasForeignKey(m => m.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.ToUser)
                    .WithMany()
                    .HasForeignKey(m => m.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(m => m.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}