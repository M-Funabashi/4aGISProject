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

            // 先运行用户选择界面
            FrmUserSelect loginForm = new FrmUserSelect();
            Application.Run(loginForm);

            // 如果用户选择了某个账号
            if (loginForm.DialogResult == DialogResult.OK && ProfileManager.Instance.CurrentUser != null)
            {
                // 启动主地图界面
                Application.Run(new FormMap()); 
            }
        }
    }
}