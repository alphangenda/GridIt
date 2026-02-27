using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCompetencesEtExamenCompetences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSkills_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSkills_ExamId",
                table: "ExamSkills",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSkills_SkillId",
                table: "ExamSkills",
                column: "SkillId");

            migrationBuilder.Sql(@"
                INSERT INTO Skills (Id, Label) VALUES
                    (NEWID(), N'Configuration et qualité générale'),
                    (NEWID(), N'Administration Django'),
                    (NEWID(), N'Templates et interface utilisateur'),
                    (NEWID(), N'API externe et fonctionnalités avancées'),
                    (NEWID(), N'Formulaires et validations'),
                    (NEWID(), N'Vues principales et logique'),
                    (NEWID(), N'Authentification et autorisations'),
                    (NEWID(), N'Modèles et base de données'),
                    (NEWID(), N'Analyser le projet de développement');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSkills");

            migrationBuilder.DropTable(
                name: "Skills");
        }
    }
}
