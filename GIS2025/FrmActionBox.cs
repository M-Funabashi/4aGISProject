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
        Loading
    }

    public partial class FrmActionBox : Form
    {
        // UI控件
        private PictureBox pbxIcon;
        private Label lblMessage;

        // 静态实例，用于Loading状态下的程序化关闭（如果需要）
        public static FrmActionBox CurrentLoadingBox = null;

        // 构造函数
        public FrmActionBox(ActionType type, string message)
        {
            InitializeCustomComponents(); // 初始化UI
            SetupContent(type, message);  // 设置内容
        }

        // 初始化UI布局（纯代码生成，无需设计器）
        private void InitializeCustomComponents()
        {
            this.FormBorderStyle = FormBorderStyle.None; // 无边框
            this.StartPosition = FormStartPosition.CenterScreen; // 居中显示
            this.Size = new Size(300, 220); // 提示框大小，可根据需要调整
            this.BackColor = Color.White; // 背景色

            // 添加一个简单的边框效果（可选，通过Paint事件画边框，这里简化用Padding和背景区分）
            // 简单起见，我们设置一个浅灰色背景，再放一个略小的白色Panel在中间也可以，
            // 或者直接用Paint事件画一圈黑线。这里为了简洁，直接用灰色背景加白色内容。
            this.Padding = new Padding(1);
            this.BackColor = Color.Gray;
            Panel container = new Panel();
            container.Dock = DockStyle.Fill;
            container.BackColor = Color.White;
            this.Controls.Add(container);

            // 1. 上半部分：图片
            pbxIcon = new PictureBox();
            pbxIcon.Size = new Size(200, 100); // 图片显示大小
            pbxIcon.SizeMode = PictureBoxSizeMode.Zoom; // 缩放模式
            pbxIcon.Location = new Point((this.Width - pbxIcon.Width) / 2, 10); // 水平居中，距离顶部40
            container.Controls.Add(pbxIcon);

            // 2. 下半部分：文字
            lblMessage = new Label();
            lblMessage.AutoSize = false;
            lblMessage.Size = new Size(260, 60); // 文字区域大小
            lblMessage.Location = new Point(20, 130); // 位置
            lblMessage.TextAlign = ContentAlignment.TopCenter; // 文字居中
            lblMessage.Font = new Font("微软雅黑", 12F, FontStyle.Regular); // 字体
            lblMessage.ForeColor = Color.DarkGray;
            container.Controls.Add(lblMessage);

            // 3. 绑定点击关闭事件（点哪里都关闭）
            container.Click += Close_Click;
            pbxIcon.Click += Close_Click;
            lblMessage.Click += Close_Click;
            this.Click += Close_Click;
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
            }

            string fullPath = Path.Combine(Application.StartupPath, @"data\pic", fileName);

            // 尝试加载图片，防止报错
            if (File.Exists(fullPath))
            {
                try { pbxIcon.Image = Image.FromFile(fullPath); }
                catch { /* 图片损坏等异常处理，可留空或加载默认图 */ }
            }
            else
            {
                // 如果找不到图片，可以用系统图标代替，或保持空白
                // MessageBox.Show("图片丢失: " + fullPath); // 调试用
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
        public static void Show(string message, ActionType type)
        {
            using (FrmActionBox box = new FrmActionBox(type, message))
            {
                box.ShowDialog(); // 模态显示，代码会停在这里直到窗口关闭
            }
        }
    }
}