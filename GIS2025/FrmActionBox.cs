using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GIS2025
{
    // 定义操作类型枚举
    public enum ActionType
    {
        Success,
        Error,
        Loading,
        Confirm // ★ 新增类型：确认操作
    }

    public partial class FrmActionBox : Form
    {
        // UI控件
        private PictureBox pbxIcon;
        private Label lblMessage;

        // ★ 新增：确认按钮
        private Button btnYes;
        private Button btnNo;

        // 静态实例，用于Loading状态下的程序化关闭（如果需要）
        public static FrmActionBox CurrentLoadingBox = null;

        // 构造函数
        public FrmActionBox(ActionType type, string message)
        {
            InitializeCustomComponents(type); // ★ 修改：传入类型以区分布局
            SetupContent(type, message);  // 设置内容
        }

        // 初始化UI布局（纯代码生成，无需设计器）
        // ★ 修改：增加参数 type
        private void InitializeCustomComponents(ActionType type)
        {
            this.FormBorderStyle = FormBorderStyle.None; // 无边框
            this.StartPosition = FormStartPosition.CenterScreen; // 居中显示
            this.BackColor = Color.Gray; // 边框颜色
            this.Padding = new Padding(1); // 边框宽度

            // 容器
            Panel container = new Panel();
            container.Dock = DockStyle.Fill;
            container.BackColor = Color.White;
            this.Controls.Add(container);

            // 1. 上半部分：图片
            pbxIcon = new PictureBox();
            pbxIcon.Size = new Size(200, 100); // 图片显示大小
            pbxIcon.SizeMode = PictureBoxSizeMode.Zoom; // 缩放模式
            pbxIcon.Location = new Point((300 - pbxIcon.Width) / 2, 20); // 水平居中 (假设宽300)
            container.Controls.Add(pbxIcon);

            // 2. 下半部分：文字
            lblMessage = new Label();
            lblMessage.AutoSize = false;
            lblMessage.Size = new Size(260, 60); // 文字区域大小
            lblMessage.TextAlign = ContentAlignment.TopCenter; // 文字居中
            lblMessage.Font = new Font("微软雅黑", 12F, FontStyle.Regular); // 字体
            lblMessage.ForeColor = Color.DarkGray;
            // 稍作调整文字位置，避免和按钮拥挤
            lblMessage.Location = new Point(20, 130);
            container.Controls.Add(lblMessage);

            // ★★★ 核心逻辑分叉 ★★★
            if (type == ActionType.Confirm)
            {
                // --- 确认模式 ---
                this.Size = new Size(300, 260); // 增加高度以容纳按钮

                // 初始化 "是" 按钮
                btnYes = CreateButton("是", Color.SeaGreen, new Point(40, 200));
                btnYes.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };
                container.Controls.Add(btnYes);

                // 初始化 "否" 按钮
                btnNo = CreateButton("否", Color.IndianRed, new Point(160, 200));
                btnNo.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };
                container.Controls.Add(btnNo);

                // ★ 注意：确认模式下，不绑定 Click=Close 事件，强制用户点击按钮
            }
            else
            {
                // --- 普通模式 (Success/Error/Loading) ---
                this.Size = new Size(300, 220); // 原始高度

                // ★ 绑定点击关闭事件（点哪里都关闭）
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
                case ActionType.Confirm: fileName = "warning.png"; break; // ★ 新增图片映射
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

        // --- 静态辅助方法，方便外部调用 ---

        /// <summary>
        /// 弹出提示框
        /// </summary>
        /// <returns>返回用户的操作结果 (Yes/No/None)</returns>
        public static DialogResult Show(string message, ActionType type)
        {
            using (FrmActionBox box = new FrmActionBox(type, message))
            {
                // ShowDialog 会返回你在按钮点击事件中设置的 DialogResult
                return box.ShowDialog();
            }
        }
    }
}