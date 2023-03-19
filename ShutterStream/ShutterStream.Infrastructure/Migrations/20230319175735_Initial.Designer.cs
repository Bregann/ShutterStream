﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using ShutterStream.Infrastructure.Database.Context;

#nullable disable

namespace ShutterStream.Infrastructure.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20230319175735_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Albums", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AlbumName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("Likes")
                        .HasColumnType("bigint");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Views")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("Username");

                    b.ToTable("Albums");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Config", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AzureBlobConnectionString")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProjectMonitorApiKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Config");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            AzureBlobConnectionString = "",
                            ProjectMonitorApiKey = ""
                        });
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Images", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AlbumId")
                        .HasColumnType("integer");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ImageName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Likes")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("UploadDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("Views")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AlbumId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Sessions", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("SessionId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Username");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Users", b =>
                {
                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.Property<bool>("CanUploadFiles")
                        .HasColumnType("boolean");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("HashedPassword")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("LastActivity")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Username");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Albums", b =>
                {
                    b.HasOne("ShutterStream.Infrastructure.Database.Models.Users", "User")
                        .WithMany("Albums")
                        .HasForeignKey("Username")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Images", b =>
                {
                    b.HasOne("ShutterStream.Infrastructure.Database.Models.Albums", "Album")
                        .WithMany("Images")
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Sessions", b =>
                {
                    b.HasOne("ShutterStream.Infrastructure.Database.Models.Users", "User")
                        .WithMany("Sessions")
                        .HasForeignKey("Username")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Albums", b =>
                {
                    b.Navigation("Images");
                });

            modelBuilder.Entity("ShutterStream.Infrastructure.Database.Models.Users", b =>
                {
                    b.Navigation("Albums");

                    b.Navigation("Sessions");
                });
#pragma warning restore 612, 618
        }
    }
}