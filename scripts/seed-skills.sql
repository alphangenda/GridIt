-- Script SQL pour créer les tables Skills et ExamSkills + données de test
-- À exécuter sur la BD locale garneau-template

-- Table des compétences
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Skills')
BEGIN
    CREATE TABLE Skills (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Label NVARCHAR(255) NOT NULL
    );
END

-- Table de liaison Examen ↔ Compétence
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExamSkills')
BEGIN
    CREATE TABLE ExamSkills (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ExamId UNIQUEIDENTIFIER NOT NULL,
        SkillId UNIQUEIDENTIFIER NOT NULL,
        Position INT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ExamSkills_Skills FOREIGN KEY (SkillId) REFERENCES Skills(Id)
    );
END

-- Données de test : compétences informatique
IF NOT EXISTS (SELECT 1 FROM Skills)
BEGIN
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
END
