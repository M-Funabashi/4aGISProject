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
            this.TopMost = true;
            InitializeCustomLayout(visitedStats, allRegions);
        }

        private void InitializeCustomLayout(Dictionary<string, int> visitedStats, List<string> allRegions)
        {
            // 数据处理
            // 找出没去过的
            var unvisitedList = allRegions.Where(r => !visitedStats.ContainsKey(r)).OrderBy(r => r).ToList();

            // 找出去过的，并按站数由多到少排序
            var visitedListSorted = visitedStats
                .Select(kvp => new { Name = kvp.Key, Count = kvp.Value })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 计算概览数据
            int total = allRegions.Count;
            int visitedCount = visitedListSorted.Count;
            double ratio = total > 0 ? (double)visitedCount / total : 0;

            // 界面布局
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.RowCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F)); 
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); 
            this.Controls.Add(mainLayout);

            // 统计概览
            GroupBox grpSummary = new GroupBox();
            grpSummary.Text = "统计概览";
            grpSummary.Dock = DockStyle.Fill;
            grpSummary.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            lblSummary = new Label();
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.TextAlign = ContentAlignment.MiddleCenter;
            lblSummary.Text = $"角落总数：{total} 个    |    已覆盖：{visitedCount} 个    |    覆盖率：{ratio:P2}"; 
            lblSummary.ForeColor = Color.DarkBlue;
            grpSummary.Controls.Add(lblSummary);

            mainLayout.Controls.Add(grpSummary, 0, 0);

            // 左右分割
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 450;
            mainLayout.Controls.Add(splitContainer, 0, 1);

            // 去过的地方
            GroupBox grpVisited = new GroupBox();
            grpVisited.Text = $"已覆盖 ({visitedCount}) - 按站数排序";
            grpVisited.Dock = DockStyle.Fill;

            dgvVisited = CreateStyleGridView();
            dgvVisited.Columns.Add("Rank", "排名");
            dgvVisited.Columns.Add("Region", "区域名");
            dgvVisited.Columns.Add("Count", "覆盖站数");

            for (int i = 0; i < visitedListSorted.Count; i++)
            {
                dgvVisited.Rows.Add(i + 1, visitedListSorted[i].Name, visitedListSorted[i].Count);
            }
            dgvVisited.Columns[0].Width = 60;
            dgvVisited.Columns[2].Width = 80;
            dgvVisited.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            grpVisited.Controls.Add(dgvVisited);
            splitContainer.Panel1.Controls.Add(grpVisited);

            // 没去过的
            GroupBox grpUnvisited = new GroupBox();
            grpUnvisited.Text = $"未覆盖 ({unvisitedList.Count})";
            grpUnvisited.Dock = DockStyle.Fill;
            dgvUnvisited = CreateStyleGridView();
            dgvUnvisited.Columns.Add("Region", "区域名");

            foreach (var name in unvisitedList)
            {
                dgvUnvisited.Rows.Add(name);
            }
            dgvUnvisited.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            grpUnvisited.Controls.Add(dgvUnvisited);
            splitContainer.Panel2.Controls.Add(grpUnvisited);
        }

        // 统一设置表格样式
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