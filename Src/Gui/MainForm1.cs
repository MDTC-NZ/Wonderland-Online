using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Data.Common;
using Plugin;
using DataBase;
using Game;
using System.Globalization;

namespace Wonderland_Private_Server
{
    public partial class Form1 : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        bool blockclose = true;

        PluginManager phostManager;
        Server.RegistrationServer registrationServer;
        DataGridView userAccountsGrid;
        DataGridView adminCharactersGrid;
        DataGridView onlinePlayersGrid;
          

        public Form1()
        {
          
            InitializeComponent();
            InitializeUserManagementTab();
        }

        void InitializeUserManagementTab()
        {
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            TabPage accountsPage = new TabPage("Accounts");
            TabPage charactersPage = new TabPage("Characters");
            TabPage onlinePage = new TabPage("Online Players");

            userAccountsGrid = CreateManagementGrid();
            adminCharactersGrid = CreateManagementGrid();
            onlinePlayersGrid = CreateManagementGrid();

            FlowLayoutPanel accountActions = CreateManagementActions();
            accountActions.Controls.Add(CreateManagementButton("Create User", CreateUserButton_Click));
            accountActions.Controls.Add(CreateManagementButton("Reset Password", ResetSelectedAccountPassword_Click));
            accountActions.Controls.Add(CreateManagementButton("Toggle GM/Admin", ToggleSelectedAccountRole_Click));
            accountActions.Controls.Add(CreateManagementButton("Delete Account", DeleteSelectedAccount_Click));
            accountActions.Controls.Add(CreateManagementButton("Refresh", RefreshUsersButton_Click));
            accountsPage.Controls.Add(userAccountsGrid);
            accountsPage.Controls.Add(accountActions);

            FlowLayoutPanel characterActions = CreateManagementActions();
            characterActions.Controls.Add(CreateManagementButton("Create Character", CreateAdminCharacter_Click));
            characterActions.Controls.Add(CreateManagementButton("Edit Character", EditAdminCharacter_Click));
            characterActions.Controls.Add(CreateManagementButton("Delete Character", DeleteAdminCharacter_Click));
            characterActions.Controls.Add(CreateManagementButton("Refresh", RefreshCharactersButton_Click));
            charactersPage.Controls.Add(adminCharactersGrid);
            charactersPage.Controls.Add(characterActions);

            FlowLayoutPanel onlineActions = CreateManagementActions();
            onlineActions.Controls.Add(CreateManagementButton("Refresh", RefreshOnlinePlayersButton_Click));
            onlinePage.Controls.Add(onlinePlayersGrid);
            onlinePage.Controls.Add(onlineActions);

            tabs.TabPages.Add(accountsPage);
            tabs.TabPages.Add(charactersPage);
            tabs.TabPages.Add(onlinePage);
            tabPage9.Controls.Add(tabs);
        }

        static DataGridView CreateManagementGrid()
        {
            return new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
        }

