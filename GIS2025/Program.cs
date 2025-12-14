using System;
using System.Windows.Forms;

namespace GIS2025
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. 先运行用户选择界面
            FrmUserSelect loginForm = new FrmUserSelect();
            Application.Run(loginForm);

            // 2. 如果用户选择了某个账号 (DialogResult == OK) 且 CurrentUser 不为空
            if (loginForm.DialogResult == DialogResult.OK && ProfileManager.Instance.CurrentUser != null)
            {
                // 3. 启动主地图界面
                Application.Run(new FormMap()); // 注意：你的主窗体类名是 FormMap 还是 Form1？请根据实际情况填写
            }
        }
    }
}