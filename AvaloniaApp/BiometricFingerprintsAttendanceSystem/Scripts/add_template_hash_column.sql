-- Migration: Add template_hash column to fingerprint_enrollments table
-- Purpose: Enables smart/incremental sync to detect template changes using SHA256 hash
-- Run this script on existing databases to enable the smart caching feature

-- Add the template_hash column if it doesn't exist
SET @dbname = DATABASE();
SET @tablename = 'fingerprint_enrollments';
SET @columnname = 'template_hash';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @dbname
    AND TABLE_NAME = @tablename
    AND COLUMN_NAME = @columnname
  ) > 0,
  'SELECT ''Column already exists''',
  CONCAT('ALTER TABLE `', @tablename, '` ADD COLUMN `', @columnname, '` VARCHAR(64) NULL COMMENT ''SHA256 hash for smart sync change detection'' AFTER `template_data`')
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Add index on template_hash for faster lookups (if not exists)
SET @indexname = 'idx_template_hash';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @dbname
    AND TABLE_NAME = @tablename
    AND INDEX_NAME = @indexname
  ) > 0,
  'SELECT ''Index already exists''',
  CONCAT('ALTER TABLE `', @tablename, '` ADD INDEX `', @indexname, '` (`', @columnname, '`)')
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Show result
SELECT 'Migration completed. Template hash column ready for smart sync.' AS result;
