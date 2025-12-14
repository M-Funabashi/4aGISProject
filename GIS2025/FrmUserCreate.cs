using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GIS2025
{
    public class FrmUserCreate : Form
    {
        private TextBox txtName;
        private FlowLayoutPanel flpAvatars;
        private Button btnConfirm;
        private string selectedAvatar = "ch_1.png"; //默认头像
        private UserProfile _editingUser = null;

        public FrmUserCreate()
        {
            InitializeCustomComponent();
            LoadAvatars();
        }

        public FrmUserCreate(UserProfile editUser = null)
        {
            _editingUser = editUser;
            InitializeCustomComponent();
            LoadAvatars();

            // 如果是编辑模式，初始化界面数据
            if (_editingUser != null)
            {
                this.Text = "修改用户资料";
                txtName.Text = _editingUser.Name;
                // 提取文件名用于选中
                selectedAvatar = Path.GetFileName(_editingUser.AvatarPath);
            }
        }

        private void InitializeCustomComponent()
        {
            this.Text = "TransitLog - 创建新用户";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 输入名字区域
            Label lblName = new Label { Text = "请输入用户名:", Location = new Point(20, 20), AutoSize = true, Font = new Font("微软雅黑", 10) };
            txtName = new TextBox { Location = new Point(20, 50), Width = 440, Font = new Font("微软雅黑", 12) };

            // 头像选择区域
            Label lblAvatar = new Label { Text = "请选择头像:", Location = new Point(20, 90), AutoSize = true, Font = new Font("微软雅黑", 10) };
            flpAvatars = new FlowLayoutPanel
            {
                Location = new Point(20, 120),
                Size = new Size(440, 200),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            // 确定按钮
            btnConfirm = new Button
            {
                Text = "确认",
                Location = new Point(150, 340),
                Size = new Size(180, 45),
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.Click += BtnConfirm_Click;

            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblAvatar);
            this.Controls.Add(flpAvatars);
            this.Controls.Add(btnConfirm);
        }

        private void LoadAvatars()
        {
            string avatarDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "chr");
            // 加载头像图片
            for (int i = 1; i <= 10; i++)
            {
                string fileName = $"ch_{i}.png";
                string fullPath = Path.Combine(avatarDir, fileName);

                if (!File.Exists(fullPath)) continue;

                PictureBox pb = new PictureBox
                {
                    Size = new Size(60, 60),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Image.FromFile(fullPath),
                    Tag = fileName,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(10),
                    BorderStyle = (fileName == selectedAvatar) ? BorderStyle.Fixed3D : BorderStyle.None // 默认选中第一个
                };

                // 点击事件：高亮选中
                pb.Click += (s, e) =>
                {
                    selectedAvatar = (string)((PictureBox)s).Tag;
                    // 刷新所有边框
                    foreach (Control c in flpAvatars.Controls)
                    {
                        if (c is PictureBox p) p.BorderStyle = BorderStyle.None;
                    }
                    ((PictureBox)s).BorderStyle = BorderStyle.Fixed3D;
                };

                flpAvatars.Controls.Add(pb);
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name)) { FrmActionBox.Show("请输入用户名", ActionType.Error); return; }

            bool success = false;

            if (_editingUser == null)
            {
                // 新建模式
                success = ProfileManager.Instance.CreateUser(name, selectedAvatar);
                if (!success) FrmActionBox.Show("用户已存在", ActionType.Error);
            }
            else
            {
                // 编辑模式
                // 如果名字没变且头像没变，直接关闭
                string currentAvatarName = Path.GetFileName(_editingUser.AvatarPath);
                if (name == _editingUser.Name && selectedAvatar == currentAvatarName)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                success = ProfileManager.Instance.UpdateUser(_editingUser, name, selectedAvatar);
                if (!success) FrmActionBox.Show("修改失败，用户名已存在或文件被占用。", ActionType.Error);
            }

            if (success)
            {
                if (_editingUser == null) FrmActionBox.Show("创建成功", ActionType.Success);
                else FrmActionBox.Show("修改成功", ActionType.Success);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}