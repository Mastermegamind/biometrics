-- MySQL schema for mda_biometrics (normalized fingerprint storage)
-- Generated: 2026-01-30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

-- --------------------------------------------------------
-- Table: admin_users
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS admin_users (
  id INT NOT NULL AUTO_INCREMENT,
  username VARCHAR(100) NOT NULL,
  usertype VARCHAR(50) NOT NULL DEFAULT 'Administrator',
  password VARCHAR(255) NOT NULL,
  name VARCHAR(150) NOT NULL,
  contactno VARCHAR(50) DEFAULT NULL,
  email VARCHAR(150) DEFAULT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_admin_users_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO admin_users (id, username, usertype, password, name, contactno, email, created_at, updated_at)
VALUES
(1, 'super-admin', 'Administrator', '$2y$12$6oHx.x58O0nGc04mf9OF9.h.JxLaIWV3bHt6tIs/5ddlttNjxhzW2', 'super-admin', '070123456789', 'ishikotevu@gmail.com', '2026-01-29 13:12:59', NULL)
ON DUPLICATE KEY UPDATE username = VALUES(username);

-- --------------------------------------------------------
-- Table: students
-- regNo is the stable primary identifier for biometrics
-- matricno is optional and kept for legacy compatibility
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS students (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  regno VARCHAR(64) NOT NULL,
  matricno VARCHAR(64) NULL,
  name VARCHAR(150) NOT NULL,
  class_name VARCHAR(150) DEFAULT NULL,
  faculty VARCHAR(150) DEFAULT NULL,
  department VARCHAR(150) DEFAULT NULL,
  bloodgroup VARCHAR(20) DEFAULT NULL,
  gradyear VARCHAR(20) DEFAULT NULL,
  gender VARCHAR(20) DEFAULT NULL,
  passport_filename VARCHAR(255) DEFAULT NULL,
  passport_url VARCHAR(255) DEFAULT NULL,
  renewal_date DATE DEFAULT NULL,
  is_enrolled TINYINT(1) DEFAULT NULL,
  fingers_enrolled INT DEFAULT NULL,
  enrolled_at DATETIME DEFAULT NULL,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  sync_status ENUM('synced','pending','error') NOT NULL DEFAULT 'synced',
  sync_error VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_students_regno (regno),
  UNIQUE KEY uq_students_matricno (matricno),
  KEY idx_students_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: fingerprint_enrollments (normalized per finger)
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS fingerprint_enrollments (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  regno VARCHAR(64) NOT NULL,
  finger_index TINYINT NOT NULL,
  finger_name VARCHAR(32) NOT NULL,
  template LONGBLOB NOT NULL,
  template_data LONGTEXT NULL,
  image_preview VARCHAR(255) NULL,
  captured_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_regno_finger (regno, finger_index),
  KEY idx_fingerprint_regno (regno),
  CONSTRAINT fk_enroll_student FOREIGN KEY (regno) REFERENCES students(regno) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: attendance
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS attendance (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  regno VARCHAR(64) NOT NULL,
  name VARCHAR(150) NOT NULL,
  date DATE NOT NULL,
  day VARCHAR(20) NOT NULL,
  timein TIME NOT NULL,
  timeout TIME DEFAULT NULL,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  sync_status ENUM('synced','pending','error') NOT NULL DEFAULT 'pending',
  sync_error VARCHAR(255) DEFAULT NULL,
  is_synced TINYINT(1) DEFAULT 0,
  PRIMARY KEY (id),
  KEY idx_attendance_regno (regno),
  KEY idx_attendance_date (date),
  CONSTRAINT fk_attendance_student FOREIGN KEY (regno) REFERENCES students(regno) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: audit_log
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit_log (
  id BIGINT NOT NULL AUTO_INCREMENT,
  actor VARCHAR(100) NOT NULL,
  action VARCHAR(100) NOT NULL,
  target VARCHAR(150) DEFAULT NULL,
  status VARCHAR(50) NOT NULL,
  message VARCHAR(255) DEFAULT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_audit_actor (actor),
  KEY idx_audit_action (action),
  KEY idx_audit_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: demo_user
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS demo_user (
  id INT NOT NULL AUTO_INCREMENT,
  regno VARCHAR(50) NOT NULL,
  name VARCHAR(150) NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_demo_regno (regno)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO demo_user (id, regno, name, created_at)
VALUES (1, 'DEMO001', 'Demo Student', '2026-01-28 16:34:33')
ON DUPLICATE KEY UPDATE regno = VALUES(regno);

-- --------------------------------------------------------
-- Table: demo_fingerprints
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS demo_fingerprints (
  regno VARCHAR(50) NOT NULL,
  finger_index TINYINT NOT NULL,
  template_base64 TEXT NOT NULL,
  image_name VARCHAR(255) DEFAULT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (regno, finger_index),
  CONSTRAINT fk_demo_fps_user FOREIGN KEY (regno) REFERENCES demo_user(regno) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: login_attempts
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS login_attempts (
  id BIGINT NOT NULL AUTO_INCREMENT,
  username VARCHAR(100) NOT NULL,
  success TINYINT(1) NOT NULL,
  attempted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  lockout_until TIMESTAMP NULL DEFAULT NULL,
  failure_count INT NOT NULL DEFAULT 0,
  message VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (id),
  KEY idx_login_attempts_username (username),
  KEY idx_login_attempts_attempted_at (attempted_at),
  KEY idx_login_attempts_lockout_until (lockout_until)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: password_reset
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS password_reset (
  id BIGINT NOT NULL AUTO_INCREMENT,
  admin_username VARCHAR(100) NOT NULL,
  target_username VARCHAR(100) NOT NULL,
  reset_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  message VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (id),
  KEY idx_password_reset_admin (admin_username),
  KEY idx_password_reset_target (target_username),
  KEY idx_password_reset_reset_at (reset_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------
-- Table: pending_sync
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS pending_sync (
  id BIGINT NOT NULL AUTO_INCREMENT,
  operation_type VARCHAR(50) NOT NULL,
  json_payload LONGTEXT NOT NULL,
  created_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  retry_count INT DEFAULT 0,
  last_error TEXT DEFAULT NULL,
  PRIMARY KEY (id),
  KEY idx_operation_type (operation_type),
  KEY idx_created_at (created_at),
  KEY idx_retry_count (retry_count)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: registration
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS registration (
  id INT NOT NULL AUTO_INCREMENT,
  username VARCHAR(100) NOT NULL,
  usertype VARCHAR(50) NOT NULL,
  password VARCHAR(255) NOT NULL,
  name VARCHAR(150) NOT NULL,
  contactno VARCHAR(50) DEFAULT NULL,
  email VARCHAR(150) DEFAULT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_registration_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
