using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.ComponentModel;

namespace DataFiles
{
    /// <summary>
    /// Loads quest data from quest.dat or XML configuration
    /// </summary>
    public class QuestLoader : IDataManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<int, Game.Quest> m_quests = new Dictionary<int, Game.Quest>();
        private readonly object m_lock = new object();

        public event ProgressChangedEventHandler onLoadProgressChanged;
        public Action<int, int, float> onWritePercentChange;
        public Action<object> onDebug;

        public QuestLoader()
        {
        }

        /// <summary>
        /// Load quests from XML configuration file
        /// </summary>
        public Task<bool> Load(string file)
        {
            return Task.Factory.StartNew<bool>(() =>
            {
                try
                {
                    if (!File.Exists(file))
                    {
                        log.Warn($"Quest file not found: {file}");
                        return false;
                    }

                    XmlDocument doc = new XmlDocument();
                    doc.Load(file);

                    XmlNodeList questNodes = doc.SelectNodes("//Quest");
                    int totalQuests = questNodes.Count;

                    lock (m_lock)
                    {
                        m_quests.Clear();

                        for (int i = 0; i < questNodes.Count; i++)
                        {
                            XmlNode node = questNodes[i];
                            Game.Quest quest = ParseQuestNode(node);

                            if (quest != null)
                            {
                                m_quests[quest.QuestID] = quest;
                                Game.QuestManager.RegisterQuestTemplate(quest);
                            }

                            onLoadProgressChanged?.Invoke(this, new ProgressChangedEventArgs((i * 100) / totalQuests, null));
                        }
                    }

                    log.Info($"Loaded {m_quests.Count} quests from {file}");
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($"Error loading quests from {file}: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Parses a quest XML node
        /// </summary>
        private Game.Quest ParseQuestNode(XmlNode node)
        {
            try
            {
                Game.Quest quest = new Game.Quest();

                // Basic Properties
                if (node.SelectSingleNode("QuestID") != null)
                    quest.QuestID = int.Parse(node.SelectSingleNode("QuestID").InnerText);

                if (node.SelectSingleNode("QuestName") != null)
                    quest.QuestName = node.SelectSingleNode("QuestName").InnerText;

                if (node.SelectSingleNode("Description") != null)
                    quest.Description = node.SelectSingleNode("Description").InnerText;

                if (node.SelectSingleNode("Level") != null)
                    quest.Level = byte.Parse(node.SelectSingleNode("Level").InnerText);

                if (node.SelectSingleNode("RequiredLevel") != null)
                    quest.RequiredLevel = int.Parse(node.SelectSingleNode("RequiredLevel").InnerText);

                // NPC IDs
                if (node.SelectSingleNode("StartNpcID") != null)
                    quest.StartNpcID = int.Parse(node.SelectSingleNode("StartNpcID").InnerText);

                if (node.SelectSingleNode("EndNpcID") != null)
                    quest.EndNpcID = int.Parse(node.SelectSingleNode("EndNpcID").InnerText);

                // Objectives
                if (node.SelectSingleNode("ObjectiveCount") != null)
                    quest.ObjectiveCount = int.Parse(node.SelectSingleNode("ObjectiveCount").InnerText);

                if (node.SelectSingleNode("ObjectiveType") != null)
                    quest.ObjectiveType = node.SelectSingleNode("ObjectiveType").InnerText;

                if (node.SelectSingleNode("ObjectiveTargetID") != null)
                    quest.ObjectiveTargetID = int.Parse(node.SelectSingleNode("ObjectiveTargetID").InnerText);

                // Rewards
                if (node.SelectSingleNode("RewardEXP") != null)
                    quest.RewardEXP = int.Parse(node.SelectSingleNode("RewardEXP").InnerText);

                if (node.SelectSingleNode("RewardMoney") != null)
                    quest.RewardMoney = int.Parse(node.SelectSingleNode("RewardMoney").InnerText);

                // Item Rewards
                XmlNodeList itemRewardNodes = node.SelectNodes("ItemRewards/Item");
                foreach (XmlNode itemNode in itemRewardNodes)
                {
                    int itemID = int.Parse(itemNode.SelectSingleNode("ItemID").InnerText);
                    int qty = int.Parse(itemNode.SelectSingleNode("Quantity").InnerText ?? "1");
                    bool isChoice = bool.Parse(itemNode.SelectSingleNode("IsChoice")?.InnerText ?? "false");
                    quest.ItemRewards.Add(new Game.QuestReward(itemID, qty, isChoice));
                }

                // Skill Rewards
                XmlNodeList skillRewardNodes = node.SelectNodes("SkillRewards/Skill");
                foreach (XmlNode skillNode in skillRewardNodes)
                {
                    int skillID = int.Parse(skillNode.InnerText);
                    quest.RewardSkills.Add(skillID);
                }

                // Flags
                if (node.SelectSingleNode("IsRepeatable") != null)
                    quest.IsRepeatable = bool.Parse(node.SelectSingleNode("IsRepeatable").InnerText);

                if (node.SelectSingleNode("TimeLimit") != null)
                    quest.TimeLimit = byte.Parse(node.SelectSingleNode("TimeLimit").InnerText);

                // Prerequisites
                XmlNodeList prereqNodes = node.SelectNodes("Prerequisites/Quest");
                foreach (XmlNode prereqNode in prereqNodes)
                {
                    int prereqID = int.Parse(prereqNode.InnerText);
                    quest.PrerequisiteQuests.Add(prereqID);
                }

                return quest;
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing quest node: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get a quest by ID
        /// </summary>
        public Game.Quest GetQuestByID(int questID)
        {
            lock (m_lock)
            {
                if (m_quests.ContainsKey(questID))
                {
                    return m_quests[questID];
                }
            }
            return null;
        }

        /// <summary>
        /// Get all loaded quests
        /// </summary>
        public Dictionary<int, Game.Quest> GetAllQuests()
        {
            lock (m_lock)
            {
                return new Dictionary<int, Game.Quest>(m_quests);
            }
        }

        /// <summary>
        /// Get all quests for a specific NPC
        /// </summary>
        public List<Game.Quest> GetQuestsByNPC(int npcID)
        {
            lock (m_lock)
            {
                return m_quests.Values
                    .Where(q => q.StartNpcID == npcID || q.EndNpcID == npcID)
                    .ToList();
            }
        }

        /// <summary>
        /// Get quests by level range
        /// </summary>
        public List<Game.Quest> GetQuestsByLevel(byte minLevel, byte maxLevel)
        {
            lock (m_lock)
            {
                return m_quests.Values
                    .Where(q => q.Level >= minLevel && q.Level <= maxLevel)
                    .ToList();
            }
        }

        public int Count
        {
            get
            {
                lock (m_lock)
                {
                    return m_quests.Count;
                }
            }
        }
    }
}
