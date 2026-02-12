-- DartMaster Database Schema
-- MariaDB SQL Script
-- Version: 1.0
-- Created: 2026-02-12

-- ============================================
-- 1. USERS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `users` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `username` VARCHAR(100) NOT NULL UNIQUE COMMENT 'Unique username',
  `email` VARCHAR(255) NOT NULL UNIQUE COMMENT 'Unique email',
  `password_hash` VARCHAR(255) NOT NULL COMMENT 'Hashed password (bcrypt)',
  `full_name` VARCHAR(255) NOT NULL COMMENT 'Full name of user',
  `role` ENUM('Admin', 'Player', 'Spectator') NOT NULL DEFAULT 'Player' COMMENT 'User role',
  `is_active` BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Whether user is active',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Account creation time',
  `last_login` TIMESTAMP NULL COMMENT 'Last login timestamp',
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX idx_username (username),
  INDEX idx_email (email),
  INDEX idx_role (role)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='User accounts and authentication';

-- ============================================
-- 2. TOURNAMENTS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `tournaments` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `name` VARCHAR(255) NOT NULL COMMENT 'Tournament name',
  `description` TEXT COMMENT 'Tournament description',
  `status` ENUM('Planning', 'Active', 'Completed') NOT NULL DEFAULT 'Planning' COMMENT 'Tournament status',
  `format` ENUM('Group', 'Series', 'Knockout') NOT NULL DEFAULT 'Group' COMMENT 'Tournament format',
  `match_format` ENUM('301', '501') NOT NULL DEFAULT '301' COMMENT 'Match format (dart game)',
  `start_date` DATETIME NOT NULL COMMENT 'Tournament start date',
  `end_date` DATETIME COMMENT 'Tournament end date',
  `registration_deadline` DATETIME COMMENT 'Registration deadline',
  `max_players` INT DEFAULT 100 COMMENT 'Maximum number of players',
  `number_of_groups` INT DEFAULT 1 COMMENT 'Number of groups (for group format)',
  `admin_id` VARCHAR(36) NOT NULL COMMENT 'Admin user ID',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (admin_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_status (status),
  INDEX idx_start_date (start_date),
  INDEX idx_admin_id (admin_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tournament information';

-- ============================================
-- 3. TOURNAMENT_GROUPS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `tournament_groups` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `tournament_id` VARCHAR(36) NOT NULL COMMENT 'Tournament ID',
  `group_name` VARCHAR(100) NOT NULL COMMENT 'Group name (e.g., Group A, Group B)',
  `group_number` INT NOT NULL COMMENT 'Group number',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
  UNIQUE KEY unique_group (tournament_id, group_number),
  INDEX idx_tournament_id (tournament_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Groups within tournaments';

-- ============================================
-- 4. TOURNAMENT_PARTICIPANTS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `tournament_participants` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `tournament_id` VARCHAR(36) NOT NULL COMMENT 'Tournament ID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'User ID',
  `group_id` VARCHAR(36) COMMENT 'Group ID (for group format)',
  `status` ENUM('Registered', 'Active', 'Withdrawn', 'WO') NOT NULL DEFAULT 'Registered' COMMENT 'Participant status (WO = Walk Over)',
  `registered_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  FOREIGN KEY (group_id) REFERENCES tournament_groups(id) ON DELETE SET NULL,
  UNIQUE KEY unique_participant (tournament_id, user_id),
  INDEX idx_tournament_id (tournament_id),
  INDEX idx_user_id (user_id),
  INDEX idx_group_id (group_id),
  INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Participants in tournaments';

-- ============================================
-- 5. MATCHES TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `matches` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `tournament_id` VARCHAR(36) NOT NULL COMMENT 'Tournament ID',
  `group_id` VARCHAR(36) COMMENT 'Group ID (for group format)',
  `match_format` ENUM('301', '501') NOT NULL DEFAULT '301' COMMENT 'Match format',
  `status` ENUM('Scheduled', 'Live', 'Waiting for confirmation', 'Completed') NOT NULL DEFAULT 'Scheduled' COMMENT 'Match status',
  `scheduled_start` DATETIME COMMENT 'Scheduled start time',
  `actual_start` DATETIME COMMENT 'Actual start time',
  `actual_end` DATETIME COMMENT 'Actual end time',
  `reporting_player_id` VARCHAR(36) COMMENT 'Player who reported the result',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
  FOREIGN KEY (group_id) REFERENCES tournament_groups(id) ON DELETE SET NULL,
  FOREIGN KEY (reporting_player_id) REFERENCES users(id) ON DELETE SET NULL,
  INDEX idx_tournament_id (tournament_id),
  INDEX idx_status (status),
  INDEX idx_group_id (group_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Matches in tournament';

-- ============================================
-- 6. MATCH_PARTICIPANTS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `match_participants` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `match_id` VARCHAR(36) NOT NULL COMMENT 'Match ID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'User ID (player)',
  `finishing_score` INT COMMENT 'Final score (0 for winner)',
  `position` INT COMMENT 'Finishing position (1st, 2nd, 3rd, etc)',
  `is_confirmed` BOOLEAN DEFAULT FALSE COMMENT 'Has player confirmed result',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  UNIQUE KEY unique_match_player (match_id, user_id),
  INDEX idx_match_id (match_id),
  INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Players participating in specific match';

-- ============================================
-- 7. DART_THROWS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `dart_throws` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `match_id` VARCHAR(36) NOT NULL COMMENT 'Match ID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'Player ID',
  `throw_number` INT NOT NULL COMMENT 'Throw sequence number (1-3 per round)',
  `round_number` INT NOT NULL COMMENT 'Round number',
  `points` INT NOT NULL COMMENT 'Points scored in this throw',
  `remaining_score` INT NOT NULL COMMENT 'Remaining score after this throw',
  `is_double` BOOLEAN DEFAULT FALSE COMMENT 'Whether this was a double',
  `thrown_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_match_id (match_id),
  INDEX idx_user_id (user_id),
  INDEX idx_round (match_id, round_number)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Individual dart throws during match';

-- ============================================
-- 8. MATCH_CONFIRMATIONS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `match_confirmations` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `match_id` VARCHAR(36) NOT NULL COMMENT 'Match ID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'User ID (confirming player)',
  `confirmed` BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Confirmation status',
  `confirmed_at` TIMESTAMP COMMENT 'When confirmation was given',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (match_id) REFERENCES matches(id) ON DELETE CASCADE,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  UNIQUE KEY unique_confirmation (match_id, user_id),
  INDEX idx_match_id (match_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Result confirmations from players';

-- ============================================
-- 9. PLAYER_STATISTICS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `player_statistics` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `tournament_id` VARCHAR(36) NOT NULL COMMENT 'Tournament ID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'User ID',
  `matches_played` INT DEFAULT 0 COMMENT 'Total matches played',
  `matches_won` INT DEFAULT 0 COMMENT 'Matches won',
  `matches_lost` INT DEFAULT 0 COMMENT 'Matches lost',
  `win_loss_ratio` DECIMAL(5, 2) DEFAULT 0 COMMENT 'Win/Loss ratio',
  `average_score` DECIMAL(7, 2) DEFAULT 0 COMMENT 'Average score per match',
  `ranking` INT COMMENT 'Current ranking in tournament',
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  UNIQUE KEY unique_tournament_player (tournament_id, user_id),
  INDEX idx_tournament_id (tournament_id),
  INDEX idx_user_id (user_id),
  INDEX idx_ranking (ranking)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Player statistics per tournament';

-- ============================================
-- 10. NOTIFICATIONS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `notifications` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'Recipient user ID',
  `type` ENUM('Match_Result', 'Match_Start', 'Tournament_Start', 'Other') NOT NULL COMMENT 'Notification type',
  `title` VARCHAR(255) NOT NULL,
  `message` TEXT NOT NULL,
  `related_match_id` VARCHAR(36) COMMENT 'Related match ID (if applicable)',
  `related_tournament_id` VARCHAR(36) COMMENT 'Related tournament ID (if applicable)',
  `is_read` BOOLEAN DEFAULT FALSE COMMENT 'Whether notification has been read',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  FOREIGN KEY (related_match_id) REFERENCES matches(id) ON DELETE SET NULL,
  FOREIGN KEY (related_tournament_id) REFERENCES tournaments(id) ON DELETE SET NULL,
  INDEX idx_user_id (user_id),
  INDEX idx_created_at (created_at),
  INDEX idx_is_read (is_read)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='User notifications for matches, tournaments, etc';

-- ============================================
-- 11. NOTIFICATION_SUBSCRIPTIONS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `notification_subscriptions` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'User ID',
  `subscription_endpoint` TEXT NOT NULL COMMENT 'Web push endpoint',
  `auth_key` VARCHAR(255) NOT NULL COMMENT 'Web push auth key',
  `p256dh_key` VARCHAR(255) NOT NULL COMMENT 'Web push p256dh key',
  `email` VARCHAR(255) COMMENT 'Email for email notifications',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='User push and email notification subscriptions';

-- ============================================
-- 12. REFRESH_TOKENS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS `refresh_tokens` (
  `id` VARCHAR(36) PRIMARY KEY COMMENT 'UUID',
  `user_id` VARCHAR(36) NOT NULL COMMENT 'User ID',
  `token_hash` VARCHAR(255) NOT NULL UNIQUE COMMENT 'Hashed refresh token',
  `expires_at` DATETIME NOT NULL COMMENT 'Token expiration time',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `revoked_at` DATETIME COMMENT 'When token was revoked',
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_user_id (user_id),
  INDEX idx_expires_at (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Refresh tokens for JWT authentication';

-- ============================================
-- INDEXES AND CONSTRAINTS
-- ============================================

-- Useful queries indexes
CREATE INDEX idx_user_tournament ON tournament_participants(user_id, tournament_id);
CREATE INDEX idx_match_participants ON match_participants(match_id, position);
CREATE INDEX idx_player_stats ON player_statistics(tournament_id, ranking);

-- Enable foreign key constraints
SET FOREIGN_KEY_CHECKS=1;

-- ============================================
-- VERSION / CHANGELOG
-- ============================================
-- v1.0 (2026-02-12): Initial schema creation
--   - Users and authentication
--   - Tournaments and participants
--   - Matches and dart throws
--   - Statistics and notifications
--   - Refresh tokens for JWT
