using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCriteriaAndWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE Criteria (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    ExamSkillId UNIQUEIDENTIFIER NOT NULL,
                    Label NVARCHAR(255) NOT NULL,
                    TotalValue INT NOT NULL,
                    Position INT NOT NULL,
                    CONSTRAINT FK_Criteria_ExamSkill
                        FOREIGN KEY (ExamSkillId) REFERENCES ExamSkills(Id)
                        ON DELETE CASCADE
                );

                CREATE TABLE CriterionWeights (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    CriterionId UNIQUEIDENTIFIER NOT NULL,
                    Weight CHAR(1) NOT NULL,
                    Value INT NOT NULL,
                    Description NVARCHAR(255) NULL,
                    IsEnabled BIT NOT NULL,
                    CONSTRAINT FK_CriterionWeights_Criterion
                        FOREIGN KEY (CriterionId) REFERENCES Criteria(Id)
                        ON DELETE CASCADE
                );

                ALTER TABLE CriterionWeights
                ADD CONSTRAINT UQ_CriterionWeight UNIQUE (CriterionId, Weight);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS CriterionWeights;
                DROP TABLE IF EXISTS Criteria;
            ");
        }
    }
}
