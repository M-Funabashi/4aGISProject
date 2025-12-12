namespace GIS2025
{
    partial class FormMap
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvProfiles = new System.Windows.Forms.TreeView();
            this.panelToolbar = new System.Windows.Forms.FlowLayoutPanel();
            this.pbAddUser = new System.Windows.Forms.PictureBox();
            this.pbAddArchive = new System.Windows.Forms.PictureBox();
            this.pbImport = new System.Windows.Forms.PictureBox();
            this.pbExport = new System.Windows.Forms.PictureBox();
            this.pbAnalysis = new System.Windows.Forms.PictureBox();
            this.pbDelete = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblStats = new System.Windows.Forms.Label();
            this.btnAddTrip = new System.Windows.Forms.Button();
            this.cbEndStop = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbStartStop = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbDirection = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbRoutes = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panelUser = new System.Windows.Forms.Panel();
            this.lblUserName = new System.Windows.Forms.Label();
            this.pbAvatar = new System.Windows.Forms.PictureBox();
            this.labelXY = new System.Windows.Forms.Label();
            this.panelMapTools = new System.Windows.Forms.Panel();
            this.bFullExtent = new System.Windows.Forms.Button();
            this.bZoomOut = new System.Windows.Forms.Button();
            this.bZoomIn = new System.Windows.Forms.Button();
            this.bPan = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbAddUser)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAddArchive)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbImport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbExport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAnalysis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDelete)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.panelUser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbAvatar)).BeginInit();
            this.panelMapTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.splitContainer1.Panel1.Controls.Add(this.tvProfiles);
            this.splitContainer1.Panel1.Controls.Add(this.panelToolbar);
            this.splitContainer1.Panel1.Controls.Add(this.label5);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            this.splitContainer1.Panel1.Controls.Add(this.panelUser);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Panel2.Controls.Add(this.labelXY);
            this.splitContainer1.Panel2.Controls.Add(this.panelMapTools);
            this.splitContainer1.Panel2.SizeChanged += new System.EventHandler(this.MapPanel_SizeChanged);
            this.splitContainer1.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.MapPanel_Paint);
            this.splitContainer1.Panel2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MapPanel_MouseDown);
            this.splitContainer1.Panel2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MapPanel_MouseMove);
            this.splitContainer1.Panel2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MapPanel_MouseUp);
            this.splitContainer1.Panel2.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.MapPanel_MouseWheel);
            this.splitContainer1.Size = new System.Drawing.Size(1445, 826);
            this.splitContainer1.SplitterDistance = 300;
            this.splitContainer1.TabIndex = 0;
            // 
            // tvProfiles
            // 
            this.tvProfiles.BackColor = System.Drawing.Color.White;
            this.tvProfiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvProfiles.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tvProfiles.FullRowSelect = true;
            this.tvProfiles.HideSelection = false;
            this.tvProfiles.ItemHeight = 24;
            this.tvProfiles.Location = new System.Drawing.Point(13, 543);
            this.tvProfiles.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tvProfiles.Name = "tvProfiles";
            this.tvProfiles.ShowLines = false;
            this.tvProfiles.Size = new System.Drawing.Size(274, 221);
            this.tvProfiles.TabIndex = 3;
            // 
            // panelToolbar
            // 
            this.panelToolbar.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelToolbar.Controls.Add(this.pbAddUser);
            this.panelToolbar.Controls.Add(this.pbAddArchive);
            this.panelToolbar.Controls.Add(this.pbImport);
            this.panelToolbar.Controls.Add(this.pbExport);
            this.panelToolbar.Controls.Add(this.pbAnalysis);
            this.panelToolbar.Controls.Add(this.pbDelete);
            this.panelToolbar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelToolbar.Location = new System.Drawing.Point(13, 764);
            this.panelToolbar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelToolbar.Name = "panelToolbar";
            this.panelToolbar.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.panelToolbar.Size = new System.Drawing.Size(274, 50);
            this.panelToolbar.TabIndex = 5;
            // 
            // pbAddUser
            // 
            this.pbAddUser.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbAddUser.Location = new System.Drawing.Point(11, 10);
            this.pbAddUser.Margin = new System.Windows.Forms.Padding(4, 4, 13, 4);
            this.pbAddUser.Name = "pbAddUser";
            this.pbAddUser.Size = new System.Drawing.Size(32, 30);
            this.pbAddUser.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbAddUser.TabIndex = 0;
            this.pbAddUser.TabStop = false;
            this.toolTip1.SetToolTip(this.pbAddUser, "新建用户");
            // 
            // pbAddArchive
            // 
            this.pbAddArchive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbAddArchive.Location = new System.Drawing.Point(60, 10);
            this.pbAddArchive.Margin = new System.Windows.Forms.Padding(4, 4, 13, 4);
            this.pbAddArchive.Name = "pbAddArchive";
            this.pbAddArchive.Size = new System.Drawing.Size(32, 30);
            this.pbAddArchive.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbAddArchive.TabIndex = 1;
            this.pbAddArchive.TabStop = false;
            this.toolTip1.SetToolTip(this.pbAddArchive, "新建档案");
            // 
            // pbImport
            // 
            this.pbImport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbImport.Location = new System.Drawing.Point(109, 10);
            this.pbImport.Margin = new System.Windows.Forms.Padding(4, 4, 13, 4);
            this.pbImport.Name = "pbImport";
            this.pbImport.Size = new System.Drawing.Size(32, 30);
            this.pbImport.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbImport.TabIndex = 2;
            this.pbImport.TabStop = false;
            this.toolTip1.SetToolTip(this.pbImport, "导入行程 (.trj)");
            // 
            // pbExport
            // 
            this.pbExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbExport.Location = new System.Drawing.Point(158, 10);
            this.pbExport.Margin = new System.Windows.Forms.Padding(4, 4, 13, 4);
            this.pbExport.Name = "pbExport";
            this.pbExport.Size = new System.Drawing.Size(32, 30);
            this.pbExport.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbExport.TabIndex = 3;
            this.pbExport.TabStop = false;
            this.toolTip1.SetToolTip(this.pbExport, "导出行程 (.trj)");
            // 
            // pbAnalysis
            // 
            this.pbAnalysis.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbAnalysis.Location = new System.Drawing.Point(207, 10);
            this.pbAnalysis.Margin = new System.Windows.Forms.Padding(4, 4, 13, 4);
            this.pbAnalysis.Name = "pbAnalysis";
            this.pbAnalysis.Size = new System.Drawing.Size(32, 30);
            this.pbAnalysis.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbAnalysis.TabIndex = 4;
            this.pbAnalysis.TabStop = false;
            this.toolTip1.SetToolTip(this.pbAnalysis, "开启/关闭 统计分析");
            // 
            // pbDelete
            // 
            this.pbDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbDelete.Location = new System.Drawing.Point(11, 48);
            this.pbDelete.Margin = new System.Windows.Forms.Padding(4, 4, 13, 4);
            this.pbDelete.Name = "pbDelete";
            this.pbDelete.Size = new System.Drawing.Size(32, 30);
            this.pbDelete.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbDelete.TabIndex = 5;
            this.pbDelete.TabStop = false;
            this.toolTip1.SetToolTip(this.pbDelete, "删除选中项");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Top;
            this.label5.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label5.Location = new System.Drawing.Point(13, 512);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label5.Size = new System.Drawing.Size(69, 31);
            this.label5.TabIndex = 2;
            this.label5.Text = "我的档案";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblStats);
            this.groupBox1.Controls.Add(this.btnAddTrip);
            this.groupBox1.Controls.Add(this.cbEndStop);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.cbStartStop);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.cbDirection);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cbRoutes);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox1.Location = new System.Drawing.Point(13, 112);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(274, 400);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "新建行程";
            // 
            // lblStats
            // 
            this.lblStats.AutoSize = true;
            this.lblStats.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblStats.ForeColor = System.Drawing.Color.DimGray;
            this.lblStats.Location = new System.Drawing.Point(20, 362);
            this.lblStats.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStats.Name = "lblStats";
            this.lblStats.Size = new System.Drawing.Size(81, 20);
            this.lblStats.TabIndex = 9;
            this.lblStats.Text = "等待计算...";
            // 
            // btnAddTrip
            // 
            this.btnAddTrip.BackColor = System.Drawing.Color.SteelBlue;
            this.btnAddTrip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddTrip.ForeColor = System.Drawing.Color.White;
            this.btnAddTrip.Location = new System.Drawing.Point(20, 306);
            this.btnAddTrip.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddTrip.Name = "btnAddTrip";
            this.btnAddTrip.Size = new System.Drawing.Size(333, 44);
            this.btnAddTrip.TabIndex = 8;
            this.btnAddTrip.Text = "生成轨迹并分析";
            this.btnAddTrip.UseVisualStyleBackColor = false;
            this.btnAddTrip.Click += new System.EventHandler(this.btnAddTrip_Click);
            // 
            // cbEndStop
            // 
            this.cbEndStop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbEndStop.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbEndStop.FormattingEnabled = true;
            this.cbEndStop.Location = new System.Drawing.Point(20, 256);
            this.cbEndStop.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbEndStop.Name = "cbEndStop";
            this.cbEndStop.Size = new System.Drawing.Size(332, 28);
            this.cbEndStop.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(20, 231);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 20);
            this.label4.TabIndex = 6;
            this.label4.Text = "下车站";
            // 
            // cbStartStop
            // 
            this.cbStartStop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStartStop.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbStartStop.FormattingEnabled = true;
            this.cbStartStop.Location = new System.Drawing.Point(20, 188);
            this.cbStartStop.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbStartStop.Name = "cbStartStop";
            this.cbStartStop.Size = new System.Drawing.Size(332, 28);
            this.cbStartStop.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(20, 162);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 20);
            this.label3.TabIndex = 4;
            this.label3.Text = "上车站";
            // 
            // cbDirection
            // 
            this.cbDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDirection.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbDirection.FormattingEnabled = true;
            this.cbDirection.Location = new System.Drawing.Point(20, 119);
            this.cbDirection.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbDirection.Name = "cbDirection";
            this.cbDirection.Size = new System.Drawing.Size(332, 28);
            this.cbDirection.TabIndex = 3;
            this.cbDirection.SelectedIndexChanged += new System.EventHandler(this.cbDirection_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(20, 94);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "方向";
            // 
            // cbRoutes
            // 
            this.cbRoutes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRoutes.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbRoutes.FormattingEnabled = true;
            this.cbRoutes.Location = new System.Drawing.Point(20, 50);
            this.cbRoutes.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbRoutes.Name = "cbRoutes";
            this.cbRoutes.Size = new System.Drawing.Size(332, 28);
            this.cbRoutes.TabIndex = 1;
            this.cbRoutes.SelectedIndexChanged += new System.EventHandler(this.cbRoutes_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(20, 25);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "选择线路";
            // 
            // panelUser
            // 
            this.panelUser.Controls.Add(this.lblUserName);
            this.panelUser.Controls.Add(this.pbAvatar);
            this.panelUser.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelUser.Location = new System.Drawing.Point(13, 12);
            this.panelUser.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelUser.Name = "panelUser";
            this.panelUser.Size = new System.Drawing.Size(274, 100);
            this.panelUser.TabIndex = 4;
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblUserName.Location = new System.Drawing.Point(107, 31);
            this.lblUserName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(92, 27);
            this.lblUserName.TabIndex = 1;
            this.lblUserName.Text = "默认用户";
            // 
            // pbAvatar
            // 
            this.pbAvatar.BackColor = System.Drawing.Color.White;
            this.pbAvatar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbAvatar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbAvatar.Location = new System.Drawing.Point(7, 12);
            this.pbAvatar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pbAvatar.Name = "pbAvatar";
            this.pbAvatar.Size = new System.Drawing.Size(79, 74);
            this.pbAvatar.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbAvatar.TabIndex = 0;
            this.pbAvatar.TabStop = false;
            this.pbAvatar.Click += new System.EventHandler(this.PbAvatar_Click);
            // 
            // labelXY
            // 
            this.labelXY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelXY.AutoSize = true;
            this.labelXY.BackColor = System.Drawing.Color.Transparent;
            this.labelXY.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelXY.Location = new System.Drawing.Point(901, 788);
            this.labelXY.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelXY.Name = "labelXY";
            this.labelXY.Size = new System.Drawing.Size(112, 18);
            this.labelXY.TabIndex = 1;
            this.labelXY.Text = "X: 0.0, Y:0.0";
            // 
            // panelMapTools
            // 
            this.panelMapTools.BackColor = System.Drawing.Color.Transparent;
            this.panelMapTools.Controls.Add(this.bFullExtent);
            this.panelMapTools.Controls.Add(this.bZoomOut);
            this.panelMapTools.Controls.Add(this.bZoomIn);
            this.panelMapTools.Controls.Add(this.bPan);
            this.panelMapTools.Location = new System.Drawing.Point(27, 25);
            this.panelMapTools.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelMapTools.Name = "panelMapTools";
            this.panelMapTools.Size = new System.Drawing.Size(333, 50);
            this.panelMapTools.TabIndex = 0;
            // 
            // bFullExtent
            // 
            this.bFullExtent.BackColor = System.Drawing.Color.White;
            this.bFullExtent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bFullExtent.Location = new System.Drawing.Point(220, 4);
            this.bFullExtent.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bFullExtent.Name = "bFullExtent";
            this.bFullExtent.Size = new System.Drawing.Size(67, 38);
            this.bFullExtent.TabIndex = 3;
            this.bFullExtent.Text = "全图";
            this.bFullExtent.UseVisualStyleBackColor = false;
            this.bFullExtent.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // bZoomOut
            // 
            this.bZoomOut.BackColor = System.Drawing.Color.White;
            this.bZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bZoomOut.Location = new System.Drawing.Point(79, 4);
            this.bZoomOut.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bZoomOut.Name = "bZoomOut";
            this.bZoomOut.Size = new System.Drawing.Size(67, 38);
            this.bZoomOut.TabIndex = 1;
            this.bZoomOut.Text = "缩小";
            this.bZoomOut.UseVisualStyleBackColor = false;
            this.bZoomOut.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // bZoomIn
            // 
            this.bZoomIn.BackColor = System.Drawing.Color.White;
            this.bZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bZoomIn.Location = new System.Drawing.Point(4, 4);
            this.bZoomIn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bZoomIn.Name = "bZoomIn";
            this.bZoomIn.Size = new System.Drawing.Size(67, 38);
            this.bZoomIn.TabIndex = 0;
            this.bZoomIn.Text = "放大";
            this.bZoomIn.UseVisualStyleBackColor = false;
            this.bZoomIn.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // bPan
            // 
            this.bPan.BackColor = System.Drawing.Color.White;
            this.bPan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bPan.Location = new System.Drawing.Point(149, 4);
            this.bPan.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bPan.Name = "bPan";
            this.bPan.Size = new System.Drawing.Size(67, 38);
            this.bPan.TabIndex = 2;
            this.bPan.Text = "漫游";
            this.bPan.UseVisualStyleBackColor = false;
            this.bPan.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // FormMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1445, 826);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FormMap";
            this.Text = "TransitLog - \'交\'游手账";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panelToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbAddUser)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAddArchive)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbImport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbExport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAnalysis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDelete)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panelUser.ResumeLayout(false);
            this.panelUser.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbAvatar)).EndInit();
            this.panelMapTools.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbRoutes;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbDirection;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbStartStop;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbEndStop;
        private System.Windows.Forms.Button btnAddTrip;
        private System.Windows.Forms.TreeView tvProfiles;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel panelMapTools;
        private System.Windows.Forms.Button bZoomIn;
        private System.Windows.Forms.Button bZoomOut;
        private System.Windows.Forms.Button bPan;
        private System.Windows.Forms.Button bFullExtent;
        private System.Windows.Forms.Label labelXY;
        private System.Windows.Forms.Label lblStats;
        private System.Windows.Forms.Panel panelUser;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.PictureBox pbAvatar;
        // 新增的底部工具栏相关控件
        private System.Windows.Forms.FlowLayoutPanel panelToolbar;
        private System.Windows.Forms.PictureBox pbAddUser;
        private System.Windows.Forms.PictureBox pbAddArchive;
        private System.Windows.Forms.PictureBox pbImport;
        private System.Windows.Forms.PictureBox pbExport;
        private System.Windows.Forms.PictureBox pbAnalysis;
        private System.Windows.Forms.PictureBox pbDelete;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}