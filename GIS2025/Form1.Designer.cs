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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
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
            this.lstTrips = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.panelMapTools = new System.Windows.Forms.Panel();
            this.bFullExtent = new System.Windows.Forms.Button();
            this.bZoomOut = new System.Windows.Forms.Button();
            this.bZoomIn = new System.Windows.Forms.Button();
            this.bPan = new System.Windows.Forms.Button();
            this.labelXY = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panelMapTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            this.splitContainer1.Panel1.Controls.Add(this.lstTrips);
            this.splitContainer1.Panel1.Controls.Add(this.label5);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(10);
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
            this.splitContainer1.Size = new System.Drawing.Size(1084, 661);
            this.splitContainer1.SplitterDistance = 300;
            this.splitContainer1.TabIndex = 0;
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
            this.groupBox1.Location = new System.Drawing.Point(10, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(280, 320);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "新建行程";
            // 
            // lblStats
            // 
            this.lblStats.AutoSize = true;
            this.lblStats.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblStats.ForeColor = System.Drawing.Color.DimGray;
            this.lblStats.Location = new System.Drawing.Point(15, 290);
            this.lblStats.Name = "lblStats";
            this.lblStats.Size = new System.Drawing.Size(104, 17);
            this.lblStats.TabIndex = 9;
            this.lblStats.Text = "等待计算...";
            // 
            // btnAddTrip
            // 
            this.btnAddTrip.BackColor = System.Drawing.Color.SteelBlue;
            this.btnAddTrip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddTrip.ForeColor = System.Drawing.Color.White;
            this.btnAddTrip.Location = new System.Drawing.Point(15, 245);
            this.btnAddTrip.Name = "btnAddTrip";
            this.btnAddTrip.Size = new System.Drawing.Size(250, 35);
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
            this.cbEndStop.Location = new System.Drawing.Point(15, 205);
            this.cbEndStop.Name = "cbEndStop";
            this.cbEndStop.Size = new System.Drawing.Size(250, 25);
            this.cbEndStop.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(15, 185);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 17);
            this.label4.TabIndex = 6;
            this.label4.Text = "下车站";
            // 
            // cbStartStop
            // 
            this.cbStartStop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStartStop.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbStartStop.FormattingEnabled = true;
            this.cbStartStop.Location = new System.Drawing.Point(15, 150);
            this.cbStartStop.Name = "cbStartStop";
            this.cbStartStop.Size = new System.Drawing.Size(250, 25);
            this.cbStartStop.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(15, 130);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "上车站";
            // 
            // cbDirection
            // 
            this.cbDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDirection.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbDirection.FormattingEnabled = true;
            this.cbDirection.Location = new System.Drawing.Point(15, 95);
            this.cbDirection.Name = "cbDirection";
            this.cbDirection.Size = new System.Drawing.Size(250, 25);
            this.cbDirection.TabIndex = 3;
            this.cbDirection.SelectedIndexChanged += new System.EventHandler(this.cbDirection_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(15, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "方向";
            // 
            // cbRoutes
            // 
            this.cbRoutes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRoutes.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbRoutes.FormattingEnabled = true;
            this.cbRoutes.Location = new System.Drawing.Point(15, 40);
            this.cbRoutes.Name = "cbRoutes";
            this.cbRoutes.Size = new System.Drawing.Size(250, 25);
            this.cbRoutes.TabIndex = 1;
            this.cbRoutes.SelectedIndexChanged += new System.EventHandler(this.cbRoutes_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(15, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "选择线路";
            // 
            // lstTrips
            // 
            this.lstTrips.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lstTrips.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lstTrips.FormattingEnabled = true;
            this.lstTrips.ItemHeight = 17;
            this.lstTrips.Location = new System.Drawing.Point(10, 362);
            this.lstTrips.Name = "lstTrips";
            this.lstTrips.Size = new System.Drawing.Size(280, 289);
            this.lstTrips.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(10, 340);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 17);
            this.label5.TabIndex = 2;
            this.label5.Text = "行程记录";
            // 
            // panelMapTools
            // 
            this.panelMapTools.BackColor = System.Drawing.Color.Transparent;
            this.panelMapTools.Controls.Add(this.bFullExtent);
            this.panelMapTools.Controls.Add(this.bZoomOut);
            this.panelMapTools.Controls.Add(this.bZoomIn);
            this.panelMapTools.Controls.Add(this.bPan);
            this.panelMapTools.Location = new System.Drawing.Point(20, 20);
            this.panelMapTools.Name = "panelMapTools";
            this.panelMapTools.Size = new System.Drawing.Size(250, 40);
            this.panelMapTools.TabIndex = 0;
            // 
            // bFullExtent
            // 
            this.bFullExtent.BackColor = System.Drawing.Color.White;
            this.bFullExtent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bFullExtent.Location = new System.Drawing.Point(165, 3);
            this.bFullExtent.Name = "bFullExtent";
            this.bFullExtent.Size = new System.Drawing.Size(50, 30);
            this.bFullExtent.TabIndex = 3;
            this.bFullExtent.Text = "全图";
            this.bFullExtent.UseVisualStyleBackColor = false;
            this.bFullExtent.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // bZoomOut
            // 
            this.bZoomOut.BackColor = System.Drawing.Color.White;
            this.bZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bZoomOut.Location = new System.Drawing.Point(59, 3);
            this.bZoomOut.Name = "bZoomOut";
            this.bZoomOut.Size = new System.Drawing.Size(50, 30);
            this.bZoomOut.TabIndex = 1;
            this.bZoomOut.Text = "缩小";
            this.bZoomOut.UseVisualStyleBackColor = false;
            this.bZoomOut.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // bZoomIn
            // 
            this.bZoomIn.BackColor = System.Drawing.Color.White;
            this.bZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bZoomIn.Location = new System.Drawing.Point(3, 3);
            this.bZoomIn.Name = "bZoomIn";
            this.bZoomIn.Size = new System.Drawing.Size(50, 30);
            this.bZoomIn.TabIndex = 0;
            this.bZoomIn.Text = "放大";
            this.bZoomIn.UseVisualStyleBackColor = false;
            this.bZoomIn.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // bPan
            // 
            this.bPan.BackColor = System.Drawing.Color.White;
            this.bPan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bPan.Location = new System.Drawing.Point(112, 3);
            this.bPan.Name = "bPan";
            this.bPan.Size = new System.Drawing.Size(50, 30);
            this.bPan.TabIndex = 2;
            this.bPan.Text = "漫游";
            this.bPan.UseVisualStyleBackColor = false;
            this.bPan.Click += new System.EventHandler(this.MapTools_Click);
            // 
            // labelXY
            // 
            this.labelXY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelXY.AutoSize = true;
            this.labelXY.BackColor = System.Drawing.Color.Transparent;
            this.labelXY.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelXY.Location = new System.Drawing.Point(600, 630);
            this.labelXY.Name = "labelXY";
            this.labelXY.Size = new System.Drawing.Size(84, 14);
            this.labelXY.TabIndex = 1;
            this.labelXY.Text = "X: 0.0, Y:0.0";
            // 
            // FormMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 661);
            this.Controls.Add(this.splitContainer1);
            this.Name = "FormMap";
            this.Text = "TransitLog - '交'游手账";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
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
        private System.Windows.Forms.ListBox lstTrips;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel panelMapTools;
        private System.Windows.Forms.Button bZoomIn;
        private System.Windows.Forms.Button bZoomOut;
        private System.Windows.Forms.Button bPan;
        private System.Windows.Forms.Button bFullExtent;
        private System.Windows.Forms.Label labelXY;
        private System.Windows.Forms.Label lblStats;
    }
}