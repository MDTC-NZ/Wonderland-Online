using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Globalization;
using MySql.Data.MySqlClient;
using System.Data;
using Game;
using Game.Bots;
using RCLibrary.Core;
using RCLibrary.Core.Networking;
using System.Reflection;
using Network;
using Game.Code;


namespace DataBase
{

    struct CharacterDataRequest
    {
        public uint ID;
        public DateTime RequestedAt;
        public bool isOld { get { return ((DateTime.Now - RequestedAt) > new TimeSpan(0, 0, 30)); } } 
        public Character Data;
    }


    public sealed class CharacterDataBase :RCLibrary.Core.DataBase
    {
        ConcurrentDictionary<int,Character> Characters_Online;
        List<CharacterDataRequest> CacheCharacters;

        const string DBServer = "CharacterDataBase";
        //DBConnector.DBOAuth DBAssist;

        public DataFiles.PhxItemDat ItemDat { private get; set; }
        public int OnlineCharacterCount { get { return Characters_Online.Count; } }

        List<string> client_requested_names = new List<string>();
          
       
        /// <summary>
        /// saves a cache of a character
        /// </summary>
        ConcurrentDictionary<int, Character> Cache = new ConcurrentDictionary<int,Character>();
        
        Dictionary<string, uint> CharNames = new Dictionary<string, uint>();

        public CharacterDataBase()
        {
            //DBAssist = new DBConnector.DBOAuth();
            Characters_Online = new ConcurrentDictionary<int, Character>();
            CacheCharacters = new List<CharacterDataRequest>();
            DebugSystem.Write("[Init] - Configuring restricted Names");
            Setup();
        }

        public void ConfigureSqlite(string path)
        {
            ServType = RCLibrary.Core.DataBaseTypes.Sqlite;
            DBFile = path;
        }

        void Setup()
        {
            #region Restricted Name
            CharNames.Add("tatsuya", 3322);//Jay
            CharNames.Add("compass", 3322);//Compass
            CharNames.Add("sharky", 3322);//Sharky
            CharNames.Add("rcharnel", 3322);//rcharnel
            CharNames.Add("Petron", 3322);//petron
            CharNames.Add("nipple", 3322);//Johnathan
            CharNames.Add("dragon", 3322);//Dragon
            CharNames.Add("maganda", 3322);//Maganda
            CharNames.Add("flurrih", 3132);//Flurrih
            #endregion
        }

        public void VerifySetup()
        {

            #region characters Columns
            Dictionary<string, string> col = new Dictionary<string, string>();
            col.Add("charID", "int/NN/PK");
            col.Add("userID", "int/NN");
            col.Add("slot", "int/NN");
            col.Add("head", "int/NN");
            col.Add("body", "int/NN");
            col.Add("name", "text");
            col.Add("name_clean", "text");
            col.Add("nickname", "text");
            col.Add("location_map", "int/NN");
            col.Add("location_x", "int/NN");
            col.Add("location_y", "int/NN");
            col.Add("haircolor", "int/NN");
            col.Add("skincolor", "int/NN");
            col.Add("clothingcolor", "int/NN");
            col.Add("eyecolor", "int/NN");
            col.Add("gold", "int/NN");
            col.Add("element", "int/NN");
            col.Add("rebirth", "int/NN");
            col.Add("job", "int/NN");
            col.Add("online", "int/NN");

            #endregion

            #region charextdata Columns
            Dictionary<string, string> extdtcol = new Dictionary<string, string>();
            extdtcol.Add("charID", "int/NN/PK");
            extdtcol.Add("Settings", "text");
            extdtcol.Add("Friends", "text");
            extdtcol.Add("Guild", "text");
            extdtcol.Add("Mail", "text");
            #endregion

            #region chartent Columns
            Dictionary<string, string> chartent = new Dictionary<string, string>();
            chartent.Add("charID", "int/NN/PK");
            chartent.Add("locked", "int");
            chartent.Add("enlarged", "int");
            chartent.Add("tenttype", "int");
            chartent.Add("floor1Color", "int");
            chartent.Add("floor1wallpaper", "int");
            chartent.Add("floor2Color", "int");
            chartent.Add("floor2wallpaperr", "int");
            #endregion

            #region charquest Columns
            Dictionary<string, string> charquest = new Dictionary<string, string>();
            charquest.Add("pri_key", "int/NN/AI/PK");
            charquest.Add("charID", "int/NN");
            charquest.Add("quest_started", "int");
            charquest.Add("quest_pos", "int");
            #endregion

            #region charunlocks Columns
            Dictionary<string, string> charunlocks = new Dictionary<string, string>();
            charunlocks.Add("pri_key", "int/NN/AI/PK");
            charunlocks.Add("charID", "int/NN");
            charunlocks.Add("maploc", "int");
            charunlocks.Add("clickID", "int");
            #endregion

            #region inv
            Dictionary<string, string> inv = new Dictionary<string, string>();
            inv.Add("pri_key", "int/NN/AI/PK");
            inv.Add("invIdx", "int/NN");
            inv.Add("charID", "int");
            inv.Add("storID", "int");
            inv.Add("itemID", "int");
            inv.Add("dmg", "int");
            inv.Add("qty", "int");
            inv.Add("pos", "int");
            inv.Add("socketID", "int");
            inv.Add("bombID", "int");
            inv.Add("sewID", "int");
            inv.Add("forge", "int");
            #endregion

            #region stats
            Dictionary<string, string> stats = new Dictionary<string, string>();
            stats.Add("pri_key", "int/NN/AI/PK");
            stats.Add("statIdx", "int/NN");
            stats.Add("charID", "int");
            stats.Add("statID", "int");
            stats.Add("StatusUp", "int");
            stats.Add("potential", "int");
            #endregion

            #region characters table Verification
            DebugSystem.Write("Checking for characters table");

            if (GetDataTable("SELECT * FROM characters") != null) goto exist;

            DebugSystem.Write("Setuping up characters table");

            string nonsqlite_prikey = "";
            string cmstr = "create table characters (";

            foreach (var t in col)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch (ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "AI": str += "AUTO_INCREMENT "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


            ExecuteNonQuery(cmstr);

        exist:
            DebugSystem.Write("Found characters table");
            if (GetDataTable("SELECT userID FROM characters WHERE 1 = 0") == null)
            {
                string columnType = ServType == RCLibrary.Core.DataBaseTypes.Sqlite ? "INTEGER" : "int(11)";
                ExecuteNonQuery("ALTER TABLE characters ADD COLUMN userID " + columnType + " NOT NULL DEFAULT 0");
                ExecuteNonQuery("UPDATE characters SET userID = CASE WHEN slot = 1 THEN charID WHEN slot = 2 THEN charID - 4500000 ELSE 0 END WHERE userID = 0");
            }
        DebugSystem.Write("Verifying characters columns");
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in col.Keys)
            {
                if (GetDataTable("select " + h + " from characters") == null)
                {
                    throw new InvalidOperationException("The characters table is missing column " + h + ".");
                }
            }
            #endregion

            #region charactersextdata Verification
        DebugSystem.Write("Checking for charactersextdata table");


            if (GetDataTable("SELECT * FROM charactersextdata") != null) goto exist2;

            DebugSystem.Write("Setuping up charactersextdata table");

            nonsqlite_prikey = "";
            cmstr = "create table " + "charactersextdata" + " (";

            foreach (var t in extdtcol)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch ( ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


            ExecuteNonQuery(cmstr);

        exist2:
            DebugSystem.Write("Found charactersextdata table");
        DebugSystem.Write("Verifying charactersextdata columns");
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in extdtcol.Keys)
            {
                if (GetDataTable("select " + h + " from " + "charactersextdata") == null)
                {
                    throw new InvalidOperationException("The charactersextdata table is missing column " + h + ".");
                }
            }
            #endregion

            #region chartent Verification
        DebugSystem.Write("Checking for chartent table");

            if (GetDataTable("SELECT * FROM " + "chartent") != null) goto exist3;

            DebugSystem.Write("Setuping up chartent table");

            nonsqlite_prikey = "";
            cmstr = "create table " + "chartent" + " (";

            foreach (var t in chartent)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch (ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


            ExecuteNonQuery(cmstr);

        exist3:
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in chartent.Keys)
            {
                if (GetDataTable("select " + h + " from " + "chartent") == null)
                {
                    throw new InvalidOperationException("The chartent table is missing column " + h + ".");
                }
            }
            #endregion

            #region charquest Verification
        DebugSystem.Write("Checking for charquest table");

            if (GetDataTable("SELECT * FROM " + "charquest") != null) goto exist4;

            DebugSystem.Write("Setuping up charquest table");

            nonsqlite_prikey = "";
            cmstr = "create table " + "charquest" + " (";

            foreach (var t in charquest)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch ( ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


           ExecuteNonQuery(cmstr);

        exist4:
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in charquest.Keys)
            {
                if (GetDataTable("select " + h + " from " + "charquest") == null)
                {
                    throw new InvalidOperationException("The charquest table is missing column " + h + ".");
                }
            }
            #endregion

            #region charunlocks Verification
        DebugSystem.Write("Checking for charunlocks table");

            if (GetDataTable("SELECT * FROM " + "charunlocks") != null) goto exist5;

            DebugSystem.Write("Setuping up charunlocks table");

            nonsqlite_prikey = "";
            cmstr = "create table " + "charunlocks" + " (";

            foreach (var t in charunlocks)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch (ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


            ExecuteNonQuery(cmstr);

        exist5:
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in charunlocks.Keys)
            {
                if (GetDataTable("select " + h + " from " + "charunlocks") == null)
                {
                    throw new InvalidOperationException("The charunlocks table is missing column " + h + ".");
                }
            }
            #endregion

            #region inv Verification
        DebugSystem.Write("Checking for inventory table");

            if (GetDataTable("SELECT * FROM " + "inventory") != null) goto exist6;

            DebugSystem.Write("Setuping up inventory table");

            nonsqlite_prikey = "";
            cmstr = "create table " + "inventory" + " (";

            foreach (var t in inv)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch (ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


            ExecuteNonQuery(cmstr);

        exist6:
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in inv.Keys)
            {
                if (GetDataTable("select " + h + " from " + "inventory") == null)
                {
                    throw new InvalidOperationException("The inventory table is missing column " + h + ".");
                }
            }
            #endregion

            #region stats Verification
        DebugSystem.Write("Checking for stats table");

            if (GetDataTable("SELECT * FROM " + "stats") != null) goto exist7;

            DebugSystem.Write("Setuping up stats table");

            nonsqlite_prikey = "";
            cmstr = "create table " + "stats" + " (";

            foreach (var t in stats)
            {
                var str = "";
                var att = t.Value.Split('/');

                switch (ServType)
                {
                    #region Mysql
                    case RCLibrary.Core.DataBaseTypes.MySQl:
                        {
                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "text "; break;
                                    case "int": str += "int(11) "; break;
                                    case "NN": str += "NOT NULL "; break;
                                    case "PK": nonsqlite_prikey = "PRIMARY KEY (" + t.Key + ")"; break;
                                }
                        } break;
                    #endregion
                    #region Sqlite
                    case RCLibrary.Core.DataBaseTypes.Sqlite:
                        {
                            if (att.Count(c => c == "pk") > 0 && att.Count(c => c == "NN") > 0)
                                att = att.Where(c => c != "NN").ToArray();

                            foreach (var a in att)
                                switch (a)
                                {
                                    case "text": str += "TEXT "; break;
                                    case "int": str += "INTEGER "; break;
                                    case "PK": str += "PRIMARY KEY "; break;
                                }
                        } break;
                    #endregion
                }

                cmstr += string.Format("{0} {1},", t.Key, str);
            }

            if (nonsqlite_prikey != "")
                cmstr += string.Format("{0},", nonsqlite_prikey);

            cmstr = cmstr.Substring(0, cmstr.Length - 1);

            if (ServType == RCLibrary.Core.DataBaseTypes.MySQl)
                cmstr += ") ENGINE=InnoDB DEFAULT CHARSET=utf8;";
            else if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
                cmstr += ");";


            ExecuteNonQuery(cmstr);

        exist7:
            DebugSystem.Write("Found stats table");
        DebugSystem.Write("Verifying stats columns");
            //table exists verify columns  
            //table exists verify columns  
            foreach (string h in stats.Keys)
            {
                if (GetDataTable("select " + h + " from " + "stats") == null)
                {
                    throw new InvalidOperationException("The stats table is missing column " + h + ".");
                }
            }
            #endregion

            EnsureCharacterIndexes();
        }

        void EnsureCharacterIndexes()
        {
            if (ServType == RCLibrary.Core.DataBaseTypes.Sqlite)
            {
                ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx_characters_userid_charid ON characters (userID, charID)");
                ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx_stats_charid ON stats (charID)");
                ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx_inventory_charid_storid ON inventory (charID, storID)");
                return;
            }

            if (ServType != RCLibrary.Core.DataBaseTypes.MySQl)
                return;

            EnsureMySqlIndex("characters", "idx_characters_userid_charid", "userID, charID");
            EnsureMySqlIndex("stats", "idx_stats_charid", "charID");
            EnsureMySqlIndex("inventory", "idx_inventory_charid_storid", "charID, storID");
        }

        void EnsureMySqlIndex(string table, string index, string columns)
        {
            DataTable indexes = GetDataTable("SHOW INDEX FROM " + table + " WHERE Key_name = '" + index + "'");
            if (indexes == null)
                throw new InvalidOperationException("Unable to verify the " + table + " character lookup index.");
            if (indexes.Rows.Count == 0)
                ExecuteNonQuery("CREATE INDEX " + index + " ON " + table + " (" + columns + ")");
        }

        public bool LockName(uint dbacc, string name)
        {
            if (name == null) return false;

            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            parameters.Add(new KeyValuePair<string, object>("@myname", name.ToLower()));

            DataTable src = null;

            if (!client_requested_names.Contains(name.ToLower()))
            {
                try { src = GetDataTable("SELECT * FROM characters where name_clean = @myname", new DbParam("@myname", name.ToLower())); }
                catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }

                if (src.Rows.Count == 0)
                {

                    if (CharNames.ContainsKey(name.ToLower()))
                    {
                        if (CharNames[name.ToLower()] != dbacc) return false;
                        else
                        {
                            client_requested_names.Add(name.ToLower()); return true;
                        }
                    }
                    else
                        client_requested_names.Add(name.ToLower()); return true;
                }
                return false;
            }
            else
                return false;
        }

        public void unLockName(string name)
        {
            if (name == null) return;
            if (client_requested_names.Contains(name.ToLower()))
                client_requested_names.Remove(name.ToLower());
        }

        //public bool NameTaken(string name,uint target)
        //{
        //    MySqlCommand cmd = null;
        //    MySqlDataReader reader = null;
        //    DataTable src = null;
        //    DataRow[] rows = new DataRow[0];

        //    MySqlConnection conn = GenerateConn();
        //    try { conn.Open(); }
        //    catch (MySqlException f) { DebugSystem.Write(f); throw; }

        //    cmd = new MySqlCommand("SELECT * FROM characters where name = @myname", conn);
        //    cmd.Parameters.AddWithValue("@myname", name);
        //    try { reader = cmd.ExecuteReader(); }
        //    catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }          
        //    src = new DataTable();
        //    src.Load(reader);
        //    if (src.Rows.Count != 0)
        //    {
        //        cmd = new MySqlCommand("SELECT * FROM characters where charID =" + target, conn);
        //        cmd.Parameters.AddWithValue("@myname", name);
        //        try { reader = cmd.ExecuteReader(); }
        //        catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
        //        src = new DataTable();
        //        src.Load(reader);
        //        if (src.Rows.Count != 0)
        //            return !(src.Rows[0]["name_clean"].ToString() == name.ToLower());
        //        else
        //            return true;
        //    }            
        //    conn.Close();
        //    return false;
        //}
        //public bool ApplyNameChange(uint charID, string name, out Exception err)
        //{
        //    MySqlCommand cmd = null;
        //    DataRow[] rows = new DataRow[0];

        //    MySqlConnection conn = GenerateConn();
        //    try { conn.Open(); }
        //    catch (MySqlException f) { DebugSystem.Write(f); err = f; return false; }

        //    cmd = new MySqlCommand("UPDATE characters SET name = @myname, name_clean=@myname2 where charID ='" + charID + "'", conn);
        //    cmd.Parameters.AddWithValue("@myname", name);
        //    cmd.Parameters.AddWithValue("@myname2", name.ToLower());
        //    try { err = null; return (cmd.ExecuteNonQuery() == 1); }
        //    catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); err = ex; return false; }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //}

        public void DeleteCharacter(UInt32 ID)
        {
            string condition = "charID = '" + ID + "';";
            string[] tables = { "charactersextdata", "chartent", "charquest", "charunlocks", "stats", "inventory", "characters" };
            foreach (string table in tables)
            {
                try { Delete(table, condition); }
                catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
            }

            if (Cache.ContainsKey((int)ID))
            {
                Character t;
                Cache.TryRemove((int)ID, out t);
            }
        }

        public bool DeleteWebCharacter(uint databaseID, uint charID)
        {
            uint gameUserID = checked(databaseID + 10000);
            if (charID != gameUserID && (ulong)charID != (ulong)gameUserID + 4500000UL)
                return false;

            DataTable rows = GetDataTable(
                "SELECT online FROM characters WHERE charID = @charID AND userID = @userID",
                new DbParam("@charID", charID),
                new DbParam("@userID", gameUserID));
            if (rows == null)
                throw new InvalidOperationException("Unable to verify character ownership.");
            if (rows.Rows.Count == 0)
                return false;
            if (Convert.ToInt32(rows.Rows[0]["online"], CultureInfo.InvariantCulture) != 0 ||
                Characters_Online.ContainsKey(checked((int)charID)))
                throw new InvalidOperationException("Log out of this character before deleting it.");

            DeleteCharacter(charID);
            return true;
        }

        public bool HasWebCharacters(uint databaseID)
        {
            uint gameUserID = checked(databaseID + 10000);
            DataTable rows = GetDataTable(
                "SELECT 1 FROM characters WHERE userID = @userID LIMIT 1",
                new DbParam("@userID", gameUserID));
            if (rows == null)
                throw new InvalidOperationException("Unable to verify the account's characters.");
            return rows.Rows.Count != 0;
        }

        public List<WebCharacterInfo> GetWebCharacters(uint databaseID)
        {
            uint gameUserID = checked(databaseID + 10000);
            uint secondCharacterID = checked(gameUserID + 4500000);
            DataTable characterRows = GetDataTable(
                "SELECT charID, name, slot, body, head, element, gold, location_map, location_x, location_y, online " +
                "FROM characters WHERE userID = @userID AND (charID = @first OR charID = @second) ORDER BY slot",
                new DbParam("@userID", gameUserID),
                new DbParam("@first", gameUserID),
                new DbParam("@second", secondCharacterID));
            if (characterRows == null)
                throw new InvalidOperationException("Unable to load characters from the game database.");

            List<WebCharacterInfo> characters = new List<WebCharacterInfo>();
            foreach (DataRow row in characterRows.Rows)
            {
                uint charID = Convert.ToUInt32(row["charID"], CultureInfo.InvariantCulture);
                WebCharacterInfo character = new WebCharacterInfo
                {
                    CharacterId = charID,
                    Name = row["name"].ToString(),
                    Slot = Convert.ToByte(row["slot"], CultureInfo.InvariantCulture),
                    IsOnline = Convert.ToInt32(row["online"], CultureInfo.InvariantCulture) != 0,
                    Body = Convert.ToByte(row["body"], CultureInfo.InvariantCulture),
                    Head = Convert.ToByte(row["head"], CultureInfo.InvariantCulture),
                    Element = Convert.ToByte(row["element"], CultureInfo.InvariantCulture),
                    Gold = Convert.ToUInt32(row["gold"], CultureInfo.InvariantCulture),
                    LocationMap = Convert.ToUInt16(row["location_map"], CultureInfo.InvariantCulture),
                    LocationX = Convert.ToUInt16(row["location_x"], CultureInfo.InvariantCulture),
                    LocationY = Convert.ToUInt16(row["location_y"], CultureInfo.InvariantCulture),
                    EquipmentItemIds = new List<ushort>()
                };
                Character liveCharacter;
                if (Characters_Online.TryGetValue(checked((int)charID), out liveCharacter))
                {
                    character.IsOnline = true;
                    character.Level = liveCharacter.Level;
                    character.LocationMap = liveCharacter.CurMap == null
                        ? liveCharacter.LoginMap
                        : checked((ushort)liveCharacter.CurMap.MapID);
                    character.LocationX = liveCharacter.CurX;
                    character.LocationY = liveCharacter.CurY;
                }
                else
                {
                    Character savedCharacter = GetCharacterData(charID);
                    if (savedCharacter != null)
                        character.Level = savedCharacter.Level;
                }

                DataTable stats = GetDataTable(
                    "SELECT statID, StatusUp FROM stats WHERE charID = @charID",
                    new DbParam("@charID", charID));
                if (stats == null)
                    throw new InvalidOperationException("Unable to load character stats.");
                foreach (DataRow stat in stats.Rows)
                {
                    int statID = Convert.ToInt32(stat["statID"], CultureInfo.InvariantCulture);
                    long value = Convert.ToInt64(stat["StatusUp"], CultureInfo.InvariantCulture);
                    switch (statID)
                    {
                        case 25: character.CurrentHp = checked((int)value); break;
                        case 26: character.CurrentSp = checked((int)value); break;
                        case 27: character.Intelligence = checked((int)value); break;
                        case 28: character.Strength = checked((int)value); break;
                        case 29: character.Constitution = checked((int)value); break;
                        case 30: character.Agility = checked((int)value); break;
                        case 33: character.Wisdom = checked((int)value); break;
                        case 36: character.Experience = value; break;
                    }
                }

                DataTable equipment = GetDataTable(
                    "SELECT itemID FROM inventory WHERE charID = @charID AND storID = 1 AND itemID > 0 ORDER BY pos",
                    new DbParam("@charID", charID));
                if (equipment == null)
                    throw new InvalidOperationException("Unable to load character equipment.");
                foreach (DataRow item in equipment.Rows)
                    character.EquipmentItemIds.Add(Convert.ToUInt16(item["itemID"], CultureInfo.InvariantCulture));

                characters.Add(character);
            }
            return characters;
        }

        public bool UpdateWebCharacter(uint charID, long experience, int hp, int sp,
            int strength, int constitution, int intelligence, int wisdom, int agility,
            uint gold, ushort map, ushort x, ushort y)
        {
            if (charID == 0)
                throw new ArgumentOutOfRangeException("charID");
            if (experience < 0 || experience > uint.MaxValue)
                throw new ArgumentOutOfRangeException("experience", "Experience must be between 0 and 4,294,967,295.");
            if (hp < 0 || hp > ushort.MaxValue || sp < 0 || sp > ushort.MaxValue ||
                strength < 0 || strength > ushort.MaxValue ||
                constitution < 0 || constitution > ushort.MaxValue ||
                intelligence < 0 || intelligence > ushort.MaxValue ||
                wisdom < 0 || wisdom > ushort.MaxValue ||
                agility < 0 || agility > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("stats", "Character stats must be between 0 and 65,535.");

            DataTable rows = GetDataTable(
                "SELECT online FROM characters WHERE charID = @charID",
                new DbParam("@charID", charID));
            if (rows == null)
                throw new InvalidOperationException("Unable to verify the character.");
            if (rows.Rows.Count == 0)
                return false;
            if (Convert.ToInt32(rows.Rows[0]["online"], CultureInfo.InvariantCulture) != 0 ||
                Characters_Online.ContainsKey(checked((int)charID)))
                throw new InvalidOperationException("Log out of this character before editing it.");

            ExecuteNonQuery(
                "UPDATE characters SET gold = @gold, location_map = @map, location_x = @x, location_y = @y " +
                "WHERE charID = @charID",
                new DbParam("@gold", gold),
                new DbParam("@map", map),
                new DbParam("@x", x),
                new DbParam("@y", y),
                new DbParam("@charID", charID));

            SetAdminCharacterStat(charID, 25, hp);
            SetAdminCharacterStat(charID, 26, sp);
            SetAdminCharacterStat(charID, 27, intelligence);
            SetAdminCharacterStat(charID, 28, strength);
            SetAdminCharacterStat(charID, 29, constitution);
            SetAdminCharacterStat(charID, 30, agility);
            SetAdminCharacterStat(charID, 33, wisdom);
            SetAdminCharacterStat(charID, 36, experience);

            Character removed;
            Cache.TryRemove(checked((int)charID), out removed);
            return true;
        }

        void SetAdminCharacterStat(uint charID, int statID, long value)
        {
            DataTable rows = GetDataTable(
                "SELECT 1 FROM stats WHERE charID = @charID AND statID = @statID",
                new DbParam("@charID", charID),
                new DbParam("@statID", statID));
            if (rows == null)
                throw new InvalidOperationException("Unable to update character stats.");

            if (rows.Rows.Count == 0)
            {
                ExecuteNonQuery(
                    "INSERT INTO stats (statIdx, charID, statID, StatusUp, potential) VALUES (0, @charID, @statID, @value, 0)",
                    new DbParam("@charID", charID),
                    new DbParam("@statID", statID),
                    new DbParam("@value", value));
            }
            else
            {
                ExecuteNonQuery(
                    "UPDATE stats SET StatusUp = @value WHERE charID = @charID AND statID = @statID",
                    new DbParam("@value", value),
                    new DbParam("@charID", charID),
                    new DbParam("@statID", statID));
            }
        }

        public Character GetCharacterData(uint charID)
        {
            bool good = true;

            if (Cache.ContainsKey((int)charID))
                return Cache[(int)charID];

            Character t = new Character();

            DataTable src = null;

            DataRow[] rows;

            #region Get Character
            try { src = GetDataTable("SELECT * FROM characters where charID = '" + charID + "'"); }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                t.Slot = byte.Parse(rows[0]["slot"].ToString());
                t.CharID = uint.Parse(rows[0]["charID"].ToString());
                t.Head = byte.Parse(rows[0]["head"].ToString());
                t.Body = (BodyStyle)uint.Parse(rows[0]["body"].ToString());
                t.CharName = rows[0]["name"].ToString();
                t.NickName = rows[0]["nickname"].ToString();
                t.LoginMap = ushort.Parse(rows[0]["location_map"].ToString());
                t.CurX = ushort.Parse(rows[0]["location_x"].ToString());
                t.CurY = ushort.Parse(rows[0]["location_y"].ToString());
                t.HairColor = ushort.Parse(rows[0]["haircolor"].ToString());
                t.SkinColor = ushort.Parse(rows[0]["skincolor"].ToString());
                t.ClothingColor = ushort.Parse(rows[0]["clothingcolor"].ToString());
                t.EyeColor = ushort.Parse(rows[0]["eyecolor"].ToString());
                t.SetGold(int.Parse(rows[0]["gold"].ToString()));
                t.Element = (Affinity)byte.Parse(rows[0]["element"].ToString());
                t.Job = (RebornJob)byte.Parse(rows[0]["job"].ToString());
            }
            else
            {
                DebugSystem.Write(DBServer + "No Character Data found for " + charID);
                return null;
            }
            #endregion

            #region load stat data
            src = GetDataTable("SELECT * FROM stats where charID = '" + charID + "'");

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                List<long[]> tmp2 = new List<long[]>();
                if (t == null) t = new Character();
                foreach (var row in rows)
                {
                    switch (long.Parse(row["statID"].ToString()))
                    {
                        case 25: t.CurHP = int.Parse(row["StatusUp"].ToString()); break;
                        case 26: t.CurSP = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 38: t.SkillPoints = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 36: t.TotalExp = long.Parse(row["StatusUp"].ToString()); break;
                        default: t.SetBaseStat(row["statID"], ushort.Parse(row["StatusUp"].ToString())); break;
                    }
                }
            }
            #endregion

            #region load equips
            src = GetDataTable("SELECT * FROM inventory where charID = '" + charID + "' AND storID =1");

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];

                src.Rows.CopyTo(rows, 0);

                ushort id;

                for (int i = 0; i < rows.Length; i++)
                {
                    id = ushort.Parse(rows[i]["itemID"].ToString());
                    if (id != 0)
                    {
                        t[byte.Parse(rows[i]["pos"].ToString())].CopyFrom(ItemDat.GetItemByID(id));
                        t[byte.Parse(rows[i]["pos"].ToString())].Ammt = 1;
                        t[byte.Parse(rows[i]["pos"].ToString())].Damage = byte.Parse(rows[i]["dmg"].ToString());
                        //                    tmp4.Add((byte)i, new string[]{id.ToString(), rows[i]["socketID"].ToString(), rows[i]["bombID"].ToString(),rows[i]["sewID"].ToString(), 
                        //rows[i]["dmg"].ToString(),rows[i]["forge"].ToString(), });
                    }
                }
            }
            #endregion

            return t;
        }

        public Character GetCharacterData(uint charID, uint userID)
        {
            if (charID == 0) return null;
            if (charID != userID && (ulong)charID != (ulong)userID + 4500000UL) return null;

            if (Cache.ContainsKey((int)charID))
                return Cache[(int)charID];

            Character t = new Character();
            DataTable src = null;
            DataRow[] rows;

            try { src = GetDataTable("SELECT * FROM characters where charID = '" + charID + "' AND userID = '" + userID + "'"); }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }

            if (src.Rows.Count == 0)
            {
                DebugSystem.Write(DBServer + "No Character Data found for charID=" + charID + " userID=" + userID);
                return null;
            }

            rows = new DataRow[src.Rows.Count];
            src.Rows.CopyTo(rows, 0);
            t.Slot = byte.Parse(rows[0]["slot"].ToString());
            t.CharID = uint.Parse(rows[0]["charID"].ToString());
            t.Head = byte.Parse(rows[0]["head"].ToString());
            t.Body = (BodyStyle)uint.Parse(rows[0]["body"].ToString());
            t.CharName = rows[0]["name"].ToString();
            t.NickName = rows[0]["nickname"].ToString();
            t.LoginMap = ushort.Parse(rows[0]["location_map"].ToString());
            t.CurX = ushort.Parse(rows[0]["location_x"].ToString());
            t.CurY = ushort.Parse(rows[0]["location_y"].ToString());
            t.HairColor = ushort.Parse(rows[0]["haircolor"].ToString());
            t.SkinColor = ushort.Parse(rows[0]["skincolor"].ToString());
            t.ClothingColor = ushort.Parse(rows[0]["clothingcolor"].ToString());
            t.EyeColor = ushort.Parse(rows[0]["eyecolor"].ToString());
            t.SetGold(int.Parse(rows[0]["gold"].ToString()));
            t.Element = (Affinity)byte.Parse(rows[0]["element"].ToString());
            t.Job = (RebornJob)byte.Parse(rows[0]["job"].ToString());

            #region load stat data
            src = GetDataTable("SELECT * FROM stats where charID = '" + charID + "'");

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                List<long[]> tmp2 = new List<long[]>();
                if (t == null) t = new Character();
                foreach (var row in rows)
                {
                    switch (long.Parse(row["statID"].ToString()))
                    {
                        case 25: t.CurHP = int.Parse(row["StatusUp"].ToString()); break;
                        case 26: t.CurSP = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 38: t.SkillPoints = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 36: t.TotalExp = long.Parse(row["StatusUp"].ToString()); break;
                        default: t.SetBaseStat(row["statID"], ushort.Parse(row["StatusUp"].ToString())); break;
                    }
                }
            }
            #endregion

            #region load equips
            src = GetDataTable("SELECT * FROM inventory where charID = '" + charID + "' AND storID =1");

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                ushort id;

                for (int i = 0; i < rows.Length; i++)
                {
                    id = ushort.Parse(rows[i]["itemID"].ToString());
                    if (id != 0)
                    {
                        t[byte.Parse(rows[i]["pos"].ToString())].CopyFrom(ItemDat.GetItemByID(id));
                        t[byte.Parse(rows[i]["pos"].ToString())].Ammt = 1;
                        t[byte.Parse(rows[i]["pos"].ToString())].Damage = byte.Parse(rows[i]["dmg"].ToString());
                    }
                }
            }
            #endregion

            return t;
        }

        public bool GetCharacterData(uint charID, ref Player t)
        {
            if (charID == 0) return false;

            uint userID = t.UserAcc?.DataBaseID + 10000 ?? 0;
            return GetCharacterData(charID, userID, ref t);
        }

        public bool GetCharacterData(uint charID, uint userID, ref Player t)
        {
            if (charID == 0) return false;
            if (charID != userID && (ulong)charID != (ulong)userID + 4500000UL) return false;

            if (Cache.ContainsKey((int)charID))
            {
                t.CharID = Cache[(int)charID].CharID;
                t.Head = Cache[(int)charID].Head;
                t.Body = Cache[(int)charID].Body;
                t.CharName = Cache[(int)charID].CharName;
                t.NickName = Cache[(int)charID].NickName;
                t.LoginMap = Cache[(int)charID].LoginMap;
                t.CurX = Cache[(int)charID].CurX;
                t.CurY = Cache[(int)charID].CurY;
                t.HairColor = Cache[(int)charID].HairColor;
                t.SkinColor = Cache[(int)charID].SkinColor;
                t.ClothingColor = Cache[(int)charID].ClothingColor;
                t.EyeColor = Cache[(int)charID].EyeColor;
                t.SetGold((int)Cache[(int)charID].Gold);
                t.Element = Cache[(int)charID].Element;
                t.Job = Cache[(int)charID].Job;
                return true;
            }

            DataTable src = null;
            DataRow[] rows = new DataRow[0];

            try { src = GetDataTable("SELECT * FROM characters where charID = '" + charID + "' AND userID = '" + userID + "'"); }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                t.CharID = uint.Parse(rows[0]["charID"].ToString());
                t.Head = byte.Parse(rows[0]["head"].ToString());
                t.Body = (BodyStyle)uint.Parse(rows[0]["body"].ToString());
                t.CharName = rows[0]["name"].ToString();
                t.NickName = rows[0]["nickname"].ToString();
                t.LoginMap = ushort.Parse(rows[0]["location_map"].ToString());
                t.CurX = ushort.Parse(rows[0]["location_x"].ToString());
                t.CurY = ushort.Parse(rows[0]["location_y"].ToString());
                t.HairColor = ushort.Parse(rows[0]["haircolor"].ToString());
                t.SkinColor = ushort.Parse(rows[0]["skincolor"].ToString());
                t.ClothingColor = ushort.Parse(rows[0]["clothingcolor"].ToString());
                t.EyeColor = ushort.Parse(rows[0]["eyecolor"].ToString());
                t.SetGold(int.Parse(rows[0]["gold"].ToString()));
                t.Element = (Affinity)byte.Parse(rows[0]["element"].ToString());
                t.Job = (RebornJob)byte.Parse(rows[0]["job"].ToString());
            }
            else
            {
                DebugSystem.Write("Character not found for " + charID);
                return false;
            }
            //load stat data
            src = GetDataTable("SELECT * FROM stats where charID = '" + charID + "'");

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                List<long[]> tmp2 = new List<long[]>();
                foreach (var row in rows)
                {
                    switch (long.Parse(row["statID"].ToString()))
                    {
                        case 25: t.CurHP = int.Parse(row["StatusUp"].ToString()); break;
                        case 26: t.CurSP = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 38: t.SkillPoints = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 36: t.TotalExp = long.Parse(row["StatusUp"].ToString()); break;
                        case 28: t.Str = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 29: t.Con = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 30: t.Agi = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 27: t.Int = ushort.Parse(row["StatusUp"].ToString()); break;
                        case 33: t.Wis = ushort.Parse(row["StatusUp"].ToString()); break;
                    }
                }
            }

            //load equips
            src = GetDataTable("SELECT * FROM inventory where charID = '" + charID + "' AND storID =1");

            if (src.Rows.Count > 0)
            {
                rows = new DataRow[src.Rows.Count];
                src.Rows.CopyTo(rows, 0);
                ushort id;

                for (int i = 0; i < rows.Length; i++)
                {
                    id = ushort.Parse(rows[i]["itemID"].ToString());
                    if (id != 0)
                    {
                        t[byte.Parse(rows[i]["pos"].ToString())].CopyFrom(ItemDat.GetItemByID(id));
                        t[byte.Parse(rows[i]["pos"].ToString())].Ammt = 1;
                        t[byte.Parse(rows[i]["pos"].ToString())].Damage = byte.Parse(rows[i]["dmg"].ToString());
                        //                    tmp4.Add((byte)i, new string[]{id.ToString(), rows[i]["socketID"].ToString(), rows[i]["bombID"].ToString(),rows[i]["sewID"].ToString(), 
                        //rows[i]["dmg"].ToString(),rows[i]["forge"].ToString(), });
                    }
                }
            }
            return true;
        }

        public bool WriteNewPlayer(uint charID, Player player)
        {
            if (charID == 0) return false;

            DataTable src = null;
            DataRow[] rows = new DataRow[0];

            #region write character Data

            Character c = (Character)player;
            Dictionary<string, object> insert = new Dictionary<string, object>();
            insert.Add("charID", player.CharID);
            insert.Add("userID", player.UserAcc.UserID);
            insert.Add("slot", player.Slot);
            insert.Add("head", c.Head.ToString());
            insert.Add("body", ((byte)c.Body));
            insert.Add("name", c.CharName);
            insert.Add("name_clean", c.CharName.ToLower());
            insert.Add("nickname", c.NickName);
            insert.Add("location_map", c.LoginMap);
            insert.Add("location_x", c.CurX);
            insert.Add("location_y", c.CurY);
            insert.Add("haircolor", c.HairColor);
            insert.Add("skincolor", c.SkinColor);
            insert.Add("clothingcolor", c.ClothingColor);
            insert.Add("eyecolor", c.EyeColor);
            insert.Add("gold", c.Gold);
            insert.Add("element", ((byte)c.Element));
            insert.Add("rebirth", BitConverter.GetBytes(c.Reborn)[0]);
            insert.Add("job", ((byte)c.Job));//fix
            insert.Add("online", BitConverter.GetBytes(false)[0]);

            try { Insert("characters", insert); }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); return false; }

            #endregion

            #region Write Extr Data

            try { src = GetDataTable(string.Format("SELECT * FROM {0} where {1}", "charactersextdata", "charID = '" + charID + "'")); }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); return false; }

            Dictionary<string, object> cols = new Dictionary<string, object>();
            cols.Add("charID", charID.ToString());
            cols.Add("Settings", player.Settings.ToString());
            cols.Add("Friends", /*player.GetFriends_Flag*/"");
            cols.Add("Guild", "0");
            cols.Add("Mail", /*player.GetMailboxFlags()*/"");

            if (src.Rows.Count == 0)
            {
                try { Insert("charactersextdata", cols); }
                catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
            }
            else
            {
                try { Update("charactersextdata", cols, "charID = '" + charID + "';"); }
                catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
            }
            #endregion

            #region write stats

            foreach (var u in player.Stat_toSave)
            {
                try { src = GetDataTable(string.Format("SELECT * FROM {0} where {1}", "stats", "charID = '" + charID + "' AND statID = '" + u[0] + "'")); }
                catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); }

                cols = new Dictionary<string, object>();
                cols.Add("statIdx", "0");
                cols.Add("charID", charID.ToString());
                cols.Add("statID", u[0].ToString());
                cols.Add("StatusUp", u[1].ToString());
                cols.Add("potential", player.Potential.ToString());



                if (src.Rows.Count == 0)
                {
                    try
                    {
                        Insert("stats", cols);
                    }
                    catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
                }
                else
                {
                    try { Update("stats", cols, "charID = '" + charID + "'AND statID = '" + u[0] + "';"); }
                    catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
                }
            }
            #endregion

            #region predelete inv
            try { ExecuteNonQuery("DELETE FROM inventory where charID ='" + charID + "';"); }
            catch (MySqlException ex) { DebugSystem.Write(new ExceptionData(ex)); throw; }
            #endregion

            #region write inv
            if (player.Inv.InventoryDBData != null)
            {
                string t = "";
                foreach (var u in player.Inv.InventoryDBData)
                {
                    t += string.Format("('{0}','{1}','0','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'),",
                         u.Key, charID, u.Value[0], u.Value[1], u.Value[2], u.Value[3], u.Value[4], u.Value[5], u.Value[6], u.Value[7]);
                }
                t = t.Substring(0, t.Length - 1);
                t += ";";
                ExecuteNonQuery(string.Format("INSERT INTO inventory (invIdx,charID,storID,itemID,dmg,qty,pos,socketID,bombID,sewID,forge) VALUES {0}", t));

            }
            #endregion

            #region write eqs

            if (player.EqData != null)
            {
                string t = "";
                foreach (var u in player.EqData)
                {
                    t += string.Format("('{0}','{1}','1','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'),",
                            u.Key, charID, u.Value[0], u.Value[1], u.Value[2], u.Value[3], u.Value[4], u.Value[5], u.Value[6], u.Value[7]);
                }
                t = t.Substring(0, t.Length - 1);
                t += ";";
                ExecuteNonQuery(string.Format("INSERT INTO inventory (invIdx,charID,storID,itemID,dmg,qty,pos,socketID,bombID,sewID,forge) VALUES {0}", t));
            }

            if (Cache.ContainsKey((int)charID))
                Cache[(int)charID] = player;
            #endregion

            return true;
        }

        public uint CreateWebCharacter(uint databaseID, string username, byte slot, string name, BodyStyle body, byte head, Affinity element)
        {
            if (databaseID == 0 || string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("A valid game account is required.");
            if (slot < 1 || slot > 2)
                throw new ArgumentOutOfRangeException("slot");
            if (string.IsNullOrEmpty(name) || name.Length < 4 || name.Length > 14 ||
                name.Any(character =>
                    !((character >= 'A' && character <= 'Z') ||
                      (character >= 'a' && character <= 'z') ||
                      (character >= '0' && character <= '9') ||
                      character == '_')))
                throw new ArgumentException("Character names must be 4-14 letters, numbers, or underscores.");

            byte maxHead;
            switch (body)
            {
                case BodyStyle.Small_Male: maxHead = 0; break;
                case BodyStyle.Small_Female: maxHead = 1; break;
                case BodyStyle.Big_Male: maxHead = 3; break;
                case BodyStyle.Big_Female: maxHead = 7; break;
                default: throw new ArgumentOutOfRangeException("body");
            }
            if (head > maxHead)
                throw new ArgumentOutOfRangeException("head");
            if (element < Affinity.Normal || element > Affinity.Wind)
                throw new ArgumentOutOfRangeException("element");

            uint userID = checked(databaseID + 10000);
            uint charID = slot == 1 ? userID : checked(userID + 4500000);
            DataTable existing = GetDataTable("SELECT 1 FROM characters WHERE charID = '" + charID + "' LIMIT 1");
            if (existing == null)
                throw new InvalidOperationException("Unable to verify the character slot.");
            if (existing.Rows.Count != 0)
                throw new InvalidOperationException("That character slot is already occupied.");
            if (!LockName(databaseID, name))
                throw new InvalidOperationException("That character name is already in use.");

            try
            {
                Player player = new Player(ItemDat);
                player.UserAcc.DataBaseID = databaseID;
                player.UserAcc.UserName = username;
                player.CharID = charID;
                player.Slot = slot;
                player.CharName = name;
                player.Body = body;
                player.Head = head;
                player.Element = element;
                player.HairColor = 0;
                player.SkinColor = 0;
                player.ClothingColor = 0;
                player.EyeColor = 0;
                player.Str = 1;
                player.Con = 1;
                player.Agi = 1;
                player.Int = 1;
                player.Wis = 1;
                player.FillHP();
                player.FillSP();
                player.Settings.ChannelCode = (ChannelCodeType)31;
                player.Settings.TRADABLE = true;
                player.Settings.PKABLE = false;
                player.Settings.JOINABLE = true;
                player.TotalExp = 6;
                player.LoginMap = 60000;
                player.CurX = 602;
                player.CurY = 455;
                player.SetGold(0);

                if (!WriteNewPlayer(charID, player))
                    throw new InvalidOperationException("The character could not be saved.");
                return charID;
            }
            finally
            {
                unLockName(name);
            }
        }

        public bool WritePlayer(uint charID, Player player)
        {
            bool rem = false;
            if (charID == 0) return rem;

            if (Cache.ContainsKey((int)charID))
                Cache[(int)charID] = player;

            DataTable src = null;

            #region write character Data
            Character c = player;
            Dictionary<string, object> insert = new Dictionary<string, object>();
            insert.Add("head", c.Head.ToString());
            insert.Add("body", ((byte)c.Body).ToString());
            insert.Add("nickname", c.NickName);
            insert.Add("location_map", (c.CurMap.Type == MapType.RegularMap) ? c.CurMap.MapID.ToString() : player.PrevMap.DstMap.ToString());
            insert.Add("location_x", (c.CurMap.Type == MapType.RegularMap) ? c.CurX.ToString() : player.PrevMap.DstX_Axis.ToString());
            insert.Add("location_y", (c.CurMap.Type == MapType.RegularMap) ? c.CurY.ToString() : player.PrevMap.DstY_Axis.ToString());
            insert.Add("haircolor", c.HairColor.ToString());
            insert.Add("skincolor", c.SkinColor.ToString());
            insert.Add("clothingcolor", c.ClothingColor.ToString());
            insert.Add("eyecolor", c.EyeColor.ToString());
            insert.Add("gold", c.Gold.ToString());
            insert.Add("element", ((byte)c.Element).ToString());
            insert.Add("rebirth", BitConverter.GetBytes(c.Reborn)[0].ToString());
            insert.Add("job", ((byte)c.Job).ToString());//fix
            insert.Add("online", BitConverter.GetBytes(false)[0].ToString());

            Update("characters", insert, "charID = '" + charID + "'");

            #endregion

            #region write stats

            foreach (var u in player.Stat_toSave)
            {
                src = GetDataTable(string.Format("SELECT * FROM {0} where {1}", "stats", "charID = '" + charID + "' AND statID = '" + u[0] + "'"));

                if (src.Rows.Count == 0)
                {
                    ExecuteNonQuery(string.Format("INSERT INTO stats {0}", string.Format("(statIdx,charID,statID,StatusUp,potential) VALUES('0','{0}','{1}','{2}','{3}');", charID, u[0], u[1], player.Potential)));
                }
                else
                {
                    ExecuteNonQuery(string.Format("UPDATE stats SET {0} where charID = '" + charID + "' AND statID = '" + u[0] + "'", string.Format(" statID = '{0}',StatusUp ='{1}',potential ='{2}'", u[0], u[1], player.Potential)));
                }
            }
            #endregion

            #region write inv

            string str = "";
            if (player.Inv.InventoryDBData != null)
                foreach (var u in player.Inv.InventoryDBData)
                {
                    ExecuteNonQuery(string.Format("UPDATE inventory SET {0} where {1};", string.Format("itemID = '{0}', dmg = '{1}', qty = '{2}', pos = '{3}', socketID = '{4}', bombID = '{5}', sewID = '{6}', forge = '{7}'",
                          u.Value[0], u.Value[1], u.Value[2], u.Value[3], u.Value[4], u.Value[5], u.Value[6], u.Value[7]), "charID ='" + charID + "' AND storID ='0' AND invIdx = '" + u.Key + "'"));
                }
            #endregion

            #region write eqs

            str = "";
            if (player.EqData != null)
                foreach (var u in player.EqData)
                {
                    ExecuteNonQuery(string.Format("UPDATE inventory SET {0} where {1};", string.Format("itemID = '{0}', dmg = '{1}', qty = '{2}', pos = '{3}', socketID = '{4}', bombID = '{5}', sewID = '{6}', forge = '{7}'",
                          u.Value[0], u.Value[1], u.Value[2], u.Value[3], u.Value[4], u.Value[5], u.Value[6], u.Value[7]), "charID ='" + charID + "' AND storID ='1' AND invIdx = '" + u.Key + "'"));
                }
            if (Cache.ContainsKey((int)charID))
                Cache[(int)charID] = player;

            #endregion

            #region write ext data
            ExecuteNonQuery(string.Format("UPDATE charactersextdata SET {0} where charID = '" + charID + "';", string.Format(" Settings = '{0}', Friends = '{1}', Guild = '{2}', Mail = '{3}'", player.Settings.ToString(), /*player.GetFriends_Flag*/"", "0", /*player.GetMailboxFlags()*/"")));
            #endregion

            return true;
        }

        public void SendOnlineCharacters(Player src)
        {
            PacketBuilder tmp = new PacketBuilder();
            tmp.Begin(null);

            SendPacket p;
            foreach (var y in (from c in Assembly.GetExecutingAssembly().GetTypes()
                               where c.IsClass && c.IsSubclassOf(typeof(GmBot))
                               select c))
            {
                var c = (Activator.CreateInstance(y) as GmBot);
                p = new SendPacket();
                p.Pack8(4);
                p.Pack32(c.CharID);
                p.Pack8((byte)c.Body); //body style
                p.Pack8((byte)c.Element); //element
                p.Pack8(c.Level); //level
                p.Pack16(10019); //map id
                p.Pack16(722); //x
                p.Pack16(995); //y
                p.Pack8(0);
                p.Pack16(c.Head);
                p.Pack16(c.HairColor);
                p.Pack16(c.SkinColor);
                p.Pack16(c.ClothingColor);
                p.Pack16(c.EyeColor);
                p.Pack8(c.WornCount);//clothesAmmt); // ammt of clothes
                p.PackArray(c.Worn_Equips);
                p.Pack32(0); p.Pack8(0); //??
                p.PackBool(c.Reborn); //is rebirth
                p.Pack8((byte)c.Job); //rb class
                p.PackString(c.CharName);//(BYTE*)c.CharacterName,c.nameLen); //CharacterName
                p.PackString(c.NickName);//(BYTE*)c.nick,c.nickLen); //nickname
                p.Pack8(255); //??
                tmp.Add(p);
            }
            foreach (var c in Characters_Online.Values)
            {
                p = new SendPacket();
                p.Pack8(4);
                p.Pack32(c.CharID);
                p.Pack8((byte)c.Body); //body style
                p.Pack8((byte)c.Element); //element
                p.Pack8(c.Level); //level
                p.Pack16((ushort)c.CurMap.MapID); //map id
                p.Pack16(c.CurX); //x
                p.Pack16(c.CurY); //y
                p.Pack8(0); p.Pack8(c.Head); p.Pack8(0);
                p.Pack16(c.HairColor);
                p.Pack16(c.SkinColor);
                p.Pack16(c.ClothingColor);
                p.Pack16(c.EyeColor);
                p.Pack8(c.WornCount);//clothesAmmt); // ammt of clothes
                p.PackArray(c.Worn_Equips);
                p.Pack32(0); p.Pack8(0); //??
                p.PackBool(c.Reborn); //is rebirth
                p.Pack8((byte)c.Job); //rb class
                p.PackString(c.CharName);//(BYTE*)c.CharacterName,c.nameLen); //CharacterName
                p.PackString(c.NickName);//(BYTE*)c.nick,c.nickLen); //nickname
                p.Pack8(255); //??
                tmp.Add(p);
            }
            src.Send(tmp.End());
        }

        public void OnCharacterJoin(Player src)
        {
            Characters_Online.TryAdd((int)src.CharID,src);
        }

        public void OnCharacterLeave(Player src)
        {
            Character s;
            if (Characters_Online.TryRemove((int)src.CharID, out s))
            {
                //Begin Saving Character Data
                WritePlayer(src.CharID,src);



            }
        }
    }
}
