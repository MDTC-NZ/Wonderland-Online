using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;
using RCLibrary.Core.Networking;
using Game;

namespace Wonderland_Private_Server.ActionCodes
{
    /// <summary>
    /// AC26 - Quest Management Handler
    /// Handles quest acceptance, completion, abandonment, and tracking
    /// </summary>
    public class AC26 : AC
    {
        public override int ID { get { return 26; } }

        public override void ProcessPkt(Player p, RecievePacket r)
        {
            try
            {
                byte subType = r.Unpack8();
                
                switch (subType)
                {
                    case 1: Recv_AcceptQuest(p, r); break;
                    case 2: Recv_AbandonQuest(p, r); break;
                    case 3: Recv_CompleteQuest(p, r); break;
                    case 4: Recv_GetQuestList(p, r); break;
                    case 5: Recv_GetQuestDetails(p, r); break;
                    case 6: Recv_UpdateQuestProgress(p, r); break;
                    default: Utilities.LogServices.Log($"AC26.{subType} - Not implemented"); break;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26 Error: {ex}");
                SendErrorPacket(p, 1);  // Error response
            }
        }

        /// <summary>
        /// AC26.1 - Accept Quest
        /// Client sends: [26][1][QuestID:2]
        /// Server responds: [26][1][QuestID:2][Success:1][ErrorCode:1]
        /// </summary>
        void Recv_AcceptQuest(Player p, RecievePacket r)
        {
            try
            {
                ushort questID = r.Unpack16();

                if (p.Quests == null)
                {
                    SendErrorPacket(p, 2);  // Quest system not ready
                    return;
                }

                bool success = p.Quests.AcceptQuest(questID);

                SendPacket response = new SendPacket();
                response.Pack((byte)26);
                response.Pack((byte)1);
                response.Pack(questID);
                response.Pack(success ? (byte)1 : (byte)0);
                response.Pack(success ? (byte)0 : (byte)1);  // Error code if failed

                p.Send(response);

                if (success)
                {
                    Utilities.LogServices.Log($"Player {p.CharName} (ID:{p.CharID}) accepted quest {questID}");
                    SendQuestListUpdate(p);  // Update quest tracker
                }
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26.1 Error: {ex}");
                SendErrorPacket(p, 2);
            }
        }

        /// <summary>
        /// AC26.2 - Abandon Quest
        /// Client sends: [26][2][QuestID:2]
        /// Server responds: [26][2][QuestID:2][Success:1]
        /// </summary>
        void Recv_AbandonQuest(Player p, RecievePacket r)
        {
            try
            {
                ushort questID = r.Unpack16();

                if (p.Quests == null)
                {
                    SendErrorPacket(p, 2);
                    return;
                }

                bool success = p.Quests.AbandonQuest(questID);

                SendPacket response = new SendPacket();
                response.Pack((byte)26);
                response.Pack((byte)2);
                response.Pack(questID);
                response.Pack(success ? (byte)1 : (byte)0);

                p.Send(response);

                if (success)
                {
                    Utilities.LogServices.Log($"Player {p.CharName} (ID:{p.CharID}) abandoned quest {questID}");
                    SendQuestListUpdate(p);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26.2 Error: {ex}");
                SendErrorPacket(p, 2);
            }
        }

        /// <summary>
        /// AC26.3 - Complete Quest (Hand In)
        /// Client sends: [26][3][QuestID:2]
        /// Server responds: [26][3][QuestID:2][Success:1][RewardExp:4][RewardGold:4]
        /// </summary>
        void Recv_CompleteQuest(Player p, RecievePacket r)
        {
            try
            {
                ushort questID = r.Unpack16();

                if (p.Quests == null)
                {
                    SendErrorPacket(p, 2);
                    return;
                }

                bool success = p.Quests.CompleteQuest(questID);

                SendPacket response = new SendPacket();
                response.Pack((byte)26);
                response.Pack((byte)3);
                response.Pack(questID);
                response.Pack(success ? (byte)1 : (byte)0);

                if (success)
                {
                    // Get quest details for reward info
                    var quest = QuestManager.GetQuestTemplate(questID);
                    if (quest != null)
                    {
                        response.Pack(quest.RewardEXP);
                        response.Pack(quest.RewardMoney);
                    }
                    else
                    {
                        response.Pack(0);  // No exp
                        response.Pack(0);  // No gold
                    }
                    
                    Utilities.LogServices.Log($"Player {p.CharName} (ID:{p.CharID}) completed quest {questID}");
                    SendQuestListUpdate(p);
                }
                else
                {
                    response.Pack(0);
                    response.Pack(0);
                }

                p.Send(response);
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26.3 Error: {ex}");
                SendErrorPacket(p, 2);
            }
        }

        /// <summary>
        /// AC26.4 - Get Quest List
        /// Client sends: [26][4]
        /// Server responds: [26][4][Count:2]
        ///   For each active quest: [QuestID:2][Progress:1][Objectives:1][QuestName:string]
        /// </summary>
        void Recv_GetQuestList(Player p, RecievePacket r)
        {
            try
            {
                if (p.Quests == null)
                {
                    SendErrorPacket(p, 2);
                    return;
                }

                SendQuestListUpdate(p);
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26.4 Error: {ex}");
                SendErrorPacket(p, 2);
            }
        }

        /// <summary>
        /// AC26.5 - Get Quest Details
        /// Client sends: [26][5][QuestID:2]
        /// Server responds: [26][5][QuestID:2]
        ///   [Name:string][Description:string]
        ///   [Progress:1][Objectives:1]
        ///   [RewardExp:4][RewardGold:4]
        ///   [ItemRewardCount:1]
        ///     For each: [ItemID:2][Quantity:2][IsChoice:1]
        /// </summary>
        void Recv_GetQuestDetails(Player p, RecievePacket r)
        {
            try
            {
                ushort questID = r.Unpack16();

                if (p.Quests == null)
                {
                    SendErrorPacket(p, 2);
                    return;
                }

                var template = QuestManager.GetQuestTemplate(questID);
                if (template == null)
                {
                    SendErrorPacket(p, 3);  // Quest not found
                    return;
                }

                SendPacket response = new SendPacket();
                response.Pack((byte)26);
                response.Pack((byte)5);
                response.Pack(questID);
                response.Pack(template.QuestName);
                response.Pack(template.Description ?? "");
                response.Pack((byte)template.CurrentProgress);
                response.Pack((byte)template.ObjectiveCount);
                response.Pack(template.RewardEXP);
                response.Pack(template.RewardMoney);
                
                if (template.ItemRewards != null && template.ItemRewards.Count > 0)
                {
                    response.Pack((byte)template.ItemRewards.Count);
                    foreach (var reward in template.ItemRewards)
                    {
                        response.Pack(reward.ItemID);
                        response.Pack(reward.Quantity);
                        response.Pack(reward.IsChoice ? (byte)1 : (byte)0);
                    }
                }
                else
                {
                    response.Pack((byte)0);  // No item rewards
                }

                p.Send(response);
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26.5 Error: {ex}");
                SendErrorPacket(p, 2);
            }
        }

        /// <summary>
        /// AC26.6 - Update Quest Progress (Internal/Debug)
        /// Client sends: [26][6][QuestID:2][Amount:1]
        /// Server responds: [26][6][QuestID:2][NewProgress:1]
        /// </summary>
        void Recv_UpdateQuestProgress(Player p, RecievePacket r)
        {
            try
            {
                ushort questID = r.Unpack16();
                byte amount = r.Unpack8();

                if (p.Quests == null)
                {
                    SendErrorPacket(p, 2);
                    return;
                }

                p.Quests.UpdateQuestProgress(questID, amount);

                SendPacket response = new SendPacket();
                response.Pack((byte)26);
                response.Pack((byte)6);
                response.Pack(questID);
                
                var quest = QuestManager.GetQuestTemplate(questID);
                if (quest != null)
                {
                    response.Pack((byte)quest.CurrentProgress);
                }
                else
                {
                    response.Pack((byte)0);
                }

                p.Send(response);
                SendQuestListUpdate(p);

                Utilities.LogServices.Log($"Player {p.CharName} quest {questID} progress updated");
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"AC26.6 Error: {ex}");
                SendErrorPacket(p, 2);
            }
        }

        /// <summary>
        /// Send updated quest list to client
        /// </summary>
        void SendQuestListUpdate(Player p)
        {
            try
            {
                SendPacket packet = new SendPacket();
                packet.Pack((byte)26);
                packet.Pack((byte)4);  // Quest list subtype

                // TODO: Get active quests from player
                // For now, send empty list
                packet.Pack((ushort)0);  // Quest count

                p.Send(packet);
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"SendQuestListUpdate Error: {ex}");
            }
        }

        /// <summary>
        /// Send error response packet
        /// </summary>
        void SendErrorPacket(Player p, byte errorCode)
        {
            try
            {
                SendPacket response = new SendPacket();
                response.Pack((byte)26);
                response.Pack((byte)0);  // Error response
                response.Pack(errorCode);
                p.Send(response);
            }
            catch (Exception ex)
            {
                Utilities.LogServices.Log($"SendErrorPacket Error: {ex}");
            }
        }
    }
}