        static FlowLayoutPanel CreateManagementActions()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 42,
                Padding = new Padding(4),
                WrapContents = true
            };
        }

        static Button CreateManagementButton(string text, EventHandler handler)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.Click += handler;
            return button;
        }

        void RefreshUsersButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (cGlobal.gUserDataBase == null)
                    throw new InvalidOperationException("The game account database is not ready yet.");

                userAccountsGrid.DataSource = cGlobal.gUserDataBase.GetLocalAccounts();
                if (userAccountsGrid.Columns.Contains("userID"))
                    userAccountsGrid.Columns["userID"].HeaderText = "Account ID";
                if (userAccountsGrid.Columns.Contains("gmLevel"))
                    userAccountsGrid.Columns["gmLevel"].Visible = false;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, "Could not load game accounts: " + ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ResetSelectedAccountPassword_Click(object sender, EventArgs e)
        {
            uint accountId;
            string username;
            int gmLevel;
            if (!TryGetSelectedAccount(out accountId, out username, out gmLevel))
                return;

            string password = ShowPasswordPrompt("Reset password for " + username);
            if (password == null)
                return;

            try
            {
                if (!cGlobal.gUserDataBase.ResetLocalPassword(accountId, password))
                    throw new InvalidOperationException("The selected account no longer exists.");
                MessageBox.Show(this, "Password reset successfully.", "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, "Could not reset the password: " + ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, "Could not reset the password: " + ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ToggleSelectedAccountRole_Click(object sender, EventArgs e)
        {
            uint accountId;
            string username;
            int gmLevel;
            if (!TryGetSelectedAccount(out accountId, out username, out gmLevel))
                return;

            int newLevel = gmLevel > 0 ? 0 : 1;
            try
            {
                if (!cGlobal.gUserDataBase.SetLocalGMLevel(accountId, newLevel))
                    throw new InvalidOperationException("The selected account no longer exists.");
                RefreshUsersButton_Click(sender, e);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, "Could not update the account role: " + ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, "Could not update the account role: " + ex.Message, "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void DeleteSelectedAccount_Click(object sender, EventArgs e)
        {
            uint accountId;
            string username;
            int gmLevel;
            if (!TryGetSelectedAccount(out accountId, out username, out gmLevel))
                return;
            if (MessageBox.Show(this, "Delete account " + username + "? Characters must be deleted first.",
                "Delete account", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (cGlobal.gCharacterDataBase.HasWebCharacters(accountId))
                    throw new InvalidOperationException("Delete this account's characters before deleting the account.");
                if (!cGlobal.gUserDataBase.DeleteLocalAccount(accountId))
                    throw new InvalidOperationException("The selected account no longer exists.");
                RefreshUsersButton_Click(sender, e);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Delete account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, ex.Message, "Delete account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        bool TryGetSelectedAccount(out uint accountId, out string username, out int gmLevel)
        {
            accountId = 0;
            username = null;
            gmLevel = 0;
            if (userAccountsGrid == null || userAccountsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Select an account first.", "User accounts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            DataGridViewRow row = userAccountsGrid.SelectedRows[0];
            accountId = Convert.ToUInt32(row.Cells["userID"].Value, CultureInfo.InvariantCulture);
            username = Convert.ToString(row.Cells["username"].Value, CultureInfo.InvariantCulture);
            gmLevel = Convert.ToInt32(row.Cells["gmLevel"].Value, CultureInfo.InvariantCulture);
            return true;
        }

        string ShowPasswordPrompt(string title)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = title;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(340, 110);

                TextBox password = new TextBox();
                password.Location = new Point(12, 12);
                password.Width = 316;
                password.UseSystemPasswordChar = true;

                Button ok = new Button();
                ok.Text = "Reset";
                ok.DialogResult = DialogResult.OK;
                ok.Location = new Point(164, 55);

                Button cancel = new Button();
                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.Location = new Point(253, 55);

                dialog.Controls.Add(password);
                dialog.Controls.Add(ok);
                dialog.Controls.Add(cancel);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                return dialog.ShowDialog(this) == DialogResult.OK ? password.Text : null;
            }
        }

        void RefreshCharactersButton_Click(object sender, EventArgs e)
        {
            try
            {
                adminCharactersGrid.DataSource = BuildAdminCharacterTable(false);
                HideCharacterIdColumns(adminCharactersGrid);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, "Could not load characters: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, "Could not load characters: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException ex)
            {
                MessageBox.Show(this, "Could not load characters: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void RefreshOnlinePlayersButton_Click(object sender, EventArgs e)
        {
            try
            {
                onlinePlayersGrid.DataSource = BuildAdminCharacterTable(true);
                HideCharacterIdColumns(onlinePlayersGrid);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, "Could not load online players: " + ex.Message, "Online players", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, "Could not load online players: " + ex.Message, "Online players", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException ex)
            {
                MessageBox.Show(this, "Could not load online players: " + ex.Message, "Online players", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        DataTable BuildAdminCharacterTable(bool onlineOnly)
        {
            if (cGlobal.gUserDataBase == null || cGlobal.gCharacterDataBase == null)
                throw new InvalidOperationException("The game databases are not ready yet.");

            DataTable rows = new DataTable();
            rows.Columns.Add("CharacterId", typeof(uint));
            rows.Columns.Add("AccountId", typeof(uint));
            rows.Columns.Add("Character", typeof(string));
            rows.Columns.Add("Account", typeof(string));
            rows.Columns.Add("Slot", typeof(byte));
            rows.Columns.Add("Level", typeof(byte));
            rows.Columns.Add("Status", typeof(string));
            rows.Columns.Add("Experience", typeof(long));
            rows.Columns.Add("HP", typeof(int));
            rows.Columns.Add("SP", typeof(int));
            rows.Columns.Add("Strength", typeof(int));
            rows.Columns.Add("Constitution", typeof(int));
            rows.Columns.Add("Intelligence", typeof(int));
            rows.Columns.Add("Wisdom", typeof(int));
            rows.Columns.Add("Agility", typeof(int));
            rows.Columns.Add("Gold", typeof(uint));
            rows.Columns.Add("Map", typeof(ushort));
            rows.Columns.Add("X", typeof(ushort));
            rows.Columns.Add("Y", typeof(ushort));

            DataTable accounts = cGlobal.gUserDataBase.GetLocalAccounts();
            foreach (DataRow account in accounts.Rows)
            {
                uint accountId = Convert.ToUInt32(account["userID"], CultureInfo.InvariantCulture);
                string username = account["username"].ToString();
                foreach (WebCharacterInfo character in cGlobal.gCharacterDataBase.GetWebCharacters(accountId))
                {
                    if (onlineOnly && !character.IsOnline)
                        continue;
                    rows.Rows.Add(character.CharacterId, accountId, character.Name, username, character.Slot,
                        character.Level, character.IsOnline ? "Online" : "Offline", character.Experience,
                        character.CurrentHp, character.CurrentSp, character.Strength, character.Constitution,
                        character.Intelligence, character.Wisdom, character.Agility, character.Gold,
                        character.LocationMap, character.LocationX, character.LocationY);
                }
            }
            return rows;
        }

        static void HideCharacterIdColumns(DataGridView grid)
        {
            if (grid.Columns.Contains("CharacterId"))
                grid.Columns["CharacterId"].Visible = false;
            if (grid.Columns.Contains("AccountId"))
                grid.Columns["AccountId"].Visible = false;
        }

        void CreateAdminCharacter_Click(object sender, EventArgs e)
        {
            if (cGlobal.gUserDataBase == null || cGlobal.gCharacterDataBase == null)
            {
                MessageBox.Show(this, "The game databases are not ready yet.", "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable accounts;
            try { accounts = cGlobal.gUserDataBase.GetLocalAccounts(); }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, "Could not load accounts: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, "Could not load accounts: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (accounts.Rows.Count == 0)
            {
                MessageBox.Show(this, "Create an account first.", "Character management", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (Form dialog = new Form())
            {
                dialog.Text = "Create game character";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(420, 290);

                TableLayoutPanel fields = CreateDialogFields();
                ComboBox account = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = accounts, DisplayMember = "username", ValueMember = "userID", Dock = DockStyle.Fill };
                NumericUpDown slot = CreateNumeric(1, 2, 1);
                TextBox name = new TextBox { MaxLength = 14, Dock = DockStyle.Fill };
                ComboBox body = CreateEnumCombo(new string[] { "Small Male", "Small Female", "Big Male", "Big Female" }, new int[] { 1, 2, 3, 4 });
                NumericUpDown head = CreateNumeric(0, 7, 0);
                ComboBox element = CreateEnumCombo(new string[] { "Normal", "Earth", "Water", "Fire", "Wind" }, new int[] { 0, 1, 2, 3, 4 });
                AddDialogField(fields, "Account", account);
                AddDialogField(fields, "Slot", slot);
                AddDialogField(fields, "Character name", name);
                AddDialogField(fields, "Appearance", body);
                AddDialogField(fields, "Head style", head);
                AddDialogField(fields, "Element", element);

                Button create = new Button { Text = "Create", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Right };
                Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Right };
                FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 38 };
                buttons.Controls.Add(cancel);
                buttons.Controls.Add(create);
                dialog.Controls.Add(fields);
                dialog.Controls.Add(buttons);
                dialog.AcceptButton = create;
                dialog.CancelButton = cancel;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    DataRowView selectedAccount = account.SelectedItem as DataRowView;
                    if (selectedAccount == null)
                        throw new InvalidOperationException("Choose an account.");
                    cGlobal.gCharacterDataBase.CreateWebCharacter(
                        Convert.ToUInt32(account.SelectedValue, CultureInfo.InvariantCulture),
                        selectedAccount["username"].ToString(),
                        (byte)slot.Value, name.Text, (BodyStyle)Convert.ToByte(body.SelectedValue, CultureInfo.InvariantCulture),
                        (byte)head.Value, (Affinity)Convert.ToByte(element.SelectedValue, CultureInfo.InvariantCulture));
                    RefreshCharactersButton_Click(sender, e);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(this, "Could not create the character: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, "Could not create the character: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (DbException ex)
                {
                    MessageBox.Show(this, "Could not create the character: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        void EditAdminCharacter_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = GetSelectedCharacterRow();
            if (row == null)
                return;
            if (string.Equals(Convert.ToString(row.Cells["Status"].Value, CultureInfo.InvariantCulture), "Online", StringComparison.Ordinal))
            {
                MessageBox.Show(this, "Log out of this character before editing it.", "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form dialog = new Form())
            {
                dialog.Text = "Edit game character";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(460, 450);

                TableLayoutPanel fields = CreateDialogFields();
                NumericUpDown experience = CreateNumeric(0, uint.MaxValue, Convert.ToDecimal(row.Cells["Experience"].Value, CultureInfo.InvariantCulture));
                NumericUpDown gold = CreateNumeric(0, uint.MaxValue, Convert.ToDecimal(row.Cells["Gold"].Value, CultureInfo.InvariantCulture));
                NumericUpDown hp = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["HP"].Value, CultureInfo.InvariantCulture));
                NumericUpDown sp = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["SP"].Value, CultureInfo.InvariantCulture));
                NumericUpDown strength = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Strength"].Value, CultureInfo.InvariantCulture));
                NumericUpDown constitution = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Constitution"].Value, CultureInfo.InvariantCulture));
                NumericUpDown intelligence = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Intelligence"].Value, CultureInfo.InvariantCulture));
                NumericUpDown wisdom = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Wisdom"].Value, CultureInfo.InvariantCulture));
                NumericUpDown agility = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Agility"].Value, CultureInfo.InvariantCulture));
                NumericUpDown map = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Map"].Value, CultureInfo.InvariantCulture));
                NumericUpDown x = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["X"].Value, CultureInfo.InvariantCulture));
                NumericUpDown y = CreateNumeric(0, ushort.MaxValue, Convert.ToDecimal(row.Cells["Y"].Value, CultureInfo.InvariantCulture));
                AddDialogField(fields, "Experience (controls level)", experience);
                AddDialogField(fields, "Gold", gold);
                AddDialogField(fields, "HP", hp);
                AddDialogField(fields, "SP", sp);
                AddDialogField(fields, "Strength", strength);
                AddDialogField(fields, "Constitution", constitution);
                AddDialogField(fields, "Intelligence", intelligence);
                AddDialogField(fields, "Wisdom", wisdom);
                AddDialogField(fields, "Agility", agility);
                AddDialogField(fields, "Map ID", map);
                AddDialogField(fields, "X", x);
                AddDialogField(fields, "Y", y);

                Button save = new Button { Text = "Save", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Right };
                Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Right };
                FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 38 };
                buttons.Controls.Add(cancel);
                buttons.Controls.Add(save);
                dialog.Controls.Add(fields);
                dialog.Controls.Add(buttons);
                dialog.AcceptButton = save;
                dialog.CancelButton = cancel;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    cGlobal.gCharacterDataBase.UpdateWebCharacter(
                        Convert.ToUInt32(row.Cells["CharacterId"].Value, CultureInfo.InvariantCulture),
                        (long)experience.Value, (int)hp.Value, (int)sp.Value,
                        (int)strength.Value, (int)constitution.Value, (int)intelligence.Value,
                        (int)wisdom.Value, (int)agility.Value, (uint)gold.Value,
                        (ushort)map.Value, (ushort)x.Value, (ushort)y.Value);
                    RefreshCharactersButton_Click(sender, e);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(this, "Could not update the character: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(this, "Could not update the character: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (DbException ex)
                {
                    MessageBox.Show(this, "Could not update the character: " + ex.Message, "Character management", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        void DeleteAdminCharacter_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = GetSelectedCharacterRow();
            if (row == null)
                return;
            string name = Convert.ToString(row.Cells["Character"].Value, CultureInfo.InvariantCulture);
            if (MessageBox.Show(this, "Delete character " + name + "? This cannot be undone.",
                "Delete character", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (!cGlobal.gCharacterDataBase.DeleteWebCharacter(
                    Convert.ToUInt32(row.Cells["AccountId"].Value, CultureInfo.InvariantCulture),
                    Convert.ToUInt32(row.Cells["CharacterId"].Value, CultureInfo.InvariantCulture)))
                    throw new InvalidOperationException("The selected character no longer exists.");
                RefreshCharactersButton_Click(sender, e);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Delete character", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (DbException ex)
            {
                MessageBox.Show(this, ex.Message, "Delete character", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        DataGridViewRow GetSelectedCharacterRow()
        {
            if (adminCharactersGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, "Select a character first.", "Character management", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return adminCharactersGrid.SelectedRows[0];
        }

        static TableLayoutPanel CreateDialogFields()
        {
            return new TableLayoutPanel
            {
                AutoScroll = true,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                RowCount = 0
            };
        }

        static void AddDialogField(TableLayoutPanel fields, string labelText, Control control)
        {
            int row = fields.RowCount++;
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields.ColumnStyles.Clear();
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Label label = new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left };
            control.Dock = DockStyle.Fill;
            fields.Controls.Add(label, 0, row);
            fields.Controls.Add(control, 1, row);
        }

        static NumericUpDown CreateNumeric(decimal minimum, decimal maximum, decimal value)
        {
            return new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Min(maximum, Math.Max(minimum, value)),
                Dock = DockStyle.Fill
            };
        }

        static ComboBox CreateEnumCombo(string[] labels, int[] values)
        {
            DataTable options = new DataTable();
            options.Columns.Add("Name", typeof(string));
            options.Columns.Add("Value", typeof(int));
            for (int i = 0; i < labels.Length; i++)
                options.Rows.Add(labels[i], values[i]);
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = options,
                DisplayMember = "Name",
                ValueMember = "Value",
                Dock = DockStyle.Fill
            };
        }

        void CreateUserButton_Click(object sender, EventArgs e)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "Create game account";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(360, 220);

                Label usernameLabel = new Label();
                usernameLabel.Text = "Username";
                usernameLabel.AutoSize = true;
                usernameLabel.Location = new Point(12, 16);

                TextBox usernameInput = new TextBox();
                usernameInput.Location = new Point(130, 12);
                usernameInput.Width = 210;

                Label passwordLabel = new Label();
                passwordLabel.Text = "Password";
                passwordLabel.AutoSize = true;
                passwordLabel.Location = new Point(12, 48);

                TextBox passwordInput = new TextBox();
                passwordInput.Location = new Point(130, 44);
                passwordInput.Width = 210;
                passwordInput.UseSystemPasswordChar = true;

                Label confirmLabel = new Label();
                confirmLabel.Text = "Confirm password";
                confirmLabel.AutoSize = true;
                confirmLabel.Location = new Point(12, 80);

                TextBox confirmInput = new TextBox();
                confirmInput.Location = new Point(130, 76);
                confirmInput.Width = 210;
                confirmInput.UseSystemPasswordChar = true;

                CheckBox gmAdminInput = new CheckBox();
                gmAdminInput.Text = "Create as GM/Admin";
                gmAdminInput.AutoSize = true;
                gmAdminInput.Location = new Point(130, 108);

                Button createButton = new Button();
                createButton.Text = "Create";
                createButton.Location = new Point(184, 164);
                createButton.DialogResult = DialogResult.None;

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Location = new Point(265, 164);
                cancelButton.DialogResult = DialogResult.Cancel;

                createButton.Click += delegate
                {
                    if (passwordInput.Text != confirmInput.Text)
                    {
                        MessageBox.Show(dialog, "The passwords do not match.", "Create account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        if (cGlobal.gUserDataBase == null)
                            throw new InvalidOperationException("The game account database is not ready yet.");

                        bool created = cGlobal.gUserDataBase.CreateAccount(
                            usernameInput.Text,
                            passwordInput.Text,
                            gmAdminInput.Checked ? 1 : 0);
                        if (!created)
                        {
                            MessageBox.Show(dialog, "That username is already in use.", "Create account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        dialog.DialogResult = DialogResult.OK;
                        dialog.Close();
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show(dialog, ex.Message, "Create account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(dialog, ex.Message, "Create account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (DbException ex)
                    {
                        MessageBox.Show(dialog, "Could not create the account: " + ex.Message, "Create account", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                dialog.Controls.Add(usernameLabel);
                dialog.Controls.Add(usernameInput);
                dialog.Controls.Add(passwordLabel);
                dialog.Controls.Add(passwordInput);
                dialog.Controls.Add(confirmLabel);
                dialog.Controls.Add(confirmInput);
                dialog.Controls.Add(gmAdminInput);
                dialog.Controls.Add(createButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = createButton;
                dialog.CancelButton = cancelButton;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    RefreshUsersButton_Click(sender, e);
            }
        }


        void DebugSystem_onNewLog(object sender, DebugItem j)
        {
            new Task(() =>
            {
                this.Invoke(new Action(() =>
                        {
                switch (j.Type)
                {
                    case DebugItemType.Info_Light: SystemLog.AppendText(j.Msg+ "\r\n=============================\r\n"); break;
                    case DebugItemType.Network_Light: NetWorkLog.AppendText(j.Msg + "\r\n=============================\r\n"); break;
                    case DebugItemType.Error: errorLog.AppendText(j.Msg + "\r\n=============================\r\n"); break;
                    //case Utilities.LogType.DB: errorLog.AppendText(j.Msg + "\r\n=============================\r\n"); break;
                    //case Utilities.LogType.THRD: SystemLog.AppendText(j.Msg + "\r\n=============================\r\n"); break;
                    //case Utilities.LogType.UPDT: SystemLog.AppendText(j.Msg + "\r\n=============================\r\n"); break;
                }
                        }));
            }
            ).Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread MainThread = new Thread(new ThreadStart(MainThreadWork));
            MainThread.IsBackground = true;
            MainThread.Init();
        }
        
        void GuiThread()
        {
            do
            {
                #region LogGUI
                string s = null;
                //if (!string.IsNullOrEmpty((s = DebugSystem.PullLogItem())))
                //    SystemLog.AppendText(s);
                #endregion
                #region TaskGui

                if (cGlobal.ApplicationTasks != null)
                    this.BeginInvoke(new Action(() => { cGlobal.ApplicationTasks.onUpdateGuiTick(); }));
                    
                #endregion
                #region Form.System.Status gui
                try
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        //thrd_label.Text = string.Format("Thread Cnt - {0}", ThreadManager.Count);
                    }));
                }
                catch { }
                #endregion
                #region Form.Update
                //try
                //{
                //    this.BeginInvoke(new Action(() =>
                //    {
                //        DateTime tmp = (cGlobal.ApplicationTasks.TaskItems.Count(c => c.TaskName == "Updating Application") > 0) ? cGlobal.ApplicationTasks.TaskItems.Single(c => c.TaskName == "Updating Application").Createdat : new DateTime();

                //        autoUpdt_label.Text = string.Format(tmp.Add(cGlobal.SrvSettings.Update.AutoUpdt_Schedule).ToString());
                //    }));
                //}
                //catch { }
                #endregion
                Thread.Sleep(5);
            }
            while (cGlobal.Run);
        }


        void RefreshDatabaseMappingDisplay()
        {
            if (cGlobal.SrvSettings == null || cGlobal.SrvSettings.DB == null)
                return;

            DataTable userMapping = new DataTable();
            userMapping.Columns.Add("Field", typeof(string));
            userMapping.Columns.Add("Value", typeof(string));
            userMapping.Rows.Add("Table", cGlobal.SrvSettings.DB.TableName_Ref ?? "user");
            userMapping.Rows.Add("User", cGlobal.SrvSettings.DB.Username_Ref ?? "username");
            userMapping.Rows.Add("Pass", cGlobal.SrvSettings.DB.Password_Ref ?? "password");
            userMapping.Rows.Add("DataBaseID", cGlobal.SrvSettings.DB.UserID_Ref ?? "userID");
            userMapping.Rows.Add("IM", cGlobal.SrvSettings.DB.IM_Ref ?? "IM");
            userMapping.Rows.Add("Char_Delete_Code", cGlobal.SrvSettings.DB.Char_Delete_Code_Ref ?? "char_delete_code");

            DataTable characterMapping = new DataTable();
            characterMapping.Columns.Add("Field", typeof(string));
            characterMapping.Columns.Add("Value", typeof(string));
            characterMapping.Rows.Add("CharacterID1", cGlobal.SrvSettings.DB.CharacterID1_Ref ?? "character1ID");
            characterMapping.Rows.Add("CharacterID2", cGlobal.SrvSettings.DB.CharacterID2_Ref ?? "character2ID");
            characterMapping.Rows.Add("Table", cGlobal.SrvSettings.DB.TableName_Ref ?? "user");
            characterMapping.Rows.Add("User", cGlobal.SrvSettings.DB.Username_Ref ?? "username");
            characterMapping.Rows.Add("Pass", cGlobal.SrvSettings.DB.Password_Ref ?? "password");
            characterMapping.Rows.Add("DataBaseID", cGlobal.SrvSettings.DB.UserID_Ref ?? "userID");

            DataTable configMapping = new DataTable();
            configMapping.Columns.Add("Field", typeof(string));
            configMapping.Columns.Add("Value", typeof(string));
            configMapping.Rows.Add("Server Type", cGlobal.SrvSettings.DB.Server_Type.ToString());
            configMapping.Rows.Add("Database", cGlobal.SrvSettings.DB.DataBase ?? "LocalServer.sqlite");
            configMapping.Rows.Add("IP", cGlobal.SrvSettings.DB.ServerIP ?? "127.0.0.1");
            configMapping.Rows.Add("Port", cGlobal.SrvSettings.DB.Port.ToString(CultureInfo.InvariantCulture));
            configMapping.Rows.Add("Password Verification", cGlobal.SrvSettings.DB.PassVerification.ToString(CultureInfo.InvariantCulture));

            if (dataGridView3 != null) dataGridView3.DataSource = userMapping;
            if (dataGridView4 != null) dataGridView4.DataSource = characterMapping;
            if (dataGridView5 != null) dataGridView5.DataSource = configMapping;
            if (groupBox7 != null) groupBox7.Text = "User Table Field Linking";
            if (groupBox8 != null) groupBox8.Text = "Character Table Field Linking";
            if (groupBox9 != null) groupBox9.Text = "Database Settings";
            if (tabPage12 != null) tabPage12.Text = "Database";
            if (tabPage17 != null) tabPage17.Text = "Node Network";
        }

        void MainThreadWork()
        {

            cGlobal.Run = true;
            DebugSystem.Initialize(ref MainOutput, true);
            DebugSystem.VerboseLvl = 1;

            DebugSystem.Write("[Init] - Initializing DataFile Objects");
            log.Info("[Init] - Initializing DataFile Objects");
            cGlobal.ItemDatManager = new DataFiles.PhxItemDat();
            cGlobal.ItemDatManager.Load(Environment.CurrentDirectory + "\\Data\\itemDat.wpdat");
            string sceneDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SceneData.Dat");
            if (!File.Exists(sceneDataPath))
            {
                sceneDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Wonderland Online", "data", "SceneData.Dat");
            }
            if (File.Exists(sceneDataPath))
            {
                if (!new Wonderland_Private_Server.DataManagement.DataFiles.SceneDataFile().LoadScenes(sceneDataPath))
                    DebugSystem.Write("[MapSystem][Warning] Scene names could not be loaded from " + sceneDataPath);
            }
            else
                DebugSystem.Write("[MapSystem][Warning] SceneData.Dat was not found; maps will use numeric names.");
            string eveDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gamedata", "eve.Emg");
            cGlobal.gEveManager = new Wonderland_Private_Server.DataManagement.DataFiles.EveManager();
            if (!cGlobal.gEveManager.LoadFile(eveDataPath))
            {
                DebugSystem.Write("[MapSystem][Warning] EVE map data could not be loaded from " + eveDataPath);
                cGlobal.gEveManager = null;
            }
            else
                DebugSystem.Write("[MapSystem][Info] Loaded " + cGlobal.gEveManager.MapCount + " EVE map records.");
            DebugSystem.Write("[Init] - Initializing DataBase Objects");
            cGlobal.gUserDataBase = new DataBase.UserDataBase();
            cGlobal.gCharacterDataBase = new DataBase.CharacterDataBase();
            cGlobal.gCharacterDataBase.ItemDat = cGlobal.ItemDatManager;
            cGlobal.gGameDataBase = new DataBase.GameDataBase();
            cGlobal.gGameDataBase.ItemDat = cGlobal.ItemDatManager;
            DebugSystem.Write("[Init] - Intializing Systems Please Wait.....");
            cGlobal.ApplicationTasks = new Server.TaskManager();
            cGlobal.Update_System = new Server.System.UpdateSystem();
            cGlobal.Update_System.MainFrm = this;
            cGlobal.Update_System.MapUpdtPanel = UpdtPane2;
            cGlobal.Update_System.AppUpdtPanel = UpdatePane;
            phostManager = new PluginManager();
            phostManager.Intialize();
           
            cGlobal.gLoginServer = new Server.LoginServer();
            cGlobal.gWorld = new Server.WorldServer(phostManager);

            //cGlobal.WLO_World = new Server.WloWorldNode();
            //cGlobal.gCharacterDataBase = new DataManagement.DataBase.CharacterDataBase();
            //cGlobal.gEveManager = new DataManagement.DataFiles.EveManager();
            //cGlobal.gGameDataBase = new DataManagement.DataBase.GameDataBase();
            //cGlobal.gItemManager = new DataManagement.DataFiles.ItemManager();
            //cGlobal.gSkillManager = new DataManagement.DataFiles.SkillDataFile();
            //cGlobal.gCompoundDat = new DataManagement.DataFiles.cCompound2Dat();
            //cGlobal.gUserDataBase = new UserDataBase();
            //cGlobal.gNpcManager = new DataManagement.DataFiles.NpcDat();
            
            cGlobal.SrvSettings = new Server.Config.Settings();

            #region load settings file
            
            DebugSystem.Write("Loading Settings File");
            if (System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PServer\\Config.settings.wlo"))
            {
                System.Xml.Serialization.XmlSerializer diskio = new System.Xml.Serialization.XmlSerializer(typeof(Server.Config.Settings));

                try
                {
                    using (StreamReader file = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PServer\\Config.settings.wlo"))
                        cGlobal.SrvSettings = (Server.Config.Settings)diskio.Deserialize(file);
                    DebugSystem.Write("Settings File loaded successfully");
                }
                catch { DebugSystem.Write("Settings File failed to load"); }
            }
            else
                DebugSystem.Write("Settings File not found");

            // Always use local SQLite database to ensure registration server works
            bool useLocalDatabase = true;
            cGlobal.SrvSettings.DB.Server_Type = RCLibrary.Core.DataBaseTypes.Sqlite;
            cGlobal.SrvSettings.DB.DataBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PServer", "LocalServer.sqlite");
            cGlobal.SrvSettings.DB.TableName_Ref = "user";
            cGlobal.SrvSettings.DB.Username_Ref = "username";
            cGlobal.SrvSettings.DB.Password_Ref = "password";
            cGlobal.SrvSettings.DB.UserID_Ref = "userID";
            cGlobal.SrvSettings.DB.CharacterID1_Ref = "character1ID";
            cGlobal.SrvSettings.DB.CharacterID2_Ref = "character2ID";
            cGlobal.SrvSettings.DB.IM_Ref = "IM";
            cGlobal.SrvSettings.DB.Char_Delete_Code_Ref = "char_delete_code";


            cGlobal.gUserDataBase.TableName = cGlobal.SrvSettings.DB.TableName_Ref;
            cGlobal.gUserDataBase.Username_Ref = cGlobal.SrvSettings.DB.Username_Ref;
            cGlobal.gUserDataBase.Password_Ref = cGlobal.SrvSettings.DB.Password_Ref;
            cGlobal.gUserDataBase.DataBaseID_Ref = cGlobal.SrvSettings.DB.UserID_Ref;
            cGlobal.gUserDataBase.IM_Ref = cGlobal.SrvSettings.DB.IM_Ref;
            cGlobal.gUserDataBase.CharacterID1_Ref = cGlobal.SrvSettings.DB.CharacterID1_Ref;
            cGlobal.gUserDataBase.CharacterID2_Ref = cGlobal.SrvSettings.DB.CharacterID2_Ref;
            cGlobal.gUserDataBase.Char_Delete_Code_Ref = cGlobal.SrvSettings.DB.Char_Delete_Code_Ref;
            cGlobal.gUserDataBase.PassVerification = (Game.VerifyPassType)cGlobal.SrvSettings.DB.PassVerification;
            //if (GitUptOption.SelectedIndex != (byte)cGlobal.SrvSettings.Update.UpdtControl)
            //    GitUptOption.SelectedIndex = (byte)cGlobal.SrvSettings.Update.UpdtControl;



            #endregion

           

            //use for testing
#if DEBUG

#endif
            

            #region Intialize Base Threads
            DebugSystem.Write("Intializing Gui Thread");
            Thread tmp2 = new Thread(new ThreadStart(GuiThread));
            tmp2.Name = "Gui Thread";
            tmp2.Init();

            #endregion

            #region intial check  if theres an update from github
            //if (cGlobal.SrvSettings.Update.UpdtControl != Server.Config.UpdtSetting.Never && cGlobal.ApplicationTasks.TaskItems.Count(c => c.TaskName == "Updating Application") > 0)
            //    goto ShutDwn;
            #endregion
                        

            #region Configure Form Data
             this.Invoke(new Action(() =>
            {
                if (dataGridView1 != null)
                {
                    dataGridView1.Columns[0].DataPropertyName = "TaskName";
                    dataGridView1.Columns[1].DataPropertyName = "Interval";
                    dataGridView1.Columns[2].DataPropertyName = "LastExecution";
                    dataGridView1.Columns[3].DataPropertyName = "NextExecution";
                    dataGridView1.Columns[4].DataPropertyName = "Status";
                    dataGridView1.DataSource = cGlobal.ApplicationTasks.TaskItems;
                }

                if (cGlobal.SrvSettings.DB == null)
                    cGlobal.SrvSettings.DB = new Server.Config.DataBaseConfig();

                cGlobal.SrvSettings.DB.Server_Type = RCLibrary.Core.DataBaseTypes.Sqlite;
                cGlobal.SrvSettings.DB.DataBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PServer", "LocalServer.sqlite");
                cGlobal.SrvSettings.DB.TableName_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.TableName_Ref) ? "user" : cGlobal.SrvSettings.DB.TableName_Ref;
                cGlobal.SrvSettings.DB.Username_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Username_Ref) ? "username" : cGlobal.SrvSettings.DB.Username_Ref;
                cGlobal.SrvSettings.DB.Password_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Password_Ref) ? "password" : cGlobal.SrvSettings.DB.Password_Ref;
                cGlobal.SrvSettings.DB.UserID_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.UserID_Ref) ? "userID" : cGlobal.SrvSettings.DB.UserID_Ref;
                cGlobal.SrvSettings.DB.CharacterID1_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.CharacterID1_Ref) ? "character1ID" : cGlobal.SrvSettings.DB.CharacterID1_Ref;
                cGlobal.SrvSettings.DB.CharacterID2_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.CharacterID2_Ref) ? "character2ID" : cGlobal.SrvSettings.DB.CharacterID2_Ref;
                cGlobal.SrvSettings.DB.IM_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.IM_Ref) ? "IM" : cGlobal.SrvSettings.DB.IM_Ref;
                cGlobal.SrvSettings.DB.Char_Delete_Code_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Char_Delete_Code_Ref) ? "char_delete_code" : cGlobal.SrvSettings.DB.Char_Delete_Code_Ref;
                cGlobal.SrvSettings.DB.ServerIP = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.ServerIP) ? "127.0.0.1" : cGlobal.SrvSettings.DB.ServerIP;
                if (TableName != null) TableName.DataBindings.Add("Text", cGlobal.SrvSettings.DB, "TableName_Ref");
                if (Username_Ref != null) Username_Ref.DataBindings.Add("Text", cGlobal.SrvSettings.DB, "Username_Ref");
                if (Password_Ref != null) Password_Ref.DataBindings.Add("Text", cGlobal.SrvSettings.DB, "Password_Ref");
                if (UserID_Ref != null) UserID_Ref.DataBindings.Add("Text", cGlobal.SrvSettings.DB, "UserID_Ref");
                if (IM_Ref != null) IM_Ref.DataBindings.Add("Text", cGlobal.SrvSettings.DB, "IM_Ref");
                if (Char_Delete_Code_Ref != null) Char_Delete_Code_Ref.DataBindings.Add("Text", cGlobal.SrvSettings.DB, "Char_Delete_Code_Ref");
                if (_passVerifi != null) _passVerifi.DataBindings.Add("SelectedIndex", cGlobal.SrvSettings.DB, "PassVerification");
                RefreshDatabaseMappingDisplay();
            }));

            // Wire up update schedule configuration
            if (updtschedule != null) updtschedule.DataBindings.Add("Checked", cGlobal.SrvSettings.Update, "EnableSchedUpdate");
            if (updtday != null) updtday.DataBindings.Add("SelectedIndex", cGlobal.SrvSettings.Update, "UpdateDay");
            if (updttime != null) updttime.DataBindings.Add("Value", cGlobal.SrvSettings.Update, "UpdateHour");
            if (updttime2 != null) updttime2.DataBindings.Add("Value", cGlobal.SrvSettings.Update, "UpdateMinute");
            if (updt_warn != null) updt_warn.DataBindings.Add("Checked", cGlobal.SrvSettings.Update, "WarnofUpdate");
            if (updt_warn_how != null) updt_warn_how.DataBindings.Add("SelectedIndex", cGlobal.SrvSettings.Update, "WarningType");
            
            // Use local SQLite database for registration server
            // This allows the registration server to start and serve the web interface
            // even if remote PostgreSQL connection is not available
            useLocalDatabase = true;
            cGlobal.SrvSettings.DB.Server_Type = RCLibrary.Core.DataBaseTypes.Sqlite;
            cGlobal.SrvSettings.DB.DataBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PServer", "LocalServer.sqlite");
            cGlobal.SrvSettings.DB.TableName_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.TableName_Ref) ? "user" : cGlobal.SrvSettings.DB.TableName_Ref;
            cGlobal.SrvSettings.DB.Username_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Username_Ref) ? "username" : cGlobal.SrvSettings.DB.Username_Ref;
            cGlobal.SrvSettings.DB.Password_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Password_Ref) ? "password" : cGlobal.SrvSettings.DB.Password_Ref;
            cGlobal.SrvSettings.DB.UserID_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.UserID_Ref) ? "userID" : cGlobal.SrvSettings.DB.UserID_Ref;
            cGlobal.SrvSettings.DB.CharacterID1_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.CharacterID1_Ref) ? "character1ID" : cGlobal.SrvSettings.DB.CharacterID1_Ref;
            cGlobal.SrvSettings.DB.CharacterID2_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.CharacterID2_Ref) ? "character2ID" : cGlobal.SrvSettings.DB.CharacterID2_Ref;
            cGlobal.SrvSettings.DB.IM_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.IM_Ref) ? "IM" : cGlobal.SrvSettings.DB.IM_Ref;
            cGlobal.SrvSettings.DB.Char_Delete_Code_Ref = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.Char_Delete_Code_Ref) ? "char_delete_code" : cGlobal.SrvSettings.DB.Char_Delete_Code_Ref;
            cGlobal.SrvSettings.DB.ServerIP = string.IsNullOrEmpty(cGlobal.SrvSettings.DB.ServerIP) ? "127.0.0.1" : cGlobal.SrvSettings.DB.ServerIP;
            
            // Optional: PostgreSQL connection can still be configured later
            // cGlobal.SrvSettings.DB.ServerIP = "wlo.kiaoragaming.xyz";
            // cGlobal.SrvSettings.DB.Port = 5423;
            #endregion

            #region DataBase Initialization

            
            DebugSystem.Write("Testing Connection to UserDatabase");
            try
            {
                if (cGlobal.SrvSettings.DB.Server_Type == RCLibrary.Core.DataBaseTypes.Sqlite)
                {
                    if (string.IsNullOrWhiteSpace(cGlobal.SrvSettings.DB.DataBase))
                        throw new InvalidOperationException("The SQLite database path is not configured.");

                    string databasePath = Path.GetFullPath(cGlobal.SrvSettings.DB.DataBase);
                    Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
                    cGlobal.gUserDataBase.ConfigureSqlite(databasePath);
                    cGlobal.gCharacterDataBase.ConfigureSqlite(databasePath);
                }
                if (cGlobal.gUserDataBase.TestConnection())
                {
                    DebugSystem.Write("Connection Successful");
                    if (useLocalDatabase)
                        cGlobal.gUserDataBase.VerifyLocalSetup();
                }
                else
                    DebugSystem.Write("Connection not successful\r\n unable to authencate  users connecting to server");

                DebugSystem.Write("Testing Connection to Character Database");
                if (cGlobal.gCharacterDataBase.TestConnection())
                {
                    DebugSystem.Write("Connection Successful");
                    DebugSystem.Write("Verifying DataBase Tables");
                    cGlobal.gCharacterDataBase.VerifySetup();
                }
                else
                    DebugSystem.Write("Connection not successful\r\n unable to create neccesary tables for the server");
            }
            catch (Exception e) { DebugSystem.Write(new ExceptionData(e)); }


            #endregion

            if (useLocalDatabase &&
                cGlobal.gUserDataBase.LocalSetupVerified &&
                cGlobal.gUserDataBase.PassVerification == Game.VerifyPassType.None)
            {
                try
                {
                    string webDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web");
                    string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certificates", "wlo.kiaoragaming.xyz.pfx");
                    string clientUpdatesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClientUpdates");
                    registrationServer = new Server.RegistrationServer(cGlobal.gUserDataBase, cGlobal.gCharacterDataBase, webDirectory, certPath, null, clientUpdatesDirectory);
                    registrationServer.Start();
                    DebugSystem.Write("Account registration and dashboard available at https://wlo.kiaoragaming.xyz:8080/");
                    DebugSystem.Write("Client updates available at https://wlo.kiaoragaming.xyz:8080/client-updates/manifest.json");
                }
                catch (Exception e)
                {
                    DebugSystem.Write(new ExceptionData(e));
                }
            }

            #region Load Data Files
            //cGlobal.gItemManager.LoadItems("Data\\Item.dat");
            //cGlobal.gSkillManager.LoadSkills("Data\\Skill.dat");
            //cGlobal.gNpcManager.LoadNpc("Data\\Npc.dat");
            //cGlobal.gEveManager.LoadFile("Data\\eve.Emg");
            //cGlobal.gCompoundDat.Load("Data\\Compound.dat");
            //cGlobal.gCompoundDat.Load("Data\\Compound2.dat", false);

            #endregion

            DebugSystem.Write("[Init] - Intializing Server Please Wait.....");
            #region Initialize Server Components
            cGlobal.gWorld.Initialize();            
            cGlobal.gLoginServer.Initialize();
            // Check for updates asynchronously to prevent UI freeze
            Task.Run(() => cGlobal.Update_System.CheckForUpdates_Application());

            //cGlobal.WLO_World.Initialize();
            Thread.Sleep(2);
            //cGlobal.TcpListener.Initialize();
            #endregion


            do
            {
                #region Thread Management
                //try
                //{
                //    Thread r;
                //    foreach (var t in ThreadManager. cGlobal.ThreadManager)
                //        if (!t.Value.IsAlive)
                //            if (cGlobal.ThreadManager.TryRemove(t.Key, out r))
                //                DebugSystem.Write(t.Value.Name + " has been Terminated", Utilities.LogType.THRD);
                //}
                //catch { }
                #endregion
                #region TaskManager
                cGlobal.ApplicationTasks.onUpdateTick();
                #endregion
                Thread.Sleep(10);
            }
            while (cGlobal.Run);

            ShutDown();
        }

        private void SaveConfiguration()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PServer", "Config.settings.wlo");

                Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                System.Xml.Serialization.XmlSerializer serializer = 
                    new System.Xml.Serialization.XmlSerializer(typeof(Server.Config.Settings));
                
                using (StreamWriter writer = new StreamWriter(configPath))
                    serializer.Serialize(writer, cGlobal.SrvSettings);

                DebugSystem.Write("Configuration saved successfully");
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("Configuration saved successfully.", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            catch (Exception ex)
            {
                DebugSystem.Write(new ExceptionData(ex));
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("Error saving configuration: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        public void ShutDown()
        {
            if (registrationServer != null)
                registrationServer.Stop();

            this.Invoke(new Action(() => { this.Enabled = false; }));

            UI.ShutDown_Dialog tmp = new UI.ShutDown_Dialog();
            tmp.Location = this.Location;
            tmp.Left = this.Left + 100;
            tmp.Top = this.Top + 250;

            tmp.ShowDialog();
            tmp.Dispose();

            cGlobal.Run = false;
            blockclose = false;

            
            this.Invoke(new Action(() => { Close(); }));
        }


        #region Git Client Events

        void GitClient_GitinfoUpdated(object sender, EventArgs e)
        {
            try
            {
                //this.BeginInvoke(new Action(() =>
                //{
                //    UpdatePane.Controls.Clear();

                //    foreach (var y in cGlobal.GitClient.Releases.Where(c => c.TagName != null).OrderByDescending(c => new Version(c.TagName)))
                //        UpdatePane.Controls.Add(new Gui.Update.GitUpdateItem(cGlobal.GitClient.myVersion, y));

                //}));
            }
            catch (Exception fe) { DebugSystem.Write(new ExceptionData(fe)); }
        }
        #endregion

        #region Form Events
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cGlobal.Run || blockclose)
            {
                e.Cancel = true;
                return;
            }

            if (registrationServer != null)
                registrationServer.Stop();
            cGlobal.Run = false;
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || (e.ColumnIndex != dataGridView1.Columns["retrytask"].Index)) return;

            
            if (e.ColumnIndex == dataGridView1.Columns["retrytask"].Index)
                cGlobal.ApplicationTasks.TaskItems[e.RowIndex].onRetry();
        }

        #region Update Section
        private void GitBranch_TextChanged(object sender, EventArgs e)
        {
            //if (cGlobal.GitClient != null && cGlobal.GitClient.Branch != GitBranch.Text)
            //{
            //    cGlobal.SrvSettings.Update.GitBranch = GitBranch.Text;
            //    cGlobal.GitClient.Branch = GitBranch.Text;
            //    cGlobal.GitClient.CheckFor_Update();
            //}
        }
        private void GitUptOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (cGlobal.SrvSettings.Update.UpdtControl != (Server.Config.UpdtSetting)GitUptOption.SelectedIndex)
            //    cGlobal.SrvSettings.Update.UpdtControl = (Server.Config.UpdtSetting)GitUptOption.SelectedIndex;

            //if ((Server.Config.UpdtSetting)GitUptOption.SelectedIndex == Server.Config.UpdtSetting.Never)
            //    cGlobal.ApplicationTasks.EndTask("Application Update");
            //else if ((Server.Config.UpdtSetting)GitUptOption.SelectedIndex != Server.Config.UpdtSetting.Never)
            //    cGlobal.ApplicationTasks.CreateTask("Application Update", cGlobal.SrvSettings.Update.UpdtChk_Interval);
        }
        #endregion

        private void updtrefresh_ValueChanged(object sender, EventArgs e)
        {
            //if (cGlobal.SrvSettings.Update.UpdtChk_Interval.Minutes != updtrefresh.Value)
            //    cGlobal.SrvSettings.Update.UpdtChk_Interval = new TimeSpan(0,(int)updtrefresh.Value,0);

            //if ((Server.Config.UpdtSetting)GitUptOption.SelectedIndex != Server.Config.UpdtSetting.Never)
            //    cGlobal.ApplicationTasks.ChangeInterval("Application Update", cGlobal.SrvSettings.Update.UpdtChk_Interval);
        }
        private void autoUpdt_Hr_ValueChanged(object sender, EventArgs e)
        {
            //cGlobal.SrvSettings.Update.AutoUpdt_Schedule = new TimeSpan((int)autoUpdt_Hr.Value, (int)autoUpdt_Min.Value, 0);
        }
        private void autoUpdt_Min_ValueChanged(object sender, EventArgs e)
        {
            //cGlobal.SrvSettings.Update.AutoUpdt_Schedule = new TimeSpan((int)autoUpdt_Hr.Value, (int)autoUpdt_Min.Value, 0);
        }
        
        #endregion

        private void updtschedule_CheckedChanged(object sender, EventArgs e)
        {
            cGlobal.SrvSettings.Update.EnableSchedUpdate = updtschedule.Checked;
        }

        private void updtday_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void updttime_ValueChanged(object sender, EventArgs e)
        {

        }

        private void updttime2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void updt_warn_how_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void updt_warn_CheckedChanged(object sender, EventArgs e)
        {
            cGlobal.SrvSettings.Update.WarnofUpdate = updt_warn.Checked;
        }



        

    }
}
