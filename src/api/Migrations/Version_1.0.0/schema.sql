CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Users` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserName` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
    `Address` varchar(200) CHARACTER SET utf8mb4 NULL,
    `City` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Rating` double NULL,
    `RatingCount` int NULL,
    `ProfilePictureUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAtUTC` datetime(6) NOT NULL,
    `CreatedById` char(36) COLLATE ascii_general_ci NOT NULL,
    `UpdatedAtUTC` datetime(6) NOT NULL,
    `UpdatedById` char(36) COLLATE ascii_general_ci NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260719062034_Ver_1.0.0', '9.0.0');

COMMIT;

