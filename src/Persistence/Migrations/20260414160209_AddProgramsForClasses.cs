using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramsForClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    current_udt text;
                BEGIN
                    SELECT c.udt_name
                    INTO current_udt
                    FROM information_schema.columns c
                    WHERE c.table_schema = 'public'
                      AND c.table_name = 'classes'
                      AND c.column_name = 'program_id';

                    IF current_udt IS NULL THEN
                        ALTER TABLE classes ADD COLUMN program_id uuid;
                    ELSIF current_udt <> 'uuid' THEN
                        ALTER TABLE classes DROP CONSTRAINT IF EXISTS fk_classes_course_programs_program_id;
                        DROP INDEX IF EXISTS ix_classes_program_id;
                        ALTER TABLE classes DROP COLUMN program_id;
                        ALTER TABLE classes ADD COLUMN program_id uuid;
                    END IF;

                    CREATE TABLE IF NOT EXISTS course_programs (
                        id uuid NOT NULL,
                        name text NOT NULL,
                        CONSTRAINT pk_course_programs PRIMARY KEY (id)
                    );

                    CREATE INDEX IF NOT EXISTS ix_classes_program_id ON classes (program_id);

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'fk_classes_course_programs_program_id'
                    ) THEN
                        ALTER TABLE classes
                            ADD CONSTRAINT fk_classes_course_programs_program_id
                            FOREIGN KEY (program_id)
                            REFERENCES course_programs (id)
                            ON DELETE SET NULL;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE classes DROP CONSTRAINT IF EXISTS fk_classes_course_programs_program_id;
                DROP INDEX IF EXISTS ix_classes_program_id;
                ALTER TABLE classes DROP COLUMN IF EXISTS program_id;
                DROP TABLE IF EXISTS course_programs;
                """);
        }
    }
}
