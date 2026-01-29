-- Create pending_sync table for offline-first and online-first modes
-- This table stores operations that need to be synced to the API

CREATE TABLE IF NOT EXISTS pending_sync (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    operation_type VARCHAR(50) NOT NULL,        -- 'Enrollment', 'ClockIn', 'ClockOut'
    json_payload LONGTEXT NOT NULL,             -- JSON serialized request data
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    retry_count INT DEFAULT 0,
    last_error TEXT,
    INDEX idx_operation_type (operation_type),
    INDEX idx_created_at (created_at),
    INDEX idx_retry_count (retry_count)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Ensure the new_enrollment table has the required columns for fingerprint storage
-- This table stores fingerprint templates for local verification

ALTER TABLE new_enrollment
    ADD COLUMN IF NOT EXISTS fingerdata1 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata2 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata3 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata4 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata5 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata6 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata7 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata8 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata9 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingerdata10 LONGBLOB,
    ADD COLUMN IF NOT EXISTS fingermask VARCHAR(10) DEFAULT '0000000000';

-- Create index on matricno for faster lookups
CREATE INDEX IF NOT EXISTS idx_enrollment_matricno ON new_enrollment(matricno);

-- Ensure attendance table has proper structure
ALTER TABLE attendance
    ADD COLUMN IF NOT EXISTS is_synced TINYINT(1) DEFAULT 0,
    ADD INDEX IF NOT EXISTS idx_attendance_matricno (matricno),
    ADD INDEX IF NOT EXISTS idx_attendance_date (date);
