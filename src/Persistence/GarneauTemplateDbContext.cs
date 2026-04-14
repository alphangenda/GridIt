using System.Reflection;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Authentication;
using Domain.Entities.Books;
using Domain.Entities.Classes;
using Domain.Entities.Identity;
using Domain.Entities.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Persistence.Extensions;
using Persistence.Interceptors;

namespace Persistence;

public class GarneauTemplateDbContext : IdentityDbContext<User, Role, Guid,
    IdentityUserClaim<Guid>, UserRole,
    IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    private readonly AuditableAndSoftDeletableEntitySaveChangesInterceptor
        _auditableAndSoftDeletableEntitySaveChangesInterceptor = null!;

    private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor = null!;
    private readonly UserSaveChangesInterceptor _userSaveChangesInterceptor = null!;
    private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor = null!;

    public GarneauTemplateDbContext(
        DbContextOptions<GarneauTemplateDbContext> options,
        AuditableAndSoftDeletableEntitySaveChangesInterceptor auditableAndSoftDeletableEntitySaveChangesInterceptor,
        AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
        UserSaveChangesInterceptor userSaveChangesInterceptor,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor)
        : base(options)
    {
        _auditableAndSoftDeletableEntitySaveChangesInterceptor = auditableAndSoftDeletableEntitySaveChangesInterceptor;
        _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
        _userSaveChangesInterceptor = userSaveChangesInterceptor;
        _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
    }

    public DbSet<Administrator> Administrators { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Class> Classes { get; set; } = null!;
    public DbSet<CourseProgram> CoursePrograms { get; set; } = null!;
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Skill> Skills { get; set; } = null!;
    public DbSet<ExamSkill> ExamSkills { get; set; } = null!;
    public DbSet<ProgramSkill> ProgramSkills { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupClass> GroupClasses { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<SessionClass> SessionClasses { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public GarneauTemplateDbContext()
    {
    }

    public GarneauTemplateDbContext(DbContextOptions<GarneauTemplateDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Instant>()
            .HaveConversion<InstantToDateTimeOffsetConverter>();
        configurationBuilder.Properties<Instant?>()
            .HaveConversion<NullableInstantToDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global query to prevent loading soft-deleted entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            if (entityType.ClrType == typeof(User))
                continue;

            entityType.AddSoftDeleteQueryFilter();
        }

        builder.Entity<Book>().Property(b => b.Price).HasPrecision(18, 2);

        builder.Entity<Exam>()
            .HasOne<Class>()
            .WithMany()
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Student>()
            .HasOne<Class>()
            .WithMany()
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Class>()
            .HasOne(c => c.Program)
            .WithMany()
            .HasForeignKey(c => c.ProgramId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ExamSkill>()
            .HasOne<Exam>()
            .WithMany()
            .HasForeignKey(es => es.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExamSkill>()
            .HasOne<Skill>()
            .WithMany()
            .HasForeignKey(es => es.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProgramSkill>()
            .HasOne<CourseProgram>()
            .WithMany()
            .HasForeignKey(ps => ps.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProgramSkill>()
            .HasOne<Skill>()
            .WithMany()
            .HasForeignKey(ps => ps.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProgramSkill>()
            .HasIndex(ps => new { ps.ProgramId, ps.SkillId })
            .IsUnique();

        builder.Entity<Session>()
            .HasMany(s => s.Classes)
            .WithMany()
            .UsingEntity<SessionClass>(
                j => j
                    .HasOne<Class>()
                    .WithMany()
                    .HasForeignKey(sc => sc.ClassId)
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<Session>()
                    .WithMany()
                    .HasForeignKey(sc => sc.SessionId)
                    .OnDelete(DeleteBehavior.Cascade));

        builder.Entity<Group>().ToTable("groups");

        builder.Entity<GroupClass>()
            .ToTable("group_classes")
            .HasOne<Group>()
            .WithMany()
            .HasForeignKey(gc => gc.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GroupClass>()
            .HasOne<Class>()
            .WithMany()
            .HasForeignKey(gc => gc.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Exam>()
            .HasOne<Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(
            _auditableAndSoftDeletableEntitySaveChangesInterceptor,
            _auditableEntitySaveChangesInterceptor,
            _userSaveChangesInterceptor,
            _entitySaveChangesInterceptor);
    }

    public async Task<int> SaveChangesAsync(CancellationToken? cancellationToken = null)
    {
        return await base.SaveChangesAsync(cancellationToken ?? CancellationToken.None);
    }
}

internal class InstantToDateTimeOffsetConverter : ValueConverter<Instant, DateTimeOffset>
{
    public InstantToDateTimeOffsetConverter()
        : base(v => v.ToDateTimeOffset(), v => Instant.FromDateTimeOffset(v)) { }
}

internal class NullableInstantToDateTimeOffsetConverter : ValueConverter<Instant?, DateTimeOffset?>
{
    public NullableInstantToDateTimeOffsetConverter()
        : base(
            v => v == null ? (DateTimeOffset?)null : v.Value.ToDateTimeOffset(),
            v => v == null ? (Instant?)null : Instant.FromDateTimeOffset(v.Value)) { }
}