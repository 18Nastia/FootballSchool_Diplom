using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

public partial class FootballSchoolContext : DbContext
{
    public FootballSchoolContext()
    {
    }

    public FootballSchoolContext(DbContextOptions<FootballSchoolContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Coach> Coaches { get; set; }

    public virtual DbSet<Facility> Facilities { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Progress> Progresses { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Training> Training { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-56H7VB7\\SQLEXPRESS;Database=FootballSchool;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__57FA4934437C3D8E");

            entity.HasOne(d => d.Student).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Student");

            entity.HasOne(d => d.Training).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Training");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("PK__Branch__12CEB04107BE5E5D");
        });

        modelBuilder.Entity<Coach>(entity =>
        {
            entity.HasKey(e => e.CoachId).HasName("PK__Coach__BDB636274675589F");
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Facility__CEAA23C5BD2B8BF4");

            entity.HasOne(d => d.Branch).WithMany(p => p.Facilities).HasConstraintName("FK_Facility_Branch");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__DA6C7FE17E85319F");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Payments).HasConstraintName("FK_Payment_Subscription");
        });

        modelBuilder.Entity<Progress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PK__Progress__D558799A376577B4");

            entity.HasOne(d => d.Student).WithMany(p => p.Progresses).HasConstraintName("FK_Progress_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__A2F4E9AC4021C8FB");

            entity.HasOne(d => d.Team).WithMany(p => p.Students)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Student_Team");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__518059B16793CE27");

            entity.HasOne(d => d.Student).WithMany(p => p.Subscriptions).HasConstraintName("FK_Subscription_Student");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PK__Team__02215C0AA6728748");
        });

        modelBuilder.Entity<Training>(entity =>
        {
            entity.HasKey(e => e.TrainingId).HasName("PK__Training__EF9C38168C9E7A2D");

            entity.HasOne(d => d.Coach).WithMany(p => p.Training)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Training_Coach");

            entity.HasOne(d => d.Facility).WithMany(p => p.Training).HasConstraintName("FK_Training_Facility");

            entity.HasOne(d => d.Team).WithMany(p => p.Training)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Training_Team");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
