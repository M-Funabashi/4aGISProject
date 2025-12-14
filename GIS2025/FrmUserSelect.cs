using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace GIS2025
{
    public class FrmUserSelect : Form
    {
        private FlowLayoutPanel flpUsers;

        // 状态变量
        private UserProfile _selectedUser = null;
        private Panel _selectedCard = null;

        public FrmUserSelect()
        {
            InitializeCustomComponent();
            LoadUserList(); //启动时加载列表
        }

        private void InitializeCustomComponent()
        {
            this.Text = "TransitLog - 欢迎页面";
            this.Size = new Size(600, 550); 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 标题
            Label lblTitle = new Label
            {
                Text = "请选择用户档案",
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.DimGray
            };

            // 用户列表容器
            flpUsers = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(20)
            };
            // 点击空白处取消选中
            flpUsers.Click += (s, e) => ClearSelection();

            // 底部按钮区域
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 110, BackColor = Color.White, Padding = new Padding(10) };

            TableLayoutPanel tlpButtons = new TableLayoutPanel();
            tlpButtons.Dock = DockStyle.Fill;
            tlpButtons.RowCount = 2;
            tlpButtons.ColumnCount = 4;
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpButtons.ColumnStyles.Clear();
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // 第一行：加载用户
            Button btnLoad = CreateButton("🚀 进入手账 (加载用户)", Color.SeaGreen, Color.White);
            btnLoad.Click += BtnLoad_Click;
            tlpButtons.Controls.Add(btnLoad, 0, 0);
            tlpButtons.SetColumnSpan(btnLoad, 4); // ★ 改为跨4列

            // 第二行：功能按钮
            Button btnCreate = CreateButton("➕ 新建", Color.White, Color.Black); // 文字精简一点防拥挤
            btnCreate.Click += BtnCreate_Click;

            Button btnEdit = CreateButton("✏️ 修改", Color.White, Color.Black);
            btnEdit.Click += BtnEdit_Click;

            Button btnOpenFolder = CreateButton("📂 文件夹", Color.White, Color.Black);
            btnOpenFolder.Click += BtnOpenFolder_Click;

            Button btnDelete = CreateButton("🗑️ 删除", Color.White, Color.DarkRed);
            btnDelete.Click += BtnDelete_Click;

            // 按顺序加入面板
            tlpButtons.Controls.Add(btnCreate, 0, 1);
            tlpButtons.Controls.Add(btnEdit, 1, 1);   
            tlpButtons.Controls.Add(btnOpenFolder, 2, 1);
            tlpButtons.Controls.Add(btnDelete, 3, 1);   
            pnlBottom.Controls.Add(tlpButtons);

            this.Controls.Add(flpUsers);
            this.Controls.Add(lblTitle);
            this.Controls.Add(pnlBottom);
        }

        // 辅助方法：快速创建统一风格的按钮
        private Button CreateButton(string text, Color backColor, Color foreColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(3); // 按钮间距
            btn.Font = new Font("微软雅黑", 10);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        // 刷新用户列表
        private void LoadUserList()
        {
            flpUsers.Controls.Clear();
            _selectedUser = null; // 重置选中状态
            _selectedCard = null;

            List<UserProfile> users = ProfileManager.Instance.LoadUserList();

            // 如果没有用户，自动弹出创建窗口
            if (users.Count == 0)
            {
                Timer t = new Timer { Interval = 100 };
                t.Tick += (s, e) => { t.Stop(); BtnCreate_Click(null, null); };
                t.Start();
                return;
            }

            foreach (var user in users)
            {
                flpUsers.Controls.Add(CreateUserCard(user));
            }
        }

        // 创建单个用户卡片
        private Control CreateUserCard(UserProfile user)
        {
            Panel card = new Panel
            {
                Size = new Size(160, 200),
                BackColor = Color.White,
                Margin = new Padding(10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle, 
                Tag = user
            };

            // 头像
            PictureBox pb = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(30, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = File.Exists(user.AvatarPath) ? Image.FromFile(user.AvatarPath) : null,
                Enabled = false
            };

            // 名字
            Label lblName = new Label
            {
                Text = user.Name,
                Location = new Point(5, 130),
                Size = new Size(150, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                Enabled = false
            };

            // 里程
            Label lblDist = new Label
            {
                Text = $"里程: {GetTotalDistance(user):F1} km",
                Location = new Point(5, 160),
                Size = new Size(150, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Enabled = false
            };

            card.Controls.Add(pb);
            card.Controls.Add(lblName);
            card.Controls.Add(lblDist);

            // 点击选中逻辑
            card.Click += (s, e) =>
            {
                SelectCard(card, user);
            };

            // 双击直接进入
            card.DoubleClick += (s, e) =>
            {
                SelectCard(card, user);
                BtnLoad_Click(null, null);
            };

            return card;
        }

        // 处理卡片选中效果
        private void SelectCard(Panel card, UserProfile user)
        {
            // 还原上一个卡片的样式
            if (_selectedCard != null)
            {
                _selectedCard.BackColor = Color.White;
                _selectedCard.BorderStyle = BorderStyle.FixedSingle;
            }

            // 设置当前卡片为选中样式
            _selectedUser = user;
            _selectedCard = card;

            _selectedCard.BackColor = Color.AliceBlue;
            _selectedCard.BorderStyle = BorderStyle.Fixed3D; 
        }

        private void ClearSelection()
        {
            if (_selectedCard != null)
            {
                _selectedCard.BackColor = Color.White;
                _selectedCard.BorderStyle = BorderStyle.FixedSingle;
            }
            _selectedUser = null;
            _selectedCard = null;
        }

        // 按钮事件处理
        // 加载用户
        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (_selectedUser == null)
            {
                //MessageBox.Show("请先点击选择一个用户！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FrmActionBox.Show("请先选择一个用户！", ActionType.Error);  // 使用新的消息框
                return;
            }

            try
            {
                ProfileManager.Instance.LoginUser(_selectedUser.Name);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("登录失败：" + ex.Message);
            }
        }

        // 新建用户
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            FrmUserCreate frm = new FrmUserCreate();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadUserList();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedUser == null)
            {
                FrmActionBox.Show("请先选择一个要修改的用户！", ActionType.Error);
                return;
            }

            // 传入当前选中用户，开启编辑模式
            FrmUserCreate frm = new FrmUserCreate(_selectedUser);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                // 修改完成后刷新列表以显示新用户
                LoadUserList();
            }
        }

        // 打开文件夹
        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            if (_selectedUser == null)
            {
                //MessageBox.Show("请先选择一个用户！");
                FrmActionBox.Show("请先选择一个用户！", ActionType.Error);
                return;
            }

            string userPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", _selectedUser.Name);
            if (!Directory.Exists(userPath)) Directory.CreateDirectory(userPath);

            try
            {
                Process.Start("explorer.exe", userPath);
            }
            catch (Exception ex)
            {
                //.Show("无法打开文件夹：" + ex.Message);
                FrmActionBox.Show("发生错误：" + ex.Message, ActionType.Error);
            }
        }

        // 4. 删除用户
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedUser == null)
            {
                //MessageBox.Show("请先选择要删除的用户！");
                FrmActionBox.Show("请先选择要删除的用户！", ActionType.Error);
                return;
            }


            if (FrmActionBox.Show("确定要删除该用户吗？此操作不可恢复。", ActionType.Confirm) == DialogResult.Yes)
                {
                try
                {
                    // 物理删除
                    string userPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", _selectedUser.Name);
                    if (Directory.Exists(userPath)) Directory.Delete(userPath, true);

                    // 内存移除
                    if (ProfileManager.Instance.Users.Contains(_selectedUser))
                    {
                        ProfileManager.Instance.Users.Remove(_selectedUser);
                    }

                    // 刷新列表
                    LoadUserList();
                    FrmActionBox.Show("操作成功！", ActionType.Success);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("删除失败，可能是文件被占用：" + ex.Message);
                    FrmActionBox.Show("发生错误：" + ex.Message, ActionType.Error);
                }
            }
        }

        // 辅助方法：计算里程
        private double GetTotalDistance(UserProfile user)
        {
            if (user.TotalDistance > 0) return user.TotalDistance;

            double total = 0;
            if (user.Archives != null)
            {
                foreach (var archive in user.Archives)
                    foreach (var trip in archive.Trips)
                        total += trip.Length;
            }
            return total;
        }
    }
}