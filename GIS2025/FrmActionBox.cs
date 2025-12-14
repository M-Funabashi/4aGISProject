using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GIS2025
{
    public enum ActionType
    {
        Success, //成功提示
        Error, //失败提示
        Loading, //加载中提示
        Confirm  //确认窗口
    }

    public partial class FrmActionBox : Form
    {
        private PictureBox pbxIcon;
        private Label lblMessage;

        // 确认按钮
        private Button btnYes;
        private Button btnNo;

        // 静态实例，用于Loading状态下的程序化关闭
        public static FrmActionBox CurrentLoadingBox = null;

        // 构造函数
        public FrmActionBox(ActionType type, string message)
        {
            InitializeCustomComponents(type); // 传入类型以区分布局
            SetupContent(type, message);  // 设置内容
        }

        // 初始化UI布局
        private void InitializeCustomComponents(ActionType type)
        {
            this.FormBorderStyle = FormBorderStyle.None; 
            this.StartPosition = FormStartPosition.CenterScreen; 
            this.BackColor = Color.Gray; 
            this.Padding = new Padding(1); 

            // 容器
            Panel container = new Panel();
            container.Dock = DockStyle.Fill;
            container.BackColor = Color.White;
            this.Controls.Add(container);

            // 上半部分图片
            pbxIcon = new PictureBox();
            pbxIcon.Size = new Size(200, 100); // 图片显示大小
            pbxIcon.SizeMode = PictureBoxSizeMode.Zoom; // 缩放模式
            pbxIcon.Location = new Point((300 - pbxIcon.Width) / 2, 20); // 水平居中 (假设宽300)
            container.Controls.Add(pbxIcon);

            // 下半部分文字
            lblMessage = new Label();
            lblMessage.AutoSize = false;
            lblMessage.Size = new Size(260, 60); 
            lblMessage.TextAlign = ContentAlignment.TopCenter; 
            lblMessage.Font = new Font("微软雅黑", 12F, FontStyle.Regular); 
            lblMessage.ForeColor = Color.DarkGray;
            lblMessage.Location = new Point(20, 130);
            container.Controls.Add(lblMessage);

            if (type == ActionType.Confirm)
            {
                // 确认模式
                this.Size = new Size(300, 260); // 增加高度以容纳按钮

                // 初始化按钮
                btnYes = CreateButton("是", Color.SeaGreen, new Point(40, 200));
                btnYes.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };
                container.Controls.Add(btnYes);
                btnNo = CreateButton("否", Color.IndianRed, new Point(160, 200));
                btnNo.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };
                container.Controls.Add(btnNo);

                // 确认模式下，不绑定 Click=Close 事件，强制用户点击按钮
            }
            else
            {
                this.Size = new Size(300, 220); 
                // 绑定点击关闭事件（点哪里都关闭）
                container.Click += Close_Click;
                pbxIcon.Click += Close_Click;
                lblMessage.Click += Close_Click;
                this.Click += Close_Click;
            }
        }

        // 辅助方法：快速创建统一风格的按钮
        private Button CreateButton(string text, Color backColor, Point loc)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(100, 35);
            btn.Location = loc;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void SetupContent(ActionType type, string message)
        {
            lblMessage.Text = message;

            // 构建图片路径
            string fileName = "";
            switch (type)
            {
                case ActionType.Success: fileName = "success.png"; break;
                case ActionType.Error: fileName = "error.png"; break;
                case ActionType.Loading: fileName = "loading.png"; break;
                case ActionType.Confirm: fileName = "warning.png"; break; 
            }

            string fullPath = Path.Combine(Application.StartupPath, @"data\pic", fileName);

            // 尝试加载图片
            if (File.Exists(fullPath))
            {
                try { pbxIcon.Image = Image.FromFile(fullPath); }
                catch { }
            }
        }

        // 点击事件处理
        private void Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // 静态辅助方法，弹出提示框
        public static DialogResult Show(string message, ActionType type)
        {
            using (FrmActionBox box = new FrmActionBox(type, message))
            {
                return box.ShowDialog();
            }
        }
    }
}