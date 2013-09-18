using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Converter.Logic;
using DbAccess;

namespace Converter.WinForms
{
    public partial class MainForm : Form
    {
        private bool _shouldExit;
        private bool _isLoading;
        private Converter.Logic.ConfigurationManager _manager;

        public MainForm()
        {
            this.InitializeComponent();
            this._manager = new ConfigurationManager();
            this._manager.ConfigurationChanged += delegate { this.UpdateUI(); };
        }

        private void UpdateUI()
        {
            try
            {
                this._isLoading = true;
                ConversionConfiguration config = this._manager.CurrentConfiguration;

                this.txtSqlAddress.Text = config.SqlServerAddress;
                this.txtSQLitePath.Text = config.SqLiteDatabaseFilePath;
                this.txtPassword.Text = config.EncryptionPassword;
                this.txtUserDB.Text = config.User;
                this.txtPassDB.Text = config.Password;

                int cboDatabaseIndex = this.cboDatabases.Items.Add(config.DatabaseName);
                this.cboDatabases.SelectedIndex = cboDatabaseIndex;

                this.cbxEncrypt.Checked = !(String.IsNullOrWhiteSpace(config.EncryptionPassword));
                this.cbxTriggers.Checked = config.CreateTriggersEnforcingForeignKeys;
                this.cbxCreateViews.Checked = config.TryToCreateViews;
                this.cbxIntegrated.Checked = config.IntegratedSecurity;

                if (config.IntegratedSecurity)
                {
                    this.lblPassword.Visible = false;
                    this.lblUser.Visible = false;
                    this.txtPassDB.Visible = false;
                    this.txtUserDB.Visible = false;
                }
                else
                {
                    this.lblPassword.Visible = true;
                    this.lblUser.Visible = true;
                    this.txtPassDB.Visible = true;
                    this.txtUserDB.Visible = true;
                }
                this._isLoading = false;

                this.UpdateSensitivity();
            }
            catch (Exception ex)
            {
                // Do nothing.
            }
        }


        private void btnBrowseSQLitePath_Click(object sender, EventArgs e)
        {
            DialogResult res = this.saveFileDialog1.ShowDialog(this);
            if (res == DialogResult.Cancel)
            {
                return;
            }

            string fpath = this.saveFileDialog1.FileName;
            this._manager.CurrentConfiguration.SqLiteDatabaseFilePath = fpath;
            this.pbrProgress.Value = 0;
            this.AddMessage(String.Format("Output file set: {0}", fpath));
        }

        private void cboDatabases_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox control = (ComboBox) sender;

            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.DatabaseName = control.SelectedText;
                this.UpdateSensitivity();
                this.pbrProgress.Value = 0;
                this.AddMessage("cboDatabases - SelectedIndexChanged");
            }
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            try
            {
                ConversionConfiguration config = this._manager.CurrentConfiguration;
                string connectionString = config.ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get the names of all DBs in the database server.
                    SqlCommand query = new SqlCommand(@"select distinct [name] from sysdatabases", conn);
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        this.cboDatabases.Items.Clear();
                        while (reader.Read())
                        {
                            this.cboDatabases.Items.Add((string)reader[0]);
                        }
                        if (this.cboDatabases.Items.Count > 0)
                        {
                            this.cboDatabases.SelectedIndex = 0;
                        }
                    }
                }
                this.pbrProgress.Value = 0;
                this.AddMessage(String.Format("Connected to SQL Server ({0})", config.SqlServerAddress));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed To Connect", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSQLitePath_TextChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.SqLiteDatabaseFilePath = this.txtSQLitePath.Text;
                this.UpdateSensitivity();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.UpdateSensitivity();

            String version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text = "SQL Server To SQLite DB Converter (" + version + ")";
        }

        private void txtSqlAddress_TextChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.SqlServerAddress = this.txtSqlAddress.Text;
                this.UpdateSensitivity();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SqlServerToSQLite.CancelConversion();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SqlServerToSQLite.IsActive)
            {
                SqlServerToSQLite.CancelConversion();
                this._shouldExit = true;
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }
        }

        private void cbxEncrypt_CheckedChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                // There is no flag for SQLite encryption.
                // The presence of a value in that property implicitly defines the value.
                this.UpdateSensitivity();
            }
        }

        private void txtUserDB_TextChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.User = this.txtUserDB.Text;
            }
        }

        private void txtPassDB_TextChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.Password = this.txtPassDB.Text;
            }
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.EncryptionPassword = this.txtPassword.Text;
                this.UpdateSensitivity();
            }
        }

        private void cbxTriggers_CheckedChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.CreateTriggersEnforcingForeignKeys = this.cbxTriggers.Checked;
            }
        }

        private void cbxCreateViews_CheckedChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.TryToCreateViews = this.cbxCreateViews.Checked;
            }
        }

        private void ChkIntegratedCheckedChanged(object sender, EventArgs e)
        {
            if (!this._isLoading)
            {
                this._manager.CurrentConfiguration.IntegratedSecurity = this.cbxIntegrated.Checked;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            ConversionConfiguration config = this._manager.CurrentConfiguration;
            string sqlConnString = config.ConnectionString;

            this.Cursor = Cursors.WaitCursor;
            SqlConversionHandler handler = this.OnSqlConversionHandler;
            SqlTableSelectionHandler selectionHandler = this.OnSqlTableSelectionHandler;
            FailedViewDefinitionHandler viewFailureHandler = this.OnFailedViewDefinitionHandler;

            SqlServerToSQLite.ConvertSqlServerToSQLiteDatabase(sqlConnString, config.SqLiteDatabaseFilePath, config.EncryptionPassword, handler, selectionHandler, viewFailureHandler, config.CreateTriggersEnforcingForeignKeys, config.TryToCreateViews);
        }

        private string OnFailedViewDefinitionHandler(ViewSchema vs)
        {
            string updated = null;
            Invoke(new MethodInvoker(() =>
                                     {
                                         var dlg = new ViewFailureDialog();
                                         dlg.View = vs;
                                         DialogResult res = dlg.ShowDialog(this);
                                         if (res == DialogResult.OK)
                                         {
                                             updated = dlg.ViewSQL;
                                         }
                                         else
                                         {
                                             updated = null;
                                         }
                                     }));
            return updated;
        }

        private List<TableSchema> OnSqlTableSelectionHandler(List<TableSchema> schema)
        {
            var config = this._manager.CurrentConfiguration;

            if (config.SelectedTables.Count == 0)
            {
                List<TableSchema> updated = null;
                Invoke(new MethodInvoker(delegate
                                         {
                                             // Allow the user to select which tables to include by showing him the table selection dialog.
                                             var dlg = new TableSelectionDialog();
                                             DialogResult res = dlg.ShowTables(schema, this);
                                             if (res == DialogResult.OK)
                                             {
                                                 updated = dlg.IncludedTables;
                                             }
                                         }));
                
                List<String> selectedTables = updated.Select(obj => obj.TableName).ToList();
                config.SelectedTables = selectedTables;
                return updated;
            }
            else
            {
                List<TableSchema> tablesMatchingSavedList = new List<TableSchema>();
                foreach (var tableSchema in schema)
                {
                    if (config.SelectedTables.Contains(tableSchema.TableName))
                    {
                        tablesMatchingSavedList.Add(tableSchema);
                    }
                }
                return tablesMatchingSavedList;
            }
        }

        private void OnSqlConversionHandler(bool done, bool success, int percent, string msg)
        {
            Invoke(new MethodInvoker(delegate
                                     {
                                         this.UpdateSensitivity();
                                         this.AddMessage(String.Format("{0}", msg));
                                         this.pbrProgress.Value = percent;

                                         if (!done)
                                         {
                                             return;
                                         }

                                         this.btnStart.Enabled = true;
                                         this.Cursor = Cursors.Default;
                                         this.UpdateSensitivity();

                                         if (success)
                                         {
                                             MessageBox.Show(this, msg, "Conversion Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                             this.pbrProgress.Value = 0;
                                             this.AddMessage("Conversion Finished.");
                                         }
                                         else
                                         {
                                             if (!this._shouldExit)
                                             {
                                                 MessageBox.Show(this, msg, "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                 this.pbrProgress.Value = 0;
                                                 this.AddMessage("Conversion Failed!");
                                             }
                                             else
                                             {
                                                 Application.Exit();
                                             }
                                         }
                                     }));
        }

        #region Private Methods
        private void UpdateSensitivity()
        {
            Invoke(new MethodInvoker(this.UpdateSensitivitySafe));
        }

        private void UpdateSensitivitySafe()
        {
            if (this.txtSQLitePath.Text.Trim().Length > 0 && (!this.cbxEncrypt.Checked || this.txtPassword.Text.Trim().Length > 0))
            {
                this.btnStart.Enabled = true && !SqlServerToSQLite.IsActive;
            }
            else
            {
                this.btnStart.Enabled = false;
            }

            this.btnSet.Enabled = this._manager.CurrentConfiguration.SqlServerAddress.Trim().Length > 0 && !SqlServerToSQLite.IsActive;
            this.btnCancel.Visible = SqlServerToSQLite.IsActive;
            this.txtSqlAddress.Enabled = !SqlServerToSQLite.IsActive;
            this.txtSQLitePath.Enabled = !SqlServerToSQLite.IsActive;
            this.btnBrowseSQLitePath.Enabled = !SqlServerToSQLite.IsActive;
            this.cbxEncrypt.Enabled = !SqlServerToSQLite.IsActive;
            this.cboDatabases.Enabled = !SqlServerToSQLite.IsActive;
            this.txtPassword.Enabled = this.cbxEncrypt.Checked && this.cbxEncrypt.Enabled;
            this.cbxIntegrated.Enabled = !SqlServerToSQLite.IsActive;
            this.cbxCreateViews.Enabled = !SqlServerToSQLite.IsActive;
            this.cbxTriggers.Enabled = !SqlServerToSQLite.IsActive;
            this.txtPassDB.Enabled = !SqlServerToSQLite.IsActive;
            this.txtUserDB.Enabled = !SqlServerToSQLite.IsActive;
        }
        #endregion

        private void ToolStripMenuItemNew(object sender, EventArgs e)
        {
            this._manager.CurrentConfiguration = new ConversionConfiguration();
        }

        private void ToolStripMenuItemOpen(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dlg.Multiselect = false;
            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                String filename = dlg.FileName;
                ConversionConfiguration config = null;
                Boolean success = SerializationHelper.TryXmlDeserialize(filename, out config);

                if (success)
                {
                    this._manager.CurrentConfiguration = config;
                }
                else
                {
                    throw new Exception("File couldn't be opened.");
                }
            }
        }

        private void ToolStripMenuItemSave(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = "xml";
            dlg.FileName = "SqlConverter.Configuration.xml";
            dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                ConversionConfiguration config = this._manager.CurrentConfiguration;
                var sw = new StreamWriter(dlg.OpenFile());
                sw.Write(config.SerializedXml);
                sw.Flush();
                sw.Close();
            }
        }

        private void ToolStripMenuItemExit(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void AddMessage(String msg)
        {
            this.lbMessages.Items.Add(msg);
            int visibleItems = this.lbMessages.ClientSize.Height / this.lbMessages.ItemHeight;
            this.lbMessages.TopIndex = Math.Max(this.lbMessages.Items.Count - visibleItems + 1, 0);

        }
    }
}