#region Copyright Notice
/* Copyright (c) 2017, Deb'jyoti Das - debjyoti@debjyoti.com
 All rights reserved.
 Redistribution and use in source and binary forms, with or without
 modification, are not permitted.Neither the name of the 
 'Deb'jyoti Das' nor the names of its contributors may be used 
 to endorse or promote products derived from this software without 
 specific prior written permission.
 THIS SOFTWARE IS PROVIDED BY Deb'jyoti Das 'AS IS' AND ANY
 EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 DISCLAIMED. IN NO EVENT SHALL Debjyoti OR Deb'jyoti OR Debojyoti Das OR Eyedia BE LIABLE FOR ANY
 DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#region Developer Information
/*
Author  - Debjyoti Das (debjyoti@debjyoti.com)
Created - 11/12/2017 3:31:31 PM
Description  - 
Modified By - 
Description  - 
*/
#endregion Developer Information

#endregion Copyright Notice
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Eyedia.Aarbac.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GenericParsing;
using System.IO;
using System.Drawing.Design;
using System.Collections.Specialized;

namespace Eyedia.Aarbac.Win
{
    public partial class frmMain : Form
    {
        public frmMain(frmAuthenticate authenticateWindow)
        {
            _AuthenticateWindow = authenticateWindow;
            InitializeComponent();
            Bind();
            SetDefault();
            LoadAssemblies();

            TypeDescriptor.AddAttributes(typeof(String),
                new EditorAttribute(typeof(PropertyGridEditor), typeof(UITypeEditor)));

            toolStripStatusLabel0.Text = authenticateWindow.User.UserName;            
            Cursor = Cursors.Default;
        }

        frmAuthenticate _AuthenticateWindow;
        RbacEngineWebRequest _Request;
        private void Bind()
        {
            cbInstances.DataSource = Rbac.GetRbacs();
            cbInstances.DisplayMember = "Name";

            List<RbacUser> users = Rbac.GetUsers();
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].Role == null)
                    users[i] = users[i].PopulateRole();
            }
            cbUsers.DataSource = users;
            cbUsers.DisplayMember = "UserName";

            cbRoles.DataSource = Rbac.GetRoles();
            cbRoles.DisplayMember = "Name";

            _Request = new RbacEngineWebRequest();
            engineInput.SelectedObject = _Request;
            
        }

        private void SetDefault()
        {
            try
            {
                if (_AuthenticateWindow.User != null)
                {
                    SelectedRbacId = _AuthenticateWindow.User.Role.RbacId;
                    SelectedUser = _AuthenticateWindow.User;
                    SelectedRole = _AuthenticateWindow.User.Role;
                }
                else
                {
                    if (cbInstances.Items.Count > 0)
                        cbInstances.SelectedIndex = 0;
                    if (cbUsers.Items.Count > 0)
                        cbUsers.Text = "Lashawn";
                    if (cbRoles.Items.Count > 0)
                        cbRoles.Text = "role_city_mgr";
                }
                txtQuery.Text = "select * from Author";

                _Request.RbacName = ((Rbac)cbInstances.SelectedItem).Name;
                _Request.UserName = ((RbacUser)cbUsers.SelectedItem).UserName;
                _Request.RoleName = ((RbacRole)cbRoles.SelectedItem).Name;
                SetStatusText("Ready");
            }
            catch { }
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            SetStatusText("Parsing...");
            
            txtErrors.Text = string.Empty;
            txtParsedQuerys1.Text = string.Empty;
            txtParsedQuery.Text = string.Empty;
            txtErrors.Visible = false;
            RbacEngineWebResponse response = new RbacEngineWebResponse();
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _Request.RbacName = ((Rbac)cbInstances.SelectedItem).Name;
                _Request.UserName = ((RbacUser)cbUsers.SelectedItem).UserName;
                _Request.RoleName = ((RbacRole)cbRoles.SelectedItem).Name;
                _Request.Query = txtQuery.Text;
                engineInput.SelectedObject = _Request;

                using (Rbac ctx = new Rbac(_Request.UserName, _Request.RbacName, _Request.RoleName))
                {
                    SqlQueryParser parser = new SqlQueryParser(ctx, _Request.SkipParsing);
                    parser.Parse(_Request.Query);
                    response.SetResult(parser);
                    BindResult(response);
                    SetStatusText("Parsing...Done.", response);
                    

                    if (parser.QueryType == RbacQueryTypes.Select)
                    {
                        SetStatusText("Parsing...Done. Executing...", response);
                        
                        using (RbacSqlQueryEngine eng = new RbacSqlQueryEngine(parser, _Request.DebugMode))
                        {
                            eng.SkipExecution = _Request.SkipExecution;
                            eng.Execute();
                            response.SetResult(eng);
                            SetStatusText("Parsing...Done. Executing...Done.", response);                            
                        }
                    }
                }

            }
            catch (RbacException ex)
            {
                txtErrors.Text = ex.Message;
                txtErrors.Visible = true;
                SetStatusText("Done.");
            }

            BindResult(response);
            tabControl1.SelectedIndex = 0;
            this.Cursor = Cursors.Default;
        }

        private void SetStatusText(string message, RbacEngineWebResponse response = null)
        {        
            toolStripStatusLabel1.Text = message;
            toolStripStatusLabel2.Text = _Request.RbacName;
            toolStripStatusLabel3.Text = _Request.UserName;
            toolStripStatusLabel4.Text = _Request.RoleName;
            toolStripStatusLabel5.Text = "00h:00m:00s:000ms";
            toolStripStatusLabel6.Text = "0 rows";

            if (response != null)
            {
                toolStripStatusLabel5.Text = response.ExecutionTime;
                toolStripStatusLabel6.Text = string.Format("{0} rows", response.Table != null ? response.Table.Rows.Count : 0);
            }

            Application.DoEvents();
        }
        private void BindResult(RbacEngineWebResponse response)
        {
            treeView1.Nodes.Clear();
            response.RbacName = _Request.RbacName;
            response.UserName = _Request.UserName;
            response.RoleName = _Request.RoleName;
            if (string.IsNullOrEmpty(response.Errors))
            {
                txtParsedQuerys1.Text = FormatQuery(response.ParsedQueryStage1);
                txtParsedQuery.Text = FormatQuery(response.ParsedQuery);
                TreeNode root = treeView1.Nodes.Add("Root");
                JToken token = JToken.Parse(JsonConvert.SerializeObject(response));
                Traverse(token, root);
                root.Expand();
            }
            else
            {
                txtErrors.Visible = true;
                txtErrors.Text = response.Errors;
            }
        }

        private string FormatQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
                return query;

            query = query.Replace("where", Environment.NewLine + "where").Replace("inner", Environment.NewLine + "inner");
            query = query.Replace("WHERE", Environment.NewLine + "WHERE");
            query = query.Replace(" in ", Environment.NewLine + " in ");
            query = query.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            return query;
        }

        private void Traverse(JToken token, TreeNode tn)
        {
            if (token is JProperty)
                if (token.First is JValue)
                    tn.Nodes.Add(((JProperty)token).Name + ": " + ((JProperty)token).Value);
                else
                    tn = tn.Nodes.Add(((JProperty)token).Name);

            foreach (JToken token2 in token.Children())
                Traverse(token2, tn);
        }

        private void showLoadedQueriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainerBase.Panel1Collapsed = !showLoadedQueriesToolStripMenuItem.Checked;
        }

        private void loadQueriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    GenericParserAdapter parser = new GenericParserAdapter(openFileDialog1.FileName);
                    parser.FirstRowHasHeader = true;
                    DataTable table = parser.GetDataTable();
                    table.TableName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                    lvwQueries.Tag = table;
                    foreach (DataRow row in table.Rows)
                    {
                        ListViewItem lvi = new ListViewItem(row["Query"].ToString());
                        lvi.SubItems.Add(row["User"].ToString());
                        lvi.SubItems.Add(row["Role"].ToString());
                        lvi.Tag = row;
                        lvwQueries.Items.Add(lvi);
                    }
                    splitContainerBase.Panel1Collapsed = false;
                }
                catch
                {
                    MessageBox.Show("Error! This operation accepts a csv file with 3 columns 'Instance', 'Query', 'User', 'Role'. The first row expected is header.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void lvwQueries_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwQueries.SelectedItems.Count == 1)
            {
                this.Cursor = Cursors.WaitCursor;
                DataRow row = (DataRow)lvwQueries.SelectedItems[0].Tag;
                //cbInstances.Text = row["Instance"].ToString();
                cbUsers.Text = row["User"].ToString();
                cbRoles.Text = row["Role"].ToString();
                txtQuery.Text = FormatQuery(row["Query"].ToString());
                btnExecute_Click(sender, e);
                this.Cursor = Cursors.Default;
            }
        }

      
        private void btnExecuteAll_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            toolStripProgressBar1.Visible = true;
            if (lvwQueries.Tag != null)
            {
                DataTable table = lvwQueries.Tag as DataTable;
                toolStripProgressBar1.Maximum = table.Rows.Count;
                if (table.Columns["ParsedQueryStage1"] == null)
                {
                    table.Columns.Add("ParsedQueryStage1");
                    table.Columns.Add("ParsedQuery");
                    table.Columns.Add("Errors");
                }
                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        Rbac rbac = new Rbac(row["User"].ToString(), "Books", row["Role"].ToString());

                        RbacEngineWebResponse response = new RbacEngineWebResponse();
                        this.Cursor = Cursors.WaitCursor;                       
                        _Request.RbacName = rbac.Name;
                        _Request.UserName = rbac.User.UserName;
                        _Request.RoleName = rbac.User.Role.Name;
                        _Request.Query = row["Query"].ToString();

                        SqlQueryParser parser = new SqlQueryParser(rbac);
                        parser.Parse(_Request.Query);
                        response.SetResult(parser);                        
                        SetStatusText("Parsing...Done.", response);

                        if (parser.QueryType == RbacQueryTypes.Select)
                        {
                            SetStatusText("Parsing...Done. Executing...", response);
                            RbacSqlQueryEngine engine = new RbacSqlQueryEngine(parser, true);
                            engine.Execute();
                            response.SetResult(engine);
                            SetStatusText("Parsing...Done. Executing...Done.", response);
                        }
                        row["ParsedQueryStage1"] = parser.ParsedQueryStage1;
                        row["ParsedQuery"] = parser.ParsedQuery;
                        row["Errors"] = parser.AllErrors + Environment.NewLine;
                        SetStatusText("Done.", response);
                    }
                    catch (Exception ex)
                    {
                        row["Errors"] = ex.Message;                        
                    }

                    toolStripProgressBar1.PerformStep();
                    Application.DoEvents();                    
                }
                toolStripProgressBar1.Visible = false;
                string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, table.TableName + "_out.csv");
                try
                {
                    table.ToCsv(fileName);
                    MessageBox.Show("Test results are saved on " + fileName + "!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Cursor = Cursors.Default;
                    MessageBox.Show(ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                tabControl1.SelectedIndex = 0;
                Cursor = Cursors.Default;
            }
        }

        private void cbInstances_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbInstances.SelectedIndex > -1)
            {
                propInstance.SelectedObject = new RbacEngineWeb(Rbac.GetRbac(((Rbac)cbInstances.SelectedItem).Name));
                tabPage2.Text = ((RbacEngineWeb)propInstance.SelectedObject).Name;

                ParseInline();
            }
                
        }

        private int SelectedRbacId
        {
            get
            {
                if (cbInstances.SelectedItem != null)
                    return ((Rbac)cbInstances.SelectedItem).RbacId;
                else
                    return -1;
            }
            set
            {
                var item = cbInstances.Items.Cast<Rbac>().Where(r => r.RbacId == value).SingleOrDefault();
                cbInstances.SelectedItem = item;
            }
        }
        private RbacRole SelectedRole
        {
            get
            {
                return (cbRoles.SelectedItem != null) ? (RbacRole)cbRoles.SelectedItem : null;
            }
            set
            {
                if (value != null)
                {
                    var item = cbRoles.Items.Cast<RbacRole>().Where(r => r.Name == value.Name).SingleOrDefault();
                    cbRoles.SelectedItem = item;
                }
                else
                {
                    cbRoles.SelectedIndex = -1;
                }
            }
        }

        private RbacUser SelectedUser
        {
            get
            {
                return (cbUsers.SelectedItem != null) ? (RbacUser)cbUsers.SelectedItem : null;
            }
            set
            {
                if (value != null)
                {
                    var item = cbUsers.Items.Cast<RbacUser>().Where(r => r.UserName == value.UserName).SingleOrDefault();
                    cbUsers.SelectedItem = item;
                }
                else
                {
                    cbUsers.SelectedIndex = -1;
                }
            }
        }

        private void cbUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbUsers.SelectedIndex > -1)
            {
                var user = (RbacUser)cbUsers.SelectedItem;
                //int roleId = 0;
                //if (cbRoles.SelectedItem != null)
                //    roleId = ((RbacRole)cbRoles.SelectedItem).RoleId;                
                SelectedRole = user.Role;

                if(SelectedRole != null)                
                    propUser.SelectedObject = new RbacRegisterUser(SelectedRole.RoleId, user.UserName, user.FullName, user.Email, string.Empty);
                tabPage3.Text = user.UserName;

                LoadUserParameters();
                ParseInline();
            }
        }

        private void LoadUserParameters()
        {
            lvwUserParameters.Items.Clear();
            if (cbUsers.SelectedIndex > -1)
            {
                var selUser = (RbacUser)cbUsers.SelectedItem;
                RbacUser user = new RbacUser(selUser.UserName);
                if(user.Parameters != null)
                {
                    foreach(var paramter in user.Parameters)
                    {
                        ListViewItem item = new ListViewItem(paramter.Key);
                        item.SubItems.Add(paramter.Value);
                        lvwUserParameters.Items.Add(item);
                    }
                }                
            }
        }

        private void cbRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbRoles.SelectedItem != null)
            {
                RbacRole dbRole = Rbac.GetRole(((RbacRole)cbRoles.SelectedItem).Name);
                RbacRoleWeb role = new RbacRoleWeb(dbRole);

                tabPage4.Text = role.Name;
                txtRole.Text = role.MetaDataRbac;
                txtEntitlements.Text = role.MetaDataEntitlements;

                role.MetaDataRbac = string.Empty;
                role.MetaDataEntitlements = string.Empty;
                propRole.SelectedObject = role;
            }
            else
            {
                tabPage4.Tag = null;
                tabPage4.Text = "Role";
            }
            ParseInline();
        }

        private void btnSaveInstance_Click(object sender, EventArgs e)
        {
             
            if (propInstance != null)
            {
                RbacEngineWeb rbac = propInstance.SelectedObject as RbacEngineWeb;          
                Rbac.Save(rbac);
            }
        }

        private void btnSaveUser_Click(object sender, EventArgs e)
        {            
            if (propUser.SelectedObject != null)
            {
                //RbacUser user = propUser.SelectedObject as RbacUser;            
                //Rbac.Save(user);
            }
        }

        private void btnSaveRole_Click(object sender, EventArgs e)
        {
            if (propRole.SelectedObject != null)
            {
                RbacRoleWeb wRole = propRole.SelectedObject as RbacRoleWeb;              
                wRole.MetaDataRbac = txtRole.Text;
                wRole.MetaDataEntitlements = txtEntitlements.Text;

                try
                {
                    Rbac.Save(wRole);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);                    
                }
            }
        }
        private void txtQuery_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            ParseInline();
        }

        private void ParseInline()
        {
            if (inlineParsingToolStripMenuItem.Checked)
                btnExecute_Click(null, null);
        }
       
        private void logsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            frmLog logWindow = new frmLog();
            logWindow.Show(this);
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            engineInput.Height = (int)(tabPage1.Height * .2);
            treeView1.Height = (int)(tabPage1.Height * .6);
            txtErrors.Height = (int)(tabPage1.Height * .2);

            splitContainerBase.SplitterDistance = 150;
            scRole1.SplitterDistance = 20;
            scRole2.SplitterDistance = 150;
        }

        private void LoadAssemblies()
        {
            //this will load the assembly into memory, so that 2nd call is more efficient
            try
            {
                Rbac rbac = new Rbac("Lashawn", "Books", "role_city_mgr");
                SqlQueryParser parser = new SqlQueryParser(rbac);
                parser.Parse("select * from Author");
            }
            catch { }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_AuthenticateWindow != null)
                _AuthenticateWindow.Close();
        }

        private void ApplyEntitlement()
        {
            if (_AuthenticateWindow.User != null)
            {
                //menu
                //recommended way is populate menu from 0, for testing/simplicity/I dont have time - I am just updating the menus

                RbacEntitlementMenu menu = _AuthenticateWindow.User.Role.Entitlement.Menus.Menu.Get("BatchTest");
                batchTestToolStripMenuItem.Visible = menu.Visible;
                loadQueriesToolStripMenuItem.Visible = menu.SubMenus.Get("LoadQueries").Visible;
                loadQueriesToolStripMenuItem.Enabled = menu.SubMenus.Get("LoadQueries").Enabled;

                showLoadedQueriesToolStripMenuItem.Visible = menu.SubMenus.Get("ShowQueries").Visible;
                showLoadedQueriesToolStripMenuItem.Enabled = menu.SubMenus.Get("ShowQueries").Enabled;

                menu = _AuthenticateWindow.User.Role.Entitlement.Menus.Menu.Get("Tools");
                toolsToolStripMenuItem.Visible = menu.Visible;

                inlineParsingToolStripMenuItem.Visible = menu.SubMenus.Get("InlineParsing").Visible;
                inlineParsingToolStripMenuItem.Enabled = menu.SubMenus.Get("InlineParsing").Enabled;

                logsToolStripMenuItem1.Visible = menu.SubMenus.Get("Logs").Visible;
                logsToolStripMenuItem1.Enabled = menu.SubMenus.Get("Logs").Enabled;


                //screens
                //ideally page load of each screen many be a good place to redirect to "Not Authorized" page.

                RbacEntitlementScreen screen = _AuthenticateWindow.User.Role.Entitlement.Screens.Get("TestBed");

                var overrideBox = screen.SubScreens.Get("OverrideBox");
                if (overrideBox.Visible)
                {
                    cbInstances.Visible = overrideBox.SubScreens.Get("Instance").Visible;
                    cbInstances.Enabled = overrideBox.SubScreens.Get("Instance").Enabled;

                    cbUsers.Visible = overrideBox.SubScreens.Get("User").Visible;
                    cbUsers.Enabled = overrideBox.SubScreens.Get("User").Enabled;

                    cbRoles.Visible = overrideBox.SubScreens.Get("Role").Visible;
                    cbRoles.Enabled = overrideBox.SubScreens.Get("Role").Enabled;
                }
                else
                {
                    cbInstances.Visible = false;
                    cbUsers.Visible = false;
                    cbRoles.Visible = false;
                }
                btnExecute.Visible = screen.SubScreens.Get("ExecuteButton").Visible;
                btnExecute.Enabled = screen.SubScreens.Get("ExecuteButton").Enabled;
                btnExecuteAll.Visible = screen.SubScreens.Get("ExecuteAllButton").Visible;
                btnExecuteAll.Enabled = screen.SubScreens.Get("ExecuteAllButton").Enabled;

                var propBox = screen.SubScreens.Get("PropertiesBox");
                tabControl1.Visible = propBox.Visible;
                tabControl1.Enabled = propBox.Enabled;
                if (tabControl1.Visible)
                {
                    engineInput.Visible = propBox.SubScreens.Get("EngineInput").Visible;
                    engineInput.Enabled = propBox.SubScreens.Get("EngineInput").Enabled;

                    if (!propBox.SubScreens.Get("Instance").Visible)
                        tabControl1.TabPages.Remove(tabPage2);
                    else
                        tabPage2.Enabled = propBox.SubScreens.Get("Instance").Enabled;

                    if (!propBox.SubScreens.Get("User").Visible)
                        tabControl1.TabPages.Remove(tabPage3);
                    else
                        tabPage3.Enabled = propBox.SubScreens.Get("User").Enabled;

                    if (!propBox.SubScreens.Get("Role").Visible)
                        tabControl1.TabPages.Remove(tabPage4);
                    else
                        tabPage4.Enabled = propBox.SubScreens.Get("Role").Enabled;
                }

            }
        }

        private void frmMain_Activated(object sender, EventArgs e)
        {
            ApplyEntitlement();
        }
    }

    
}

