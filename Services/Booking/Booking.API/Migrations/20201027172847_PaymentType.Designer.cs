﻿// <auto-generated />
using System;
using Booking.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Booking.API.Migrations
{
    [DbContext(typeof(BookingContext))]
    [Migration("20201027172847_PaymentType")]
    partial class PaymentType
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Booking.API.Idempotency.EventRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Time")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("EventRequests");
                });

            modelBuilder.Entity("Booking.API.Model.Booking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("CheckIn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CheckOut")
                        .HasColumnType("datetime2");

                    b.Property<string>("CustomerEmail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("CustomerId")
                        .HasColumnType("int");

                    b.Property<string>("CustomerName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CustomerPhone")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EnteredDateTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("HotelId")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalFee")
                        .HasColumnType("decimal(18,4)");

                    b.HasKey("Id");

                    b.ToTable("Bookings");
                });

            modelBuilder.Entity("Booking.API.Model.BookingPayment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,4)");

                    b.Property<int>("BookingId")
                        .HasColumnType("int");

                    b.Property<DateTime>("EnteredDateTime")
                        .HasColumnType("datetime2");

                    b.Property<int?>("PaymentMethodId")
                        .HasColumnType("int");

                    b.Property<string>("PaymentMode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("PaymentTypeId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BookingId");

                    b.HasIndex("PaymentMethodId");

                    b.HasIndex("PaymentTypeId");

                    b.ToTable("BookingPayments");
                });

            modelBuilder.Entity("Booking.API.Model.PaymentMethod", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("CardHolderName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CardNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Expiry")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("PaymentTypeId")
                        .HasColumnType("int");

                    b.Property<int>("SecurityNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PaymentTypeId");

                    b.ToTable("PaymentMethod");
                });

            modelBuilder.Entity("Booking.API.Model.PaymentType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Icon")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("PaymentType");
                });

            modelBuilder.Entity("Booking.API.Model.BookingPayment", b =>
                {
                    b.HasOne("Booking.API.Model.Booking", "Booking")
                        .WithMany("BookingPayments")
                        .HasForeignKey("BookingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Booking.API.Model.PaymentMethod", "PaymentMethod")
                        .WithMany("BookingPayments")
                        .HasForeignKey("PaymentMethodId");

                    b.HasOne("Booking.API.Model.PaymentType", null)
                        .WithMany("BookingPayments")
                        .HasForeignKey("PaymentTypeId");
                });

            modelBuilder.Entity("Booking.API.Model.PaymentMethod", b =>
                {
                    b.HasOne("Booking.API.Model.PaymentType", "PaymentType")
                        .WithMany()
                        .HasForeignKey("PaymentTypeId");
                });
#pragma warning restore 612, 618
        }
    }
}
