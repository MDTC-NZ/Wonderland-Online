using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
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
            if (GetDataTable("SELECT userID, username, password, character1ID, character2ID, IM, char_delete_code FROM user WHERE 1 = 0") == null)
                throw new InvalidOperationException("The local user table is missing required columns.");
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
            if (!LocalSetupVerified || PassVerification != VerifyPassType.None ||
                TableName != "user" || Username_Ref != "username" || Password_Ref != "password")
                throw new InvalidOperationException("Account registration is available only for the default local account database.");

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

            if (GetDataTable("SELECT 1 FROM user WHERE username = @username", new DbParam("@username", username)).Rows.Count != 0)
                return false;

            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("username", username);
            values.Add("password", HashPassword(password));
            try
            {
                Insert("user", values);
                return true;
            }
            catch (DbException)
            {
                if (GetDataTable("SELECT 1 FROM user WHERE username = @username", new DbParam("@username", username)).Rows.Count != 0)
                    return false;
                throw;
            }
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
            if (username == null || password == null)
                return false;

            username = username.Trim().ToLowerInvariant();

            try
            {
                var userTable = GetDataTable("SELECT " + Password_Ref + " FROM " + TableName + " WHERE " + Username_Ref + " = @username", 
                    new DbParam("@username", username));

                if (userTable.Rows.Count == 0)
                    return false;

                string storedHash = userTable.Rows[0][Password_Ref].ToString();
                string providedHash = HashPassword(password);

                return storedHash == providedHash;
            }
            catch (DbException)
            {
                return false;
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
                            userData = new string[] { rows[0][Username_Ref].ToString(), ch, (rows[0][IM_Ref].ToString() == "") ? "0" : rows[0][IM_Ref].ToString() };
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
                            userData = new string[] { rows[0][Username_Ref].ToString(), ch, (rows[0][IM_Ref].ToString() == "") ? "0" : rows[0][IM_Ref].ToString() };
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
