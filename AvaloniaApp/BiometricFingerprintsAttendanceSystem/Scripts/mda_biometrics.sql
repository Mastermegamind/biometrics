-- phpMyAdmin SQL Dump
-- version 6.0.0-dev+20251014.c784570216
-- https://www.phpmyadmin.net/
--
-- Host: localhost
-- Generation Time: Jan 29, 2026 at 12:14 PM
-- Server version: 10.11.14-MariaDB-0+deb12u2
-- PHP Version: 8.4.17

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `mda_biometrics`
--

-- --------------------------------------------------------

--
-- Table structure for table `attendance`
--

CREATE TABLE `attendance` (
  `id` int(11) NOT NULL,
  `matricno` varchar(50) NOT NULL,
  `name` varchar(150) NOT NULL,
  `date` date NOT NULL,
  `day` varchar(20) NOT NULL,
  `timein` time NOT NULL,
  `timeout` time DEFAULT NULL,
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `sync_status` enum('synced','pending','error') NOT NULL DEFAULT 'pending',
  `sync_error` varchar(255) DEFAULT NULL,
  `is_synced` tinyint(1) DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `audit_log`
--

CREATE TABLE `audit_log` (
  `id` bigint(20) NOT NULL,
  `actor` varchar(100) NOT NULL,
  `action` varchar(100) NOT NULL,
  `target` varchar(150) DEFAULT NULL,
  `status` varchar(50) NOT NULL,
  `message` varchar(255) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `admin_users`
--

CREATE TABLE `admin_users` (
  `id` int(11) NOT NULL,
  `username` varchar(100) NOT NULL,
  `usertype` varchar(50) NOT NULL DEFAULT 'Administrator',
  `password` varchar(255) NOT NULL,
  `name` varchar(150) NOT NULL,
  `contactno` varchar(50) DEFAULT NULL,
  `email` varchar(150) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `login_attempts`
--

CREATE TABLE `login_attempts` (
  `id` bigint(20) NOT NULL,
  `username` varchar(100) NOT NULL,
  `success` tinyint(1) NOT NULL,
  `attempted_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `lockout_until` timestamp NULL DEFAULT NULL,
  `failure_count` int(11) NOT NULL DEFAULT 0,
  `message` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `password_reset`
--

CREATE TABLE `password_reset` (
  `id` bigint(20) NOT NULL,
  `admin_username` varchar(100) NOT NULL,
  `target_username` varchar(100) NOT NULL,
  `reset_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `message` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `demo_fingerprints`
--

CREATE TABLE `demo_fingerprints` (
  `regno` varchar(50) NOT NULL,
  `finger_index` tinyint(4) NOT NULL,
  `template_base64` text NOT NULL,
  `image_name` varchar(255) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `demo_user`
--

CREATE TABLE `demo_user` (
  `id` int(11) NOT NULL,
  `regno` varchar(50) NOT NULL,
  `name` varchar(150) NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `demo_user`
--

INSERT INTO `demo_user` (`id`, `regno`, `name`, `created_at`) VALUES
(1, 'DEMO001', 'Demo Student', '2026-01-28 16:34:33');

-- --------------------------------------------------------

--
-- Table structure for table `new_enrollment`
--

CREATE TABLE `new_enrollment` (
  `matricno` varchar(50) NOT NULL,
  `fingerdata1` longblob DEFAULT NULL,
  `fingerdata2` longblob DEFAULT NULL,
  `fingerdata3` longblob DEFAULT NULL,
  `fingerdata4` longblob DEFAULT NULL,
  `fingerdata5` longblob DEFAULT NULL,
  `fingerdata6` longblob DEFAULT NULL,
  `fingerdata7` longblob DEFAULT NULL,
  `fingerdata8` longblob DEFAULT NULL,
  `fingerdata9` longblob DEFAULT NULL,
  `fingerdata10` longblob DEFAULT NULL,
  `fingermask` int(11) NOT NULL DEFAULT 0,
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `sync_status` enum('synced','pending','error') NOT NULL DEFAULT 'synced',
  `sync_error` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pending_sync`
--

CREATE TABLE `pending_sync` (
  `id` bigint(20) NOT NULL,
  `operation_type` varchar(50) NOT NULL,
  `json_payload` longtext NOT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `retry_count` int(11) DEFAULT 0,
  `last_error` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `registration`
--

CREATE TABLE `registration` (
  `id` int(11) NOT NULL,
  `username` varchar(100) NOT NULL,
  `usertype` varchar(50) NOT NULL,
  `password` varchar(255) NOT NULL,
  `name` varchar(150) NOT NULL,
  `contactno` varchar(50) DEFAULT NULL,
  `email` varchar(150) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `students`
--

CREATE TABLE `students` (
  `matricno` varchar(50) NOT NULL,
  `name` varchar(150) NOT NULL,
  `faculty` varchar(150) NOT NULL,
  `department` varchar(150) NOT NULL,
  `bloodgroup` varchar(20) NOT NULL,
  `gradyear` varchar(20) NOT NULL,
  `gender` varchar(20) NOT NULL,
  `passport` longblob DEFAULT NULL,
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `sync_status` enum('synced','pending','error') NOT NULL DEFAULT 'synced',
  `sync_error` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `attendance`
--
ALTER TABLE `attendance`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_attendance_matricno` (`matricno`),
  ADD KEY `idx_attendance_date` (`date`);

--
-- Indexes for table `audit_log`
--
ALTER TABLE `audit_log`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_audit_actor` (`actor`),
  ADD KEY `idx_audit_action` (`action`),
  ADD KEY `idx_audit_created_at` (`created_at`);

--
-- Indexes for table `admin_users`
--
ALTER TABLE `admin_users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_admin_users_username` (`username`);

--
-- Indexes for table `login_attempts`
--
ALTER TABLE `login_attempts`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_login_attempts_username` (`username`),
  ADD KEY `idx_login_attempts_attempted_at` (`attempted_at`),
  ADD KEY `idx_login_attempts_lockout_until` (`lockout_until`);

--
-- Indexes for table `password_reset`
--
ALTER TABLE `password_reset`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_password_reset_admin` (`admin_username`),
  ADD KEY `idx_password_reset_target` (`target_username`),
  ADD KEY `idx_password_reset_reset_at` (`reset_at`);

--
-- Indexes for table `demo_fingerprints`
--
ALTER TABLE `demo_fingerprints`
  ADD PRIMARY KEY (`regno`,`finger_index`);

--
-- Indexes for table `demo_user`
--
ALTER TABLE `demo_user`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_demo_regno` (`regno`);

--
-- Indexes for table `new_enrollment`
--
ALTER TABLE `new_enrollment`
  ADD PRIMARY KEY (`matricno`),
  ADD KEY `idx_enrollment_matricno` (`matricno`);

--
-- Indexes for table `pending_sync`
--
ALTER TABLE `pending_sync`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_operation_type` (`operation_type`),
  ADD KEY `idx_created_at` (`created_at`),
  ADD KEY `idx_retry_count` (`retry_count`);

--
-- Indexes for table `registration`
--
ALTER TABLE `registration`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_registration_username` (`username`);

--
-- Indexes for table `students`
--
ALTER TABLE `students`
  ADD PRIMARY KEY (`matricno`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `attendance`
--
ALTER TABLE `attendance`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `audit_log`
--
ALTER TABLE `audit_log`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `admin_users`
--
ALTER TABLE `admin_users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `login_attempts`
--
ALTER TABLE `login_attempts`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `demo_user`
--
ALTER TABLE `demo_user`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT for table `pending_sync`
--
ALTER TABLE `pending_sync`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `password_reset`
--
ALTER TABLE `password_reset`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `registration`
--
ALTER TABLE `registration`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `attendance`
--
ALTER TABLE `attendance`
  ADD CONSTRAINT `fk_attendance_student` FOREIGN KEY (`matricno`) REFERENCES `students` (`matricno`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `demo_fingerprints`
--
ALTER TABLE `demo_fingerprints`
  ADD CONSTRAINT `fk_demo_fps_user` FOREIGN KEY (`regno`) REFERENCES `demo_user` (`regno`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `new_enrollment`
--
ALTER TABLE `new_enrollment`
  ADD CONSTRAINT `fk_enrollment_student` FOREIGN KEY (`matricno`) REFERENCES `students` (`matricno`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
