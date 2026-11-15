using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using Game;
using System.Threading;
using RCLibrary.Core;
using Game.Code;
using System.Security.Cryptography;

namespace DataBase
{
    public enum GMStatus
    {
        None,
    }

    public sealed class MallCharacterInfo
    {
        public uint CharacterId { get; set; }
        public string Name { get; set; }
        public byte Slot { get; set; }
    }

    public sealed class WebCharacterInfo
    {
        public uint CharacterId { get; set; }
        public uint AccountId { get; set; }
        public string AccountUsername { get; set; }
        public string Name { get; set; }
        public byte Slot { get; set; }
        public byte Level { get; set; }
        public bool IsOnline { get; set; }
        public byte Body { get; set; }
        public byte Head { get; set; }
        public byte Element { get; set; }
        public uint Gold { get; set; }
        public ushort LocationMap { get; set; }
        public ushort LocationX { get; set; }
        public ushort LocationY { get; set; }
        public long Experience { get; set; }
        public int CurrentHp { get; set; }
        public int CurrentSp { get; set; }
        public int Strength { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Agility { get; set; }
        public List<ushort> EquipmentItemIds { get; set; }
    }

    public sealed class MallProductInfo
    {
        public ushort ItemId { get; set; }
        public string Category { get; set; }
        public int BasePrice { get; set; }
        public int Price { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public sealed class MallPurchaseResult
    {
        public bool Success { get; set; }
        public bool InsufficientPoints { get; set; }
        public bool CharacterNotFound { get; set; }
        public bool AlreadyProcessed { get; set; }
        public string OrderId { get; set; }
        public string Status { get; set; }
        public int Balance { get; set; }
    }

    public sealed class UserDataBase : RCLibrary.Core.DataBase
    {
        readonly object mylock = new object();

        //Used to provide flexibility to alter columns name and match them with the correct value
        public string TableName = "user";
        public string Username_Ref = "username";
        public string Password_Ref = "password";
        public string DataBaseID_Ref = "userID";
        public string CharacterID1_Ref = "character1ID";
        public string CharacterID2_Ref = "character2ID";
        public string IM_Ref = "IM";
        public string Char_Delete_Code_Ref = "char_delete_code";
        public VerifyPassType PassVerification;
        public bool LocalSetupVerified { get; private set; }

        const int PasswordHashIterations = 100000;
        const string PasswordHashPrefix = "wlo-pbkdf2-sha1$";


        List<User> GOnlineUsers;

        bool shutdown = false;

        public UserDataBase()
        {

        }

        public void ConfigureSqlite(string path)
        {
            ServType = RCLibrary.Core.DataBaseTypes.Sqlite;
            DBFile = path;
        }

        public void VerifyLocalSetup()
        {
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS user (userID INTEGER PRIMARY KEY, username TEXT NOT NULL UNIQUE, password TEXT NOT NULL, character1ID INTEGER, character2ID INTEGER, IM INTEGER NOT NULL DEFAULT 0, char_delete_code TEXT)");
            DataTable columns = GetDataTable("PRAGMA table_info(user)");
            if (columns == null)
                throw new InvalidOperationException("The local user table could not be inspected.");

            bool hasGMLevel = false;
            foreach (DataRow column in columns.Rows)
            {
                if (string.Equals(column["name"].ToString(), "gmLevel", StringComparison.OrdinalIgnoreCase))
                {
                    hasGMLevel = true;
                    break;
                }
            }
            if (!hasGMLevel)
                ExecuteNonQuery("ALTER TABLE user ADD COLUMN gmLevel INTEGER NOT NULL DEFAULT 0");

            if (GetDataTable("SELECT userID, username, password, character1ID, character2ID, IM, char_delete_code, gmLevel FROM user WHERE 1 = 0") == null)
                throw new InvalidOperationException("The local user table is missing required columns.");
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS mall_orders (orderID TEXT PRIMARY KEY, requestID TEXT NOT NULL, userID INTEGER NOT NULL, charID INTEGER NOT NULL, itemID INTEGER NOT NULL, price INTEGER NOT NULL, status TEXT NOT NULL, createdAt TEXT NOT NULL, deliveredAt TEXT, UNIQUE(userID, requestID))");
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS mall_reviews (userID INTEGER NOT NULL, itemID INTEGER NOT NULL, rating INTEGER NOT NULL, reviewText TEXT NOT NULL, createdAt TEXT NOT NULL, UNIQUE(userID, itemID))");
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS mall_newsletter (email TEXT PRIMARY KEY, subscribedAt TEXT NOT NULL)");
            LocalSetupVerified = true;
        }
        
        public int Count() { return GOnlineUsers.Count; }

        public bool isLoggedin(string user)
        {
            bool resp = (GOnlineUsers.Count(c => c.UserName == user) > 0);
            DebugSystem.Write(DebugItemType.Info_Heavy, "Checking if User '{0}' is Online... [Resp]: {1}", DebugItemType.Info_Heavy, user, resp);
            return resp;
        }

        public override bool VerifyPassword(string check, string with)
        {
            if (with != null && with.StartsWith(PasswordHashPrefix, StringComparison.Ordinal))
                return VerifyHashedPassword(check, with);
            return base.VerifyPassword(check, with);
        }

        public bool CreateAccount(string username, string password)
        {
            return CreateAccount(username, password, 0);
        }

        public bool CreateAccount(string username, string password, int gmLevel)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Account registration is available only for the default local account database.");

            if (gmLevel < 0 || gmLevel > 1)
                throw new ArgumentOutOfRangeException("gmLevel", "GM/Admin level must be 0 or 1.");

            if (username == null)
                throw new ArgumentException("Enter a username.", "username");

            username = username.Trim().ToLowerInvariant();
            if (username.Length < 4 || username.Length > 14)
                throw new ArgumentException("Usernames must be 4 to 14 characters long.", "username");
            for (int i = 0; i < username.Length; i++)
            {
                char character = username[i];
                if (!((character >= 'a' && character <= 'z') ||
                      (character >= '0' && character <= '9') || character == '_'))
                    throw new ArgumentException("Use only lowercase letters, numbers, and underscores in usernames.", "username");
            }

            if (password == null || password.Length < 4 || password.Length > 14)
                throw new ArgumentException("Passwords must be 4 to 14 characters long.", "password");
            for (int i = 0; i < password.Length; i++)
            {
                if (char.IsControl(password[i]))
                    throw new ArgumentException("Passwords cannot contain control characters.", "password");
            }

            if (LocalAccountExists(username))
                return false;

            try
            {
                return ExecuteNonQuery(
                    "INSERT INTO user (username, password, gmLevel) VALUES (@username, @password, @gmLevel)",
                    new DbParam("@username", username),
                    new DbParam("@password", HashPassword(password)),
                    new DbParam("@gmLevel", gmLevel)) == 1;
            }
            catch (DbException)
            {
                if (LocalAccountExists(username))
                    return false;
                throw;
            }
        }

        bool LocalAccountExists(string username)
        {
            DataTable existing = GetDataTable("SELECT 1 FROM user WHERE username = @username", new DbParam("@username", username));
            if (existing == null)
                throw new InvalidOperationException("The game account database could not be queried.");
            return existing.Rows.Count != 0;
        }

        public DataTable GetLocalAccounts()
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Account management is available only for the default local account database.");

            DataTable accounts = GetDataTable(
                "SELECT userID, username, gmLevel, CASE WHEN gmLevel > 0 THEN 'GM/Admin' ELSE 'Player' END AS role FROM user ORDER BY userID");
            if (accounts == null)
                throw new InvalidOperationException("The game account database could not be queried.");
            return accounts;
        }

        public int GetLocalGMLevel(uint userID)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Account management is available only for the default local account database.");

            DataTable account = GetDataTable(
                "SELECT gmLevel FROM user WHERE userID = @userID",
                new DbParam("@userID", userID));
            if (account == null)
                throw new InvalidOperationException("The game account database could not be queried.");
            if (account.Rows.Count == 0)
                return 0;
            return Convert.ToInt32(account.Rows[0]["gmLevel"], CultureInfo.InvariantCulture);
        }

        public bool SetLocalGMLevel(uint userID, int gmLevel)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Account management is available only for the default local account database.");
            if (userID == 0)
                throw new ArgumentOutOfRangeException("userID");
            if (gmLevel < 0 || gmLevel > 1)
                throw new ArgumentOutOfRangeException("gmLevel", "GM/Admin level must be 0 or 1.");

            return ExecuteNonQuery(
                "UPDATE user SET gmLevel = @gmLevel WHERE userID = @userID",
                new DbParam("@gmLevel", gmLevel),
                new DbParam("@userID", userID)) == 1;
        }

        public bool ResetLocalPassword(uint userID, string newPassword)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Password reset is available only for the default local account database.");
            if (userID == 0)
                throw new ArgumentOutOfRangeException("userID");
            ValidateLocalPassword(newPassword);

