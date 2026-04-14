using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "program_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_skills", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_skills_course_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "course_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_program_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_program_skills_program_id_skill_id",
                table: "program_skills",
                columns: new[] { "program_id", "skill_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_program_skills_skill_id",
                table: "program_skills",
                column: "skill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "program_skills");
        }
    }
}
