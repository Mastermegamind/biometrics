-- Create pending_sync and fingerprint_enrollments tables for offline-first mode

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

CREATE TABLE IF NOT EXISTS fingerprint_enrollments (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    regno VARCHAR(64) NOT NULL,
    finger_index TINYINT NOT NULL,
    finger_name VARCHAR(32) NOT NULL,
    template LONGBLOB NOT NULL,
    template_data LONGTEXT NULL,
    image_preview VARCHAR(255) NULL,
    captured_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_regno_finger (regno, finger_index),
    INDEX idx_fingerprint_regno (regno)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Ensure attendance table has proper structure
ALTER TABLE attendance
    ADD COLUMN IF NOT EXISTS is_synced TINYINT(1) DEFAULT 0,
    ADD INDEX IF NOT EXISTS idx_attendance_regno (regno),
    ADD INDEX IF NOT EXISTS idx_attendance_date (date);
