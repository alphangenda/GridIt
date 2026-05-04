using Domain.Constants.User;
using Domain.Entities;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence;

public class GarneauTemplateDbContextInitializer
{
    private const string MemberEmail = "member@gmail.com";
    private const string AdminEmail = "admin@gmail.com";
    private const string Password = "Qwerty123!";

    private readonly ILogger<GarneauTemplateDbContextInitializer> _logger;
    private readonly GarneauTemplateDbContext _context;
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;

    public GarneauTemplateDbContextInitializer(ILogger<GarneauTemplateDbContextInitializer> logger,
        GarneauTemplateDbContext context,
        RoleManager<Role> roleManager,
        UserManager<User> userManager)
    {
        _logger = logger;
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
            await EnsureRawTablesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedRoles();
            await SeedAdmins();
            await SeedMembers();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task EnsureRawTablesAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS criteria (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                exam_skill_id UUID NOT NULL,
                label VARCHAR(500) NOT NULL,
                total_value INT NOT NULL,
                position INT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS criterion_weights (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                criterion_id UUID NOT NULL,
                weight VARCHAR(10) NOT NULL,
                value DECIMAL(10, 2) NOT NULL,
                description VARCHAR(500) NULL,
                is_enabled BOOL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS default_criterion_letters (
                letter VARCHAR(5) NOT NULL PRIMARY KEY,
                description VARCHAR(255) NOT NULL,
                default_percent INT NOT NULL,
                is_enabled BOOL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS default_selected_skills (
                skill_id UUID NOT NULL PRIMARY KEY,
                is_selected BOOL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS class_skills (
                class_id UUID NOT NULL,
                skill_id UUID NOT NULL,
                PRIMARY KEY (class_id, skill_id)
            );

            CREATE TABLE IF NOT EXISTS exam_groups (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                exam_id UUID NOT NULL,
                class_id UUID NOT NULL,
                name VARCHAR(255) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS exam_group_students (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                group_id UUID NOT NULL,
                number VARCHAR(100) NOT NULL,
                first_name VARCHAR(255) NOT NULL,
                last_name VARCHAR(255) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS student_evaluations (
                id UUID NOT NULL PRIMARY KEY,
                exam_id UUID NOT NULL,
                student_id VARCHAR(255) NOT NULL,
                competency_id VARCHAR(255) NOT NULL,
                grade CHAR(1) NULL,
                comment TEXT NULL,
                CONSTRAINT uq_student_eval UNIQUE (exam_id, student_id, competency_id)
            );

            CREATE TABLE IF NOT EXISTS student_criterion_evaluations (
                id UUID NOT NULL PRIMARY KEY,
                exam_id UUID NOT NULL,
                student_id VARCHAR(255) NOT NULL,
                criterion_id UUID NOT NULL,
                grade CHAR(1) NULL,
                comment TEXT NULL,
                CONSTRAINT uq_student_crit_eval UNIQUE (exam_id, student_id, criterion_id)
            );

            CREATE TABLE IF NOT EXISTS student_exam_videos (
                id UUID NOT NULL PRIMARY KEY,
                exam_id UUID NOT NULL,
                student_id VARCHAR(255) NOT NULL,
                video_url VARCHAR(500) NULL,
                CONSTRAINT uq_student_exam_video UNIQUE (exam_id, student_id)
            );
        ");

        await _context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO default_criterion_letters (letter, description, default_percent, is_enabled)
            SELECT v.letter, v.description, v.default_percent, v.is_enabled
            FROM (VALUES
                ('A', 'Très bien', 100, true),
                ('B', 'Bien', 75, true),
                ('C', 'Moyen', 60, true),
                ('D', 'Passable', 40, true),
                ('E', 'Faible', 10, true),
                ('F', 'Insuffisant', 0, true)
            ) AS v(letter, description, default_percent, is_enabled)
            WHERE NOT EXISTS (SELECT 1 FROM default_criterion_letters);
        ");
    }

    private async Task SeedRoles()
    {
        if (!_roleManager.RoleExistsAsync(Roles.ADMINISTRATOR).Result)
            await _roleManager.CreateAsync(new Role { Name = Roles.ADMINISTRATOR, NormalizedName = Roles.ADMINISTRATOR.Normalize() });

        if (!_roleManager.RoleExistsAsync(Roles.MEMBER).Result)
            await _roleManager.CreateAsync(new Role { Name = Roles.MEMBER, NormalizedName = Roles.MEMBER.Normalize() });
    }

    private async Task SeedAdmins()
    {
        var user = await _userManager.FindByEmailAsync(AdminEmail);
        if (user != null)
            return;

        user = BuildUser(AdminEmail);
        var result = await _userManager.CreateAsync(user, Password);

        if (result.Succeeded)
            await _userManager.AddToRoleAsync(user, Roles.ADMINISTRATOR);
        else
            throw new Exception($"Could not seed/create {Roles.ADMINISTRATOR} user.");


        var admin = new Administrator("Super", "Admin");
        admin.SetUser(user);
        _context.Administrators.Add(admin);
        await _context.SaveChangesAsync();
    }

    private async Task SeedMembers()
    {
        var user = await _userManager.FindByEmailAsync(MemberEmail);
        if (user != null)
            return;

        user = BuildUser(MemberEmail);
        var result = await _userManager.CreateAsync(user, Password);

        if (result.Succeeded)
            await _userManager.AddToRoleAsync(user, Roles.MEMBER);
        else
            throw new Exception($"Could not seed/create {Roles.MEMBER} user.");

        var existingMember = _context.Members.IgnoreQueryFilters().FirstOrDefault(x => x.User.Id == user.Id);
        if (existingMember is { Active: true })
            return;

        if (existingMember == null)
        {
            var member = new Member("John", "Doe", 1, "123, my street", "Quebec", "A1A 1A1");
            member.SetUser(user);
            _context.Members.Add(member);
            await _context.SaveChangesAsync();
        }
        else if (!existingMember.Active)
        {
            existingMember.Activate();
            _context.Members.Update(existingMember);
            await _context.SaveChangesAsync();
        }
    }

    private User BuildUser(string email)
    {
        return new User
        {
            Email = email,
            UserName = email,
            NormalizedEmail = email.Normalize(),
            NormalizedUserName = email,
            PhoneNumber = "555-555-5555",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = false
        };
    }
}