            return ExecuteNonQuery(
                "UPDATE user SET password = @password WHERE userID = @userID",
                new DbParam("@password", HashPassword(newPassword)),
                new DbParam("@userID", userID)) == 1;
        }

        public bool DeleteLocalAccount(uint userID)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Account management is available only for the default local account database.");
            if (userID == 0)
                throw new ArgumentOutOfRangeException("userID");

            return ExecuteNonQuery(
                "DELETE FROM user WHERE userID = @userID",
                new DbParam("@userID", userID)) == 1;
        }

        static void ValidateLocalPassword(string password)
        {
            if (password == null || password.Length < 4 || password.Length > 14)
                throw new ArgumentException("Passwords must be 4 to 14 characters long.", "password");
            for (int i = 0; i < password.Length; i++)
                if (char.IsControl(password[i]))
                    throw new ArgumentException("Passwords cannot contain control characters.", "password");
        }

        public bool ResetPassword(string username, string newPassword)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Password reset is available only for the default local account database.");

            if (username == null)
                throw new ArgumentException("Enter a username.", "username");

            username = username.Trim().ToLowerInvariant();

            if (newPassword == null || newPassword.Length < 4 || newPassword.Length > 14)
                throw new ArgumentException("Passwords must be 4 to 14 characters long.", "newPassword");
            
            for (int i = 0; i < newPassword.Length; i++)
            {
                if (char.IsControl(newPassword[i]))
                    throw new ArgumentException("Passwords cannot contain control characters.", "newPassword");
            }

            var userTable = GetDataTable("SELECT " + DataBaseID_Ref + " FROM " + TableName + " WHERE " + Username_Ref + " = @username", new DbParam("@username", username));

            if (userTable.Rows.Count == 0)
                return false;

            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add(Password_Ref, HashPassword(newPassword));

            try
            {
                string whereClause = Username_Ref + " = @username";
                var parameters = new[] { new DbParam("@username", username) };
                ExecuteNonQuery("UPDATE " + TableName + " SET " + Password_Ref + " = @password WHERE " + whereClause,
                    new DbParam("@password", HashPassword(newPassword)),
                    new DbParam("@username", username));
                return true;
            }
            catch (DbException)
            {
                return false;
            }
        }

        public bool VerifyAccount(string username, string password)
        {
            uint userID;
            string[] userData;
            return GetUserData(username, password, out userID, out userData);
        }

        public List<MallCharacterInfo> GetMallCharacters(uint userID)
        {
            uint gameUserID = checked(userID + 10000);
            uint firstCharacterID = gameUserID;
            uint secondCharacterID = checked(firstCharacterID + 4500000);
            DataTable rows = GetDataTable(
                "SELECT charID, name, slot FROM characters WHERE userID = @gameUserID AND (charID = @first OR charID = @second) ORDER BY slot",
                new DbParam("@gameUserID", gameUserID),
                new DbParam("@first", firstCharacterID),
                new DbParam("@second", secondCharacterID));
            if (rows == null)
                throw new InvalidOperationException("Unable to load characters from the game database.");

            List<MallCharacterInfo> characters = new List<MallCharacterInfo>();
            foreach (DataRow row in rows.Rows)
            {
                characters.Add(new MallCharacterInfo
                {
                    CharacterId = Convert.ToUInt32(row["charID"], CultureInfo.InvariantCulture),
                    Name = row["name"].ToString(),
                    Slot = Convert.ToByte(row["slot"], CultureInfo.InvariantCulture)
                });
            }
            return characters;
        }

        public int GetMallBalance(uint userID)
        {
            DataTable rows = GetDataTable(
                "SELECT IM FROM user WHERE userID = @userID",
                new DbParam("@userID", userID));
            if (rows == null || rows.Rows.Count == 0)
                throw new InvalidOperationException("The game account could not be found.");
            return Convert.ToInt32(rows.Rows[0]["IM"], CultureInfo.InvariantCulture);
        }

        public List<MallProductInfo> GetMallCatalog(DataFiles.PhxItemDat itemData)
        {
            if (itemData == null)
                throw new InvalidOperationException("Game item data is not loaded.");

            Dictionary<ushort, KeyValuePair<double, int>> ratings = new Dictionary<ushort, KeyValuePair<double, int>>();
            DataTable reviewRows = GetDataTable("SELECT itemID, AVG(rating) AS averageRating, COUNT(*) AS reviewCount FROM mall_reviews GROUP BY itemID");
            if (reviewRows == null)
                throw new InvalidOperationException("Unable to load Mall ratings.");
            foreach (DataRow row in reviewRows.Rows)
            {
                ushort itemID = Convert.ToUInt16(row["itemID"], CultureInfo.InvariantCulture);
                ratings[itemID] = new KeyValuePair<double, int>(
                    Convert.ToDouble(row["averageRating"], CultureInfo.InvariantCulture),
                    Convert.ToInt32(row["reviewCount"], CultureInfo.InvariantCulture));
            }

            List<MallProductInfo> products = new List<MallProductInfo>();
            foreach (DataFiles.PhxItemInfo item in itemData.GetItemList().ToList())
            {
                string category;
                string icon;
                int basePrice;
                if (item.ItemID == 0 || item.cellwidth != 1 || item.cellheight != 1 ||
                    !TryGetMallCategory(item.ItemType, out category, out icon, out basePrice))
                    continue;

                KeyValuePair<double, int> rating;
                ratings.TryGetValue(item.ItemID, out rating);
                products.Add(new MallProductInfo
                {
                    ItemId = item.ItemID,
                    Category = category,
                    BasePrice = basePrice,
                    Price = (basePrice * 90) / 100,
                    Name = "Game item #" + item.ItemID.ToString(CultureInfo.InvariantCulture),
                    Description = "A valid in-game item. Delivered when your selected character next logs in with an open inventory slot.",
                    Icon = icon,
                    AverageRating = rating.Key,
                    ReviewCount = rating.Value
                });
            }

            return products.OrderBy(product => product.ItemId).ToList();
        }

        static bool TryGetMallCategory(byte itemType, out string category, out string icon, out int basePrice)
        {
            switch ((DataFiles.eItemType)itemType)
            {
                case DataFiles.eItemType.Katana_Falchion:
                case DataFiles.eItemType.Sword:
                case DataFiles.eItemType.Spear:
                case DataFiles.eItemType.Bow:
                case DataFiles.eItemType.Wand_Staff:
                case DataFiles.eItemType.Claw:
                case DataFiles.eItemType.Ax:
                case DataFiles.eItemType.Gavel_Pestle:
                case DataFiles.eItemType.Fan:
                case DataFiles.eItemType.Gun:
                case DataFiles.eItemType.Body_Item:
                case DataFiles.eItemType.Head_Item:
                case DataFiles.eItemType.Arm_Item:
                case DataFiles.eItemType.Shoes_Item:
                case DataFiles.eItemType.Special_Item:
                    category = "equipment";
                    icon = "⚔";
                    basePrice = 2500;
                    return true;
                case DataFiles.eItemType.Pet_Fruit:
                    category = "pets";
                    icon = "🐾";
                    basePrice = 5000;
                    return true;
                case DataFiles.eItemType.Vehicle:
                    category = "mounts";
                    icon = "🛞";
                    basePrice = 3500;
                    return true;
                case DataFiles.eItemType.Prepared_Food:
                case DataFiles.eItemType.Battle_Item:
                case DataFiles.eItemType.Item:
                    category = "consumables";
                    icon = "✦";
                    basePrice = 1000;
                    return true;
                default:
                    category = null;
                    icon = null;
                    basePrice = 0;
                    return false;
            }
        }

        public MallPurchaseResult CreateMallPurchase(uint userID, uint charID, MallProductInfo product, string requestID)
        {
            if (product == null || string.IsNullOrEmpty(requestID))
                throw new ArgumentException("A valid product and request ID are required.");
            if (!LocalSetupVerified || ServType != DataBaseTypes.Sqlite || string.IsNullOrEmpty(DBFile))
                throw new InvalidOperationException("Mall checkout requires the verified local game database.");

            uint firstCharacterID = checked(userID + 10000);
            uint secondCharacterID = checked(firstCharacterID + 4500000);
            if (charID != firstCharacterID && charID != secondCharacterID)
                return new MallPurchaseResult { CharacterNotFound = true };

            return ExecuteMallTransaction((connection, transaction) =>
            {
                string existingOrderId = null;
                string existingStatus = null;
                using (DbCommand existingOrder = CreateTransactionCommand(connection, transaction,
                    "SELECT orderID, status FROM mall_orders WHERE userID = @userID AND requestID = @requestID",
                    new DbParam("@userID", userID),
                    new DbParam("@requestID", requestID)))
                {
                    using (DbDataReader reader = existingOrder.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingOrderId = reader["orderID"].ToString();
                            existingStatus = reader["status"].ToString();
                        }
                    }
                }

                if (existingOrderId != null)
                {
                    return new MallPurchaseResult
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        OrderId = existingOrderId,
                        Status = existingStatus,
                        Balance = ReadMallBalance(connection, transaction, userID)
                    };
                }

                using (DbCommand character = CreateTransactionCommand(connection, transaction,
                    "SELECT 1 FROM characters WHERE charID = @charID",
                    new DbParam("@charID", charID)))
                {
                    if (character.ExecuteScalar() == null)
                        return new MallPurchaseResult { CharacterNotFound = true };
                }

                using (DbCommand debit = CreateTransactionCommand(connection, transaction,
                    "UPDATE user SET IM = IM - @price WHERE userID = @userID AND IM >= @price",
                    new DbParam("@price", product.Price),
                    new DbParam("@userID", userID)))
                {
                    if (debit.ExecuteNonQuery() != 1)
                    {
                        return new MallPurchaseResult
                        {
                            InsufficientPoints = true,
                            Balance = ReadMallBalance(connection, transaction, userID)
                        };
                    }
                }

                string orderID = Guid.NewGuid().ToString("N");
                using (DbCommand order = CreateTransactionCommand(connection, transaction,
                    "INSERT INTO mall_orders (orderID, requestID, userID, charID, itemID, price, status, createdAt) VALUES (@orderID, @requestID, @userID, @charID, @itemID, @price, 'pending', @createdAt)",
                    new DbParam("@orderID", orderID),
                    new DbParam("@requestID", requestID),
                    new DbParam("@userID", userID),
                    new DbParam("@charID", charID),
                    new DbParam("@itemID", product.ItemId),
                    new DbParam("@price", product.Price),
                    new DbParam("@createdAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))))
                {
                    order.ExecuteNonQuery();
                }

                return new MallPurchaseResult
                {
                    Success = true,
                    OrderId = orderID,
                    Status = "pending",
                    Balance = ReadMallBalance(connection, transaction, userID)
                };
            });
        }

        public int ProcessMallDeliveries(uint charID, DataFiles.PhxItemDat itemData)
        {
            if (!LocalSetupVerified || ServType != DataBaseTypes.Sqlite || string.IsNullOrEmpty(DBFile))
                throw new InvalidOperationException("Mall delivery requires the verified local game database.");
            if (itemData == null)
                throw new InvalidOperationException("Game item data is not loaded.");

            Dictionary<ushort, DataFiles.PhxItemInfo> items = itemData.GetItemList()
                .GroupBy(item => item.ItemID)
                .ToDictionary(group => group.Key, group => group.First());

            return ExecuteMallTransaction((connection, transaction) =>
            {
                List<KeyValuePair<string, ushort>> pending = new List<KeyValuePair<string, ushort>>();
                using (DbCommand orders = CreateTransactionCommand(connection, transaction,
                    "SELECT orderID, itemID FROM mall_orders WHERE charID = @charID AND status = 'pending' ORDER BY createdAt",
                    new DbParam("@charID", charID)))
                {
                    using (DbDataReader reader = orders.ExecuteReader())
                    {
                        while (reader.Read())
                            pending.Add(new KeyValuePair<string, ushort>(reader["orderID"].ToString(), Convert.ToUInt16(reader["itemID"], CultureInfo.InvariantCulture)));
                    }
                }

                int delivered = 0;
                foreach (KeyValuePair<string, ushort> order in pending)
                {
                    DataFiles.PhxItemInfo item;
                    string category;
                    string icon;
                    int price;
                    if (!items.TryGetValue(order.Value, out item) || item.ItemID == 0 ||
                        item.cellwidth != 1 || item.cellheight != 1 ||
                        !TryGetMallCategory(item.ItemType, out category, out icon, out price))
                        continue;

                    int inventoryIndex;
                    using (DbCommand emptySlot = CreateTransactionCommand(connection, transaction,
                        "SELECT invIdx FROM inventory WHERE charID = @charID AND storID = 0 AND itemID = 0 ORDER BY pos LIMIT 1",
                        new DbParam("@charID", charID)))
                    {
                        using (DbDataReader reader = emptySlot.ExecuteReader())
                        {
                            if (!reader.Read())
                                continue;
                            inventoryIndex = Convert.ToInt32(reader["invIdx"], CultureInfo.InvariantCulture);
                        }
                    }

                    using (DbCommand grant = CreateTransactionCommand(connection, transaction,
                        "UPDATE inventory SET itemID = @itemID, dmg = 0, qty = 1, socketID = 0, bombID = 0, sewID = 0, forge = 0 WHERE charID = @charID AND storID = 0 AND invIdx = @invIdx AND itemID = 0",
                        new DbParam("@itemID", order.Value),
                        new DbParam("@charID", charID),
                        new DbParam("@invIdx", inventoryIndex)))
                    {
                        if (grant.ExecuteNonQuery() != 1)
                            continue;
                    }

                    using (DbCommand complete = CreateTransactionCommand(connection, transaction,
                        "UPDATE mall_orders SET status = 'delivered', deliveredAt = @deliveredAt WHERE orderID = @orderID AND status = 'pending'",
                        new DbParam("@deliveredAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
                        new DbParam("@orderID", order.Key)))
                    {
                        if (complete.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Mall delivery order changed while being processed.");
                    }
                    delivered++;
                }

                return delivered;
            });
        }

        public DataTable GetMallReviews(ushort itemID)
        {
            DataTable rows = GetDataTable(
                "SELECT u.username, r.rating, r.reviewText, r.createdAt FROM mall_reviews r JOIN user u ON u.userID = r.userID WHERE r.itemID = @itemID ORDER BY r.createdAt DESC LIMIT 50",
                new DbParam("@itemID", itemID));
            if (rows == null)
                throw new InvalidOperationException("Unable to load item reviews.");
            return rows;
        }

        public bool SaveMallReview(uint userID, ushort itemID, int rating, string reviewText)
        {
            if (rating < 1 || rating > 5 || reviewText == null || reviewText.Length > 500)
                throw new ArgumentException("Reviews must have a 1-5 rating and be no longer than 500 characters.");

            if (!LocalSetupVerified || ServType != DataBaseTypes.Sqlite || string.IsNullOrEmpty(DBFile))
                throw new InvalidOperationException("Mall reviews require the verified local game database.");

            return ExecuteMallTransaction((connection, transaction) =>
            {
                using (DbCommand purchased = CreateTransactionCommand(connection, transaction,
                    "SELECT 1 FROM mall_orders WHERE userID = @userID AND itemID = @itemID AND status = 'delivered' LIMIT 1",
                    new DbParam("@userID", userID),
                    new DbParam("@itemID", itemID)))
                {
                    if (purchased.ExecuteScalar() == null)
                        return false;
                }

                using (DbCommand review = CreateTransactionCommand(connection, transaction,
                    "INSERT OR REPLACE INTO mall_reviews (userID, itemID, rating, reviewText, createdAt) VALUES (@userID, @itemID, @rating, @reviewText, @createdAt)",
                    new DbParam("@userID", userID),
                    new DbParam("@itemID", itemID),
                    new DbParam("@rating", rating),
                    new DbParam("@reviewText", reviewText.Trim()),
                    new DbParam("@createdAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))))
                {
                    review.ExecuteNonQuery();
                }
                return true;
            });
        }

        public void SubscribeMallNewsletter(string email)
        {
            ExecuteNonQuery(
                "INSERT OR IGNORE INTO mall_newsletter (email, subscribedAt) VALUES (@email, @subscribedAt)",
                new DbParam("@email", email),
                new DbParam("@subscribedAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
        }

        static DbCommand CreateTransactionCommand(DbConnection connection, DbTransaction transaction, string sql, params DbParam[] parameters)
        {
            DbCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            foreach (DbParam value in parameters)
            {
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = value.identifier;
                parameter.Value = value.value;
                command.Parameters.Add(parameter);
            }
            return command;
        }

        T ExecuteMallTransaction<T>(Func<SQLiteConnection, SQLiteTransaction, T> operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");
            if (!LocalSetupVerified || ServType != DataBaseTypes.Sqlite || string.IsNullOrEmpty(DBFile))
                throw new InvalidOperationException("Mall transactions require the verified local game database.");

            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + DBFile + ";Version=3;"))
            {
                connection.Open();
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        T result = operation(connection, transaction);
                        transaction.Commit();
                        return result;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        static int ReadMallBalance(DbConnection connection, DbTransaction transaction, uint userID)
        {
            using (DbCommand balance = CreateTransactionCommand(connection, transaction,
                "SELECT IM FROM user WHERE userID = @userID",
                new DbParam("@userID", userID)))
            {
                object value = balance.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    throw new InvalidOperationException("The game account could not be found.");
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }
        
        public override bool VerifySaltedPassword(string password, string salt, string with)
        {
            return (hashMD5(hashMD5(salt) + hashMD5(password))== with);
        }

        public bool Update_Player_ID(uint user, UInt32 id, byte slot)
        {
            if (user == 0) return false;

            string col = "";
            switch (slot)
            {
                case 1: { col = CharacterID1_Ref; } break;
                case 2: { col = CharacterID2_Ref; } break;
            }

            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add(col, id.ToString());

            //try { Update(TableName, cols, UserID_Ref + " = '" + user + "'"); }
            //catch (MySqlException ex) { DebugSystem.Write(ex); return false; }

            return true;
        }

        public bool GetUserData(string user, string pass,out uint userID,out string[] userData)
        {
            DataRow[] rows = new DataRow[0];

            var src = GetDataTable("SELECT * FROM " + TableName + " WHERE " + Username_Ref + " = @id", new DbParam("@id", user));

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);

                switch (PassVerification)
                {
                    case VerifyPassType.None:
                         if (VerifyPassword(pass, rows[0][Password_Ref].ToString()))
                        {
                            UpgradeLegacyPassword(user, pass, rows[0][Password_Ref].ToString());
                            string ch = "";
                            if (rows[0][Char_Delete_Code_Ref] != DBNull.Value)
                                ch = rows[0][Char_Delete_Code_Ref].ToString();
                            uint.TryParse(rows[0][DataBaseID_Ref].ToString(), out userID);
                            string gmLevel = rows[0].Table.Columns.Contains("gmLevel") ? rows[0]["gmLevel"].ToString() : "0";
                            userData = new string[] { rows[0][Username_Ref].ToString(), ch, (rows[0][IM_Ref].ToString() == "") ? "0" : rows[0][IM_Ref].ToString(), gmLevel };
                            return true;
                        } break;
                    //if (BCrypt.Net.BCrypt.Verify(pass, rows[0][Password_Ref].ToString()))
                    //{
                    //    string ch = "0";
                    //    if (rows[0][Char_Delete_Code_Ref] != DBNull.Value)
                    //        ch = rows[0][Char_Delete_Code_Ref].ToString();

                    //    return new string[] { rows[0][Username_Ref].ToString(), ch, (rows[0][IM_Ref].ToString() == "")?"0":rows[0][IM_Ref].ToString()};
                    //}
                    case VerifyPassType.IPBoard_3x:
                        if (VerifySaltedPassword(pass, rows[0]["members_pass_salt"].ToString(), rows[0][Password_Ref].ToString()))
                        {
                            string ch = "";
                            if (rows[0][Char_Delete_Code_Ref] != DBNull.Value)
                                ch = rows[0][Char_Delete_Code_Ref].ToString();
                            uint.TryParse(rows[0][DataBaseID_Ref].ToString(), out userID);
                            string gmLevel = rows[0].Table.Columns.Contains("gmLevel") ? rows[0]["gmLevel"].ToString() : "0";
                            userData = new string[] { rows[0][Username_Ref].ToString(), ch, (rows[0][IM_Ref].ToString() == "") ? "0" : rows[0][IM_Ref].ToString(), gmLevel };
                            return true;
                        } break;
                }
            }
            userID = 0;
            userData = null;
            return false;
        }

        void UpgradeLegacyPassword(string username, string password, string storedPassword)
        {
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                storedPassword.StartsWith(PasswordHashPrefix, StringComparison.Ordinal))
                return;

            try
            {
                ExecuteNonQuery(
                    "UPDATE user SET password = @password WHERE username = @username AND password = @legacyPassword",
                    new DbParam("@password", HashPassword(password)),
                    new DbParam("@username", username),
                    new DbParam("@legacyPassword", storedPassword));
            }
            catch (Exception ex)
            {
                DebugSystem.Write(new ExceptionData(ex));
            }
        }

        static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider random = new RNGCryptoServiceProvider())
                random.GetBytes(salt);

            byte[] hash;
            using (Rfc2898DeriveBytes derivation = new Rfc2898DeriveBytes(password, salt, PasswordHashIterations))
                hash = derivation.GetBytes(32);

            return PasswordHashPrefix + PasswordHashIterations + "$" +
                Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        static bool VerifyHashedPassword(string password, string storedPassword)
        {
            if (password == null)
                return false;

            string[] parts = storedPassword.Split('$');
            int iterations;
            byte[] salt;
            byte[] expectedHash;
            if (parts.Length != 4 ||
                parts[0] != "wlo-pbkdf2-sha1" ||
                !int.TryParse(parts[1], out iterations) ||
                iterations != PasswordHashIterations)
                return false;

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (salt.Length != 16 || expectedHash.Length != 32)
                return false;

            byte[] actualHash;
            using (Rfc2898DeriveBytes derivation = new Rfc2898DeriveBytes(password, salt, iterations))
                actualHash = derivation.GetBytes(expectedHash.Length);

            int difference = 0;
            for (int i = 0; i < expectedHash.Length; i++)
                difference |= expectedHash[i] ^ actualHash[i];
            return difference == 0;
        }

        public int GetIMPoints(uint user)
        {
            DataTable src = null;
            DataRow[] rows = new DataRow[0];

            try
            {
                src = GetDataTable("SELECT * FROM " + TableName + " where " + DataBaseID_Ref + " = '" + user + "'"
                    );
            }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); return 0; }

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                return int.Parse(rows[0][IM_Ref].ToString());
            }
            return 0;
        }

        public bool UpdateUser(uint user, string delete = null, object im = null, object char1 = null, object char2 = null)
        {
            if (user == 0) return false;

            Dictionary<string, string> str = new Dictionary<string, string>();
            if (delete != null) str.Add(Char_Delete_Code_Ref, delete);
            if (im != null) str.Add(IM_Ref, im.ToString());
            if (char1 != null) str.Add(CharacterID1_Ref, char1.ToString());
            if (char2 != null) str.Add(CharacterID2_Ref, char2.ToString());

            //try { Update(TableName, str, UserID_Ref + " = '" + user + "'"); }
            //catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); return false; }

            return true;
        }

        public bool OnLogin(User usr)
        {
            return true;
        }
        public void OnLogOff(User usr)
        {
        }
    }


}
