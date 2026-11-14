-- Quest System Database Schema
-- Wonderland Online Private Server

-- Main quest data table
CREATE TABLE IF NOT EXISTS quests (
    questID INT PRIMARY KEY NOT NULL,
    questName VARCHAR(255) NOT NULL,
    description TEXT,
    level TINYINT DEFAULT 1,
    requiredLevel INT DEFAULT 1,
    startNpcID INT,
    endNpcID INT,
    objectiveCount INT DEFAULT 1,
    objectiveType VARCHAR(50),
    objectiveTargetID INT,
    rewardEXP INT DEFAULT 0,
    rewardMoney INT DEFAULT 0,
    isRepeatable BOOLEAN DEFAULT 0,
    timeLimit TINYINT DEFAULT 0,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_level (level),
    INDEX idx_startNpc (startNpcID),
    INDEX idx_endNpc (endNpcID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quest item rewards table
CREATE TABLE IF NOT EXISTS quest_item_rewards (
    rewardID INT AUTO_INCREMENT PRIMARY KEY,
    questID INT NOT NULL,
    itemID INT NOT NULL,
    quantity INT DEFAULT 1,
    isChoice BOOLEAN DEFAULT 0,
    FOREIGN KEY (questID) REFERENCES quests(questID) ON DELETE CASCADE,
    INDEX idx_quest (questID),
    INDEX idx_item (itemID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quest skill rewards table
CREATE TABLE IF NOT EXISTS quest_skill_rewards (
    rewardID INT AUTO_INCREMENT PRIMARY KEY,
    questID INT NOT NULL,
    skillID INT NOT NULL,
    FOREIGN KEY (questID) REFERENCES quests(questID) ON DELETE CASCADE,
    INDEX idx_quest (questID),
    INDEX idx_skill (skillID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quest prerequisites table (quest chains)
CREATE TABLE IF NOT EXISTS quest_prerequisites (
    prerequisiteID INT AUTO_INCREMENT PRIMARY KEY,
    questID INT NOT NULL,
    prerequisiteQuestID INT NOT NULL,
    FOREIGN KEY (questID) REFERENCES quests(questID) ON DELETE CASCADE,
    FOREIGN KEY (prerequisiteQuestID) REFERENCES quests(questID) ON DELETE CASCADE,
    UNIQUE KEY unique_prerequisite (questID, prerequisiteQuestID),
    INDEX idx_quest (questID),
    INDEX idx_prerequisite (prerequisiteQuestID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player active quests table
CREATE TABLE IF NOT EXISTS character_active_quests (
    charQuestID INT AUTO_INCREMENT PRIMARY KEY,
    charID INT NOT NULL,
    questID INT NOT NULL,
    progress INT DEFAULT 0,
    state INT DEFAULT 0,
    acceptedTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completedTime TIMESTAMP NULL,
    timeExpired BOOLEAN DEFAULT 0,
    FOREIGN KEY (charID) REFERENCES characters(charID) ON DELETE CASCADE,
    FOREIGN KEY (questID) REFERENCES quests(questID) ON DELETE CASCADE,
    UNIQUE KEY unique_active_quest (charID, questID),
    INDEX idx_charID (charID),
    INDEX idx_questID (questID),
    INDEX idx_state (state),
    INDEX idx_acceptedTime (acceptedTime)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Player completed quests table (for history and repeatable quest tracking)
CREATE TABLE IF NOT EXISTS character_completed_quests (
    charQuestID INT AUTO_INCREMENT PRIMARY KEY,
    charID INT NOT NULL,
    questID INT NOT NULL,
    completedTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    rewardsClaimed BOOLEAN DEFAULT 1,
    timesCompleted INT DEFAULT 1,
    FOREIGN KEY (charID) REFERENCES characters(charID) ON DELETE CASCADE,
    FOREIGN KEY (questID) REFERENCES quests(questID) ON DELETE CASCADE,
    UNIQUE KEY unique_completed_quest (charID, questID),
    INDEX idx_charID (charID),
    INDEX idx_questID (questID),
    INDEX idx_completedTime (completedTime)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quest objective progress table (for complex multi-stage quests)
CREATE TABLE IF NOT EXISTS quest_objective_progress (
    objectiveProgressID INT AUTO_INCREMENT PRIMARY KEY,
    charQuestID INT NOT NULL,
    objectiveIndex INT NOT NULL,
    progress INT DEFAULT 0,
    completed BOOLEAN DEFAULT 0,
    FOREIGN KEY (charQuestID) REFERENCES character_active_quests(charQuestID) ON DELETE CASCADE,
    INDEX idx_charQuestID (charQuestID),
    INDEX idx_objectiveIndex (objectiveIndex)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Sample data
INSERT INTO quests (questID, questName, description, level, requiredLevel, startNpcID, endNpcID, objectiveCount, objectiveType, objectiveTargetID, rewardEXP, rewardMoney, isRepeatable, timeLimit) VALUES
(1, 'First Steps', 'Talk to the village elder to begin your adventure', 1, 1, 100, 100, 1, 'TalkToNpc', 100, 100, 50, 0, 0),
(2, 'Collect Herbs', 'Gather 10 healing herbs for the herbalist', 5, 5, 101, 101, 10, 'CollectItem', 5001, 500, 200, 0, 0),
(3, 'Defeat Slimes', 'Defeat 5 slimes terrorizing the village', 3, 1, 100, 100, 5, 'KillMob', 1001, 300, 150, 1, 24),
(4, 'Deliver Package', 'Deliver the merchant package to the next town', 10, 10, 102, 103, 1, 'DeliverItem', 5002, 1000, 500, 0, 48);

-- Insert item rewards
INSERT INTO quest_item_rewards (questID, itemID, quantity, isChoice) VALUES
(2, 2001, 1, 0),
(3, 2002, 2, 0),
(4, 2003, 1, 0),
(4, 2004, 1, 1);

-- Insert prerequisites
INSERT INTO quest_prerequisites (questID, prerequisiteQuestID) VALUES
(2, 1),
(4, 3);
