using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GIS2025
{
    public class FrmStatsReport : Form
    {
        // 控件定义
        private DataGridView dgvVisited;
        private DataGridView dgvUnvisited;
        private Label lblSummary;

        // 构造函数：接收统计数据和所有区域列表
        public FrmStatsReport(Dictionary<string, int> visitedStats, List<string> allRegions)
        {
            this.Text = "足迹覆盖率统计报告";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            // ★关键修改1：允许调整大小，且不作为模态对话框时也能正常显示
            this.TopMost = true; // 可选：让它保持在最前，或者去掉这行让它普通显示

            InitializeCustomLayout(visitedStats, allRegions);
        }

        private void InitializeCustomLayout(Dictionary<string, int> visitedStats, List<string> allRegions)
        {
            // 1. 数据处理
            // (1) 找出没去过的
            var unvisitedList = allRegions.Where(r => !visitedStats.ContainsKey(r)).OrderBy(r => r).ToList();

            // (2) 找出去过的，并【按站数由多到少排序】 (关键修改3)
            var visitedListSorted = visitedStats
                .Select(kvp => new { Name = kvp.Key, Count = kvp.Value })
                .OrderByDescending(x => x.Count)
                .ToList();

            // (3) 计算概览数据
            int total = allRegions.Count;
            int visitedCount = visitedListSorted.Count;
            double ratio = total > 0 ? (double)visitedCount / total : 0;

            // 2. 界面布局
            // 使用 TableLayoutPanel 做主容器：上面是概览，下面是详细列表
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.RowCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F)); // 概览区域高度
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 列表区域自动填充
            this.Controls.Add(mainLayout);

            // --- 板块1：统计概览 ---
            GroupBox grpSummary = new GroupBox();
            grpSummary.Text = "统计概览";
            grpSummary.Dock = DockStyle.Fill;
            grpSummary.Font = new Font("微软雅黑", 10F, FontStyle.Bold);

            lblSummary = new Label();
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.TextAlign = ContentAlignment.MiddleCenter;
            lblSummary.Text = $"总单元：{total} 个    |    已打卡：{visitedCount} 个    |    覆盖率：{ratio:P2}"; // P2自动转百分比保留2位
            lblSummary.ForeColor = Color.DarkBlue;
            grpSummary.Controls.Add(lblSummary);

            mainLayout.Controls.Add(grpSummary, 0, 0);

            // --- 下半部分：左右分割 ---
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 450; // 左侧稍宽一点
            mainLayout.Controls.Add(splitContainer, 0, 1);

            // --- 板块2：去过的 (左侧) ---
            GroupBox grpVisited = new GroupBox();
            grpVisited.Text = $"已打卡 ({visitedCount}) - 按热度排序";
            grpVisited.Dock = DockStyle.Fill;

            dgvVisited = CreateStyleGridView();
            // 添加列
            dgvVisited.Columns.Add("Rank", "排名");
            dgvVisited.Columns.Add("Region", "行政区/乡镇");
            dgvVisited.Columns.Add("Count", "打卡站数");
            // 填充数据
            for (int i = 0; i < visitedListSorted.Count; i++)
            {
                dgvVisited.Rows.Add(i + 1, visitedListSorted[i].Name, visitedListSorted[i].Count);
            }
            // 调整列宽
            dgvVisited.Columns[0].Width = 60;
            dgvVisited.Columns[2].Width = 80;
            dgvVisited.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            grpVisited.Controls.Add(dgvVisited);
            splitContainer.Panel1.Controls.Add(grpVisited);

            // --- 板块3：没去过的 (右侧) ---
            GroupBox grpUnvisited = new GroupBox();
            grpUnvisited.Text = $"未打卡 ({unvisitedList.Count})";
            grpUnvisited.Dock = DockStyle.Fill;

            dgvUnvisited = CreateStyleGridView();
            // 添加列
            dgvUnvisited.Columns.Add("Region", "行政区/乡镇");
            // 填充数据
            foreach (var name in unvisitedList)
            {
                dgvUnvisited.Rows.Add(name);
            }
            dgvUnvisited.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            grpUnvisited.Controls.Add(dgvUnvisited);
            splitContainer.Panel2.Controls.Add(grpUnvisited);
        }

        // 辅助方法：统一设置表格样式 (美观方面)
        private DataGridView CreateStyleGridView()
        {
            DataGridView dgv = new DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false; // 隐藏最左侧的空白头
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // 整行选中
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.WhiteSmoke; // 隔行变色
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
            dgv.GridColor = Color.LightGray;
            return dgv;
        }
    }
}