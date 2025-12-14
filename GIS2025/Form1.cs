using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using XGIS;

namespace GIS2025
{
    public partial class FormMap : Form
    {
        XView view = null;
        Bitmap backwindow;
        XVectorLayer districtLayer;
        BusDataManager _dataManager;
        JourneyCalculator _calculator;
        XWebTileLayer tiandituLayer;
        System.Windows.Forms.Timer refreshTimer;
        bool _isHeatmapEnabled = false; // 开关状态

        Dictionary<string, int> _heatmapStats = null; // 统计结果缓存 (行政区名 -> 次数)

        // 交互状态
        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;

        // UI 控件
        PictureBox pbLoading;
        private FrmStatsReport _currentReportForm = null; 

        public FormMap()
        {
            InitializeComponent();
            DoubleBuffered = true;
            // 双缓冲设置保持不变...
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, splitContainer1.Panel2, new object[] { true });

            string myKey = "52e398187b19acc51fec54eb09f085c1";
            tiandituLayer = new XWebTileLayer(myKey);

            _dataManager = new BusDataManager();
            _calculator = new JourneyCalculator(_dataManager);

            LoadBasemap();
            LoadBusData();
            InitRouteSearch();
            InitLoadingControl();
            InitStatusLabels();

            if (ProfileManager.Instance.CurrentUser != null)
            {
                ProfileManager.Instance.RestoreGeometries(_calculator);
            }

            InitProfileUI();

            tvProfiles.AllowDrop = true;
            tvProfiles.ItemDrag += TvProfiles_ItemDrag;
            tvProfiles.DragEnter += TvProfiles_DragEnter;
            tvProfiles.DragOver += TvProfiles_DragOver;
            tvProfiles.DragDrop += TvProfiles_DragDrop;

            // Timer 设置保持不变...
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 800;
            refreshTimer.Tick += (s, e) => { if (currentMouseAction == XExploreActions.noaction) UpdateMap(); };
            refreshTimer.Start();

            UpdateMap();
        }

        // 【新增】初始化标签的方法
        private void InitStatusLabels()
        {
            // 不再需要 new Label，也不需要 calculate location
            // 直接给设计器里生成的控件设置初始值
            toolStripStatusLabel1.Text = "暂无统计对象";
            toolStripStatusLabel2.Text = "准备就绪";
            toolStripStatusLabel3.Text = "";
        }

        // ==========================================
        // 【新增】 档案系统 UI 初始化
        // ==========================================
        private void InitProfileUI()
        {
            // 1. 设置头像框
            pbAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbAvatar.Cursor = Cursors.Default;
            //pbAvatar.Click += PbAvatar_Click;
            pbAvatar.BorderStyle = BorderStyle.FixedSingle;

            // 2. 设置 TreeView
            tvProfiles.HideSelection = false;
            // tvProfiles.ContextMenuStrip = cmsProfile; // 【移除】不再需要右键菜单
            tvProfiles.AfterSelect += (s, e) => UpdateMap();
            // tvProfiles.NodeMouseClick += TvProfiles_NodeMouseClick; // 【移除】不需要右键选中了

            // 3. 【新增】初始化底部工具栏
            InitBottomToolbar();

            RefreshTree();

            // 5. 自动选中
            /*var lastUser = ProfileManager.Instance.Users.LastOrDefault();
            if (lastUser != null)
            {
                object target = lastUser;
                var lastArchive = lastUser.Archives.LastOrDefault();
                if (lastArchive != null)
                {
                    target = lastArchive;
                    var lastTrip = lastArchive.Trips.LastOrDefault();
                    if (lastTrip != null) target = lastTrip;
                }
                SelectNodeByTag(target);
            }
            */
            if (ProfileManager.Instance.CurrentUser != null)
            {
                SelectNodeByTag(ProfileManager.Instance.CurrentUser);
            }
        }

        private void InitBottomToolbar()
        {
            // 辅助函数：更智能的图片加载
            void LoadIcon(PictureBox pb, string fileName)
            {
                // 1. 强制设置显示模式为缩放，防止图片太大只显示一部分导致“隐形”
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.BackColor = Color.Transparent;

                // 2. 尝试寻找路径 (兼容 Debug 模式和发布模式)
                // 优先找 bin/Debug/data/pic/icon
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "icon", fileName);

                // 如果找不到，尝试去项目源码目录找 (防止你忘了复制 data 文件夹)
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\data\pic\icon", fileName);
                }

                if (File.Exists(path))
                {
                    pb.Image = Image.FromFile(path);
                }
                else
                {
                    // 如果真的找不到，给一个显眼的颜色提示
                    pb.BackColor = Color.Red;
                }
            }

            // 加载图片
            LoadIcon(pbAddUser, "add_user.png");
            LoadIcon(pbAddArchive, "add_route.png");
            LoadIcon(pbImport, "load.png");
            LoadIcon(pbExport, "save.png");
            LoadIcon(pbAnalysis, "anal.png");
            LoadIcon(pbDelete, "delete.png");
            LoadIcon(pbRename, "edit.png");
            LoadIcon(pbOpenRaw, "file.png");

            // 绑定事件 (确保这些逻辑还在)
            // 注意：这里用 SwitchUser() 替换了原来的 CreateNewUser()
            pbAddUser.Click -= null; // 防止重复绑定
            pbAddUser.Click += (s, e) => SwitchUser();

            pbAddArchive.Click += (s, e) => CreateNewArchive();
            pbImport.Click += (s, e) => ImportTrj_Click();
            pbExport.Click += (s, e) => ExportTrj_Click();
            pbDelete.Click += (s, e) => DeleteCurrentNode();
            pbRename.Click += (s, e) => RenameArchive_Click();
            pbOpenRaw.Click += (s, e) => OpenRawData_Click();
            toolTip1.SetToolTip(pbRename, "重命名档案");
            toolTip1.SetToolTip(pbOpenRaw, "查看原始文件(.trj)");
            pbAnalysis.Click += ToggleHeatmap_Click;
        }

        private void ToggleHeatmap_Click(object sender, EventArgs e)
        {
            if (_isHeatmapEnabled)
            {
                // 关闭功能
                _isHeatmapEnabled = false;
                _heatmapStats = null;

                // 【UI 反馈】 按钮背景恢复透明
                pbAnalysis.BackColor = Color.Transparent;
                pbAnalysis.BorderStyle = BorderStyle.None;
                toolStripStatusLabel1.Text = "";
            }
            else
            {
                // 开启功能
                if (tvProfiles.SelectedNode == null)
                {
                    FrmActionBox.Show("请先选择一个档案或用户！", ActionType.Error);
                    return;
                }

                // 1. 计算去过的数据 (字典: 区域名 -> 站点数)
                _heatmapStats = CalculateStatsForNode(tvProfiles.SelectedNode.Tag);

                // 2. 开启地图渲染
                _isHeatmapEnabled = true;

                // 【UI 反馈】 按钮背景变色
                pbAnalysis.BackColor = Color.LightSkyBlue;
                pbAnalysis.BorderStyle = BorderStyle.FixedSingle;

                // 【新增】显示提示标签
                toolStripStatusLabel1.Text = $"当前统计对象: {tvProfiles.SelectedNode.Text}";

                // ==========================================
                // ★★★ 新增：覆盖率统计与弹窗报告 ★★★
                // ==========================================
                ShowCoverageReport(_heatmapStats);
            }
            // 刷新地图
            UpdateMap();
        }

        // 辅助方法：生成并展示统计报告
        private void ShowCoverageReport(Dictionary<string, int> visitedStats)
        {
            if (districtLayer == null || districtLayer.FeatureCount() == 0)
            {
                FrmActionBox.Show("未加载地图图层，无法计算覆盖率。", ActionType.Error);
                return;
            }

            // 1. 获取所有乡镇单元的总列表
            List<string> allRegions = new List<string>();
            for (int i = 0; i < districtLayer.FeatureCount(); i++)
            {
                string name = districtLayer.GetFeature(i).getAttribute(0).ToString().Trim();
                if (!allRegions.Contains(name))
                {
                    allRegions.Add(name);
                }
            }

            // 2. ★关键修改1：检查窗口是否已打开
            if (_currentReportForm != null && !_currentReportForm.IsDisposed)
            {
                // 如果已经打开了，就把它带到前台，不重新创建
                _currentReportForm.BringToFront();
                return;
            }

            // 3. 创建新窗口并显示
            // 注意：这里不需要再手动拼装 StringBuilder 文本了，直接把原始数据传给新窗口即可
            _currentReportForm = new FrmStatsReport(visitedStats, allRegions);

            // ★关键修改1：使用 Show() 实现非模态，允许与主程序交互
            _currentReportForm.Show(this);
        }

        // 刷新 TreeView 显示
        private void RefreshTree()
        {
            tvProfiles.Nodes.Clear();

            // ==========================================
            // 【核心修改】只显示当前用户
            // ==========================================
            var currentUser = ProfileManager.Instance.CurrentUser;
            if (currentUser == null) return;

            // 1级节点：当前用户
            TreeNode userNode = new TreeNode(currentUser.Name);
            userNode.Tag = currentUser;
            userNode.ImageKey = "user";

            // 显示该用户下的所有档案
            foreach (var archive in currentUser.Archives)
            {
                // 2级节点：档案
                TreeNode archiveNode = new TreeNode(archive.Name);
                archiveNode.Tag = archive;

                foreach (var trip in archive.Trips)
                {
                    // 3级节点：行程
                    TreeNode tripNode = new TreeNode($"{trip.RouteName} ({trip.StartStop}-{trip.EndStop})");
                    tripNode.Tag = trip;
                    archiveNode.Nodes.Add(tripNode);
                }
                userNode.Nodes.Add(archiveNode);
            }
            tvProfiles.Nodes.Add(userNode);
            tvProfiles.ExpandAll();
        }

        // 【新增】根据数据对象自动选中对应的树节点
        private void SelectNodeByTag(object tag)
        {
            if (tag == null) return;

            // 遍历所有节点寻找目标
            foreach (TreeNode userNode in tvProfiles.Nodes)
            {
                if (userNode.Tag == tag)
                {
                    tvProfiles.SelectedNode = userNode;
                    userNode.EnsureVisible(); // 确保滚动到可见区域
                    return;
                }
                foreach (TreeNode archiveNode in userNode.Nodes)
                {
                    if (archiveNode.Tag == tag)
                    {
                        tvProfiles.SelectedNode = archiveNode;
                        archiveNode.EnsureVisible();
                        return;
                    }
                    foreach (TreeNode tripNode in archiveNode.Nodes)
                    {
                        if (tripNode.Tag == tag)
                        {
                            tvProfiles.SelectedNode = tripNode;
                            tripNode.EnsureVisible();
                            return;
                        }
                    }
                }
            }
        }


        // ==========================================
        // 【新增】 档案系统交互逻辑
        // ==========================================

        // 点击头像更换图片
        /*
        private void PbAvatar_Click(object sender, EventArgs e)
        {
            // 获取当前选中的用户
            UserProfile currentUser = GetSelectedUser();
            if (currentUser == null) { FrmActionBox.Show("请先选择一个档案或用户！", ActionType.Error); return; }

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "图片文件|*.jpg;*.png;*.bmp";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // 复制图片到 app 目录，防止源文件被删
                string destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "avatars");
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                string destPath = Path.Combine(destDir, Guid.NewGuid().ToString() + Path.GetExtension(dlg.FileName));
                File.Copy(dlg.FileName, destPath, true);

                currentUser.AvatarPath = destPath;
                pbAvatar.Image = Image.FromFile(destPath);
                ProfileManager.Instance.SaveCurrentUser();
            }
        }
        */
        // 【新增】切换用户
        private void SwitchUser()
        {
            DialogResult result = FrmActionBox.Show("确定要退出当前用户并切换账号吗？", ActionType.Confirm);
            if (result == DialogResult.Yes)
            {
                // 最简单的办法：重启程序，这样会重新进入 FrmUserSelect 界面
                Application.Restart();
            }
        }


        /*
        // 右键点击节点时自动选中它
        private void TvProfiles_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right) tvProfiles.SelectedNode = e.Node;
        }
        */

        // 【重写】创建新档案 (改为直接生成 .trj 文件)
        private void CreateNewArchive()
        {
            // 不需要先选用户了，因为只能给 CurrentUser 建档案
            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入档案名称:", "新建档案", DateTime.Now.ToString("yyyy-MM-dd") + " 出游");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newArchive = new DailyArchive(name);

                // 1. 加入内存列表
                ProfileManager.Instance.CurrentUser.Archives.Add(newArchive);

                // 2. 【关键】立即保存为 .trj 文件
                ProfileManager.Instance.SaveArchive(newArchive);

                RefreshTree();
                SelectNodeByTag(newArchive); // 自动选中
                FrmActionBox.Show("创建成功", ActionType.Success);
            }
        }

        // 【重写】删除逻辑 (适配文件删除)
        private void DeleteCurrentNode()
        {
            TreeNode node = tvProfiles.SelectedNode;
            if (node == null) return;
            if (node.Tag is UserProfile)
            {
                FrmActionBox.Show("无法删除当前登录的用户，请去登录界面删除", ActionType.Error);
                return;
            }
            DialogResult result = FrmActionBox.Show($"确定要删除 '{node.Text}' 吗？此操作不可恢复。", ActionType.Confirm);
            if (result == DialogResult.Yes)
            {
                
                if (node.Tag is DailyArchive archive)
                {
                    // 删除档案文件
                    ProfileManager.Instance.DeleteArchive(archive);
                    FrmActionBox.Show("操作成功", ActionType.Success);
                }
                else if (node.Tag is TripArchiveItem trip)
                {
                    // 删除行程：先从父档案里移除，然后保存父档案
                    var parentArchive = node.Parent.Tag as DailyArchive;
                    if (parentArchive != null)
                    {
                        parentArchive.Trips.Remove(trip);
                        // 保存更新后的档案文件
                        ProfileManager.Instance.SaveArchive(parentArchive);
                    }
                    FrmActionBox.Show("操作成功", ActionType.Success);
                }

                RefreshTree();
                UpdateMap();
            }
        }

        // ==========================================
        // .trj 导入导出交互逻辑
        // ==========================================

        // 【重写】导入 .trj
        private void ImportTrj_Click()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "公交行程文件 (*.trj)|*.trj";
            dlg.Title = "导入公交行程文件";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 1. 构造目标路径：user/当前用户/文件名.trj
                    string userDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", ProfileManager.Instance.CurrentUser.Name);
                    string destPath = Path.Combine(userDir, Path.GetFileName(dlg.FileName));

                    // 2. 复制文件 (如果重名则覆盖)
                    File.Copy(dlg.FileName, destPath, true);

                    // 3. 重新加载该用户的档案 (为了省事，直接调用 LoginUser 重新读一遍目录)
                    // 这一步会把新复制进来的文件读到内存 Archives 列表里
                    ProfileManager.Instance.LoginUser(ProfileManager.Instance.CurrentUser.Name);

                    // 4. 复活几何 (因为刚读进来只有文本)
                    ProfileManager.Instance.RestoreGeometries(_calculator);

                    RefreshTree();
                    FrmActionBox.Show("导入成功！", ActionType.Success);
                }
                catch (Exception ex)
                {
                    FrmActionBox.Show("导入失败: " + ex.Message, ActionType.Error);
                }
            }
        }

        // 【重写】导出 .trj
        private void ExportTrj_Click()
        {
            DailyArchive targetArchive = GetSelectedArchive();
            if (targetArchive == null)
            {
                FrmActionBox.Show("请先选中一个【档案】进行导出！", ActionType.Error);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "公交行程文件 (*.trj)|*.trj";
            dlg.FileName = targetArchive.Name + ".trj";
            dlg.Title = "分享我的行程";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 1. 找到源文件路径
                    string userDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", ProfileManager.Instance.CurrentUser.Name);
                    // 这里假设文件名和档案名一致（在SaveArchive里处理过），或者我们可以重新序列化一次到目标路径
                    // 为了保险起见，直接把内存里的对象序列化到目标路径最稳妥

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(targetArchive, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(dlg.FileName, json);

                    FrmActionBox.Show("导出成功！", ActionType.Success);
                }
                catch (Exception ex)
                {
                    FrmActionBox.Show("导出失败: " + ex.Message, ActionType.Error);
                }
            }
        }

        // 辅助方法：获取当前选中节点所属的用户对象
        private UserProfile GetSelectedUser()
        {
            TreeNode node = tvProfiles.SelectedNode;
            while (node != null)
            {
                if (node.Tag is UserProfile u) return u;
                node = node.Parent;
            }
            return null; // 没找到
        }

        // 辅助方法：获取当前选中节点所属的档案对象 (用于添加行程)
        private DailyArchive GetSelectedArchive()
        {
            TreeNode node = tvProfiles.SelectedNode;
            while (node != null)
            {
                if (node.Tag is DailyArchive a) return a;
                node = node.Parent;
            }
            return null;
        }

        // ==========================================
        // 生成轨迹 (核心逻辑修改：保存到档案)
        // ==========================================
        private async void btnAddTrip_Click(object sender, EventArgs e)
        {
            // 1. 基础校验 (保持不变)
            DailyArchive targetArchive = GetSelectedArchive();
            if (targetArchive == null) { FrmActionBox.Show("请先在左侧列表中选择一个【档案】...", ActionType.Error); return; }
            if (cbStartStop.SelectedItem == null || cbEndStop.SelectedItem == null) { FrmActionBox.Show("请选择完整的起止站点！", ActionType.Error); return; }

            // 2. 准备 UI 状态
            pbLoading.Visible = true;
            pbLoading.BringToFront();
            this.Refresh();
            btnAddTrip.Enabled = false; // 禁用按钮
            lblStats.Text = "正在规划路线...当这个状态一直持续，请重启程序...";
            refreshTimer.Stop();

            string route = cbRoutes.Text;
            string dir = cbDirection.SelectedItem.ToString();
            string start = cbStartStop.SelectedItem.ToString();
            string end = cbEndStop.SelectedItem.ToString();

            // 定义结果变量
            XLineSpatial tripGeometry = null;
            Dictionary<string, int> stats = null;
            string errorMessage = null; // 用于在 UI 线程显示错误

            try
            {
                // 3. 【核心修改】后台计算 (去除内部的空 try-catch)
                await Task.Run(() =>
                {
                    // 直接调用，如果有错误让它抛出来，被外层的 try-catch 捕获
                    // 注意：JourneyCalculator 里的 throw Exception 会在这里中断 Task
                    tripGeometry = _calculator.ReconstructTrip(route, dir, start, end);
                    stats = _calculator.AnalyzeDistrictsByLogic(route, dir, start, end);
                });

                // 4. 处理正常结果
                if (tripGeometry != null && stats != null)
                {
                    var newItem = new TripArchiveItem(route, dir, start, end, tripGeometry);
                    newItem.Length = tripGeometry.length;
                    targetArchive.Trips.Add(newItem);
                    ProfileManager.Instance.SaveArchive(targetArchive);

                    RefreshTree();
                    SelectNodeByTag(newItem);

                    // 更新统计文本
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"行程：{start} -> {end}");
                    sb.AppendLine($"里程：{newItem.Length:F2} km (共 {stats.Values.Sum()} 站)");
                    foreach (var kvp in stats) sb.Append($"{kvp.Key}: {kvp.Value}站 ");
                    lblStats.Text = sb.ToString();

                    FrmActionBox.Show("添加成功", ActionType.Success);
                }
                else
                {
                    // 如果没抛异常但返回了空 (比如降级逻辑也失败了)
                    errorMessage = "未能规划出有效路线，请检查数据。";
                }
            }
            catch (Exception ex)
            {
                // 5. 【增强鲁棒性】捕获所有后台异常
                errorMessage = "规划失败: " + ex.Message;
            }
            finally
            {
                // 6. 【关键】无论成功还是失败，必须恢复 UI 状态
                pbLoading.Visible = false;
                btnAddTrip.Enabled = true; // 复活按钮
                refreshTimer.Start();

                // 统一显示错误信息 (在 UI 线程弹窗是安全的)
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    lblStats.Text = "计算失败";
                    FrmActionBox.Show(errorMessage, ActionType.Error);
                }
            }
        }

        // ==========================================
        // 地图绘制 (支持多层级显示)
        // ==========================================
        private void UpdateMap()
        {
            if (view == null || splitContainer1.Panel2.Width == 0) return;
            // ... (view 更新和 Bitmap 创建代码保持不变) ...
            view.UpdateMapWindow(splitContainer1.Panel2.ClientRectangle);
            if (backwindow != null) backwindow.Dispose();
            backwindow = new Bitmap(splitContainer1.Panel2.Width, splitContainer1.Panel2.Height);

            using (Graphics g = Graphics.FromImage(backwindow))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                if (tiandituLayer != null) tiandituLayer.Draw(g, view);

                // 画行政区
                if (districtLayer != null)
                {
                    // 默认样式 (透明底)
                    XThematic defaultStyle = new XThematic(
                        new Pen(Color.Silver, 1),
                        new Pen(Color.Silver, 1),
                        new SolidBrush(Color.FromArgb(30, 200, 200, 200)),
                        new Pen(Color.Black), new SolidBrush(Color.Black), 2);

                    for (int i = 0; i < districtLayer.FeatureCount(); i++)
                    {
                        XFeature feature = districtLayer.GetFeature(i);
                        if (!feature.spatial.extent.IntersectOrNot(view.CurrentMapExtent)) continue;

                        XThematic styleToUse = defaultStyle;

                        // ============================================================
                        // 【关键】调试代码必须放在这个 IF 里面！！！
                        // 只有当热力图开启(Enabled) 且 数据存在(!=null) 时，才允许运行下面的代码
                        // ============================================================
                        if (_isHeatmapEnabled && _heatmapStats != null)
                        {
                            // 获取行政区名称 (假设在第0列，根据之前的弹窗结果调整)
                            string name = feature.getAttribute(0).ToString().Trim();


                            if (_heatmapStats.ContainsKey(name))
                            {
                                int count = _heatmapStats[name];
                                int alpha = Math.Min(220, 50 + count * 20);
                                SolidBrush heatBrush = new SolidBrush(Color.FromArgb(alpha, 0, 100, 255));
                                styleToUse = new XThematic(
                                    new Pen(Color.Gray, 1),
                                    new Pen(Color.Gray, 1),
                                    heatBrush,
                                    new Pen(Color.Black), new SolidBrush(Color.Black), 2);
                            }
                        }
                        // ============================================================

                        // 绘制
                        feature.draw(g, view, false, 0, styleToUse);
                    }
                }

                // 【核心修改】根据 TreeView 选中项，决定画什么
                TreeNode node = tvProfiles.SelectedNode;
                List<TripArchiveItem> tripsToDraw = new List<TripArchiveItem>();
                TripArchiveItem highlightTrip = null;

                string lengthText = "";

                if (node != null)
                {
                    // 1. 先尝试找到当前节点所属的 UserProfile 对象
                    UserProfile activeUser = null;
                    if (node.Tag is UserProfile u) activeUser = u;
                    else if (node.Tag is DailyArchive a && node.Parent?.Tag is UserProfile p) activeUser = p;
                    else if (node.Tag is TripArchiveItem t && node.Parent?.Parent?.Tag is UserProfile pp) activeUser = pp;

                    // 2. 如果找到了用户，就统一刷新左上角的用户信息 (头像 + 总里程)
                    if (activeUser != null)
                    {
                        // 刷新头像 (复用之前的逻辑)
                        if (!string.IsNullOrEmpty(activeUser.AvatarPath) && File.Exists(activeUser.AvatarPath))
                            pbAvatar.Image = Image.FromFile(activeUser.AvatarPath);
                        else
                        {
                            string fixPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "chr", Path.GetFileName(activeUser.AvatarPath));
                            if (File.Exists(fixPath)) pbAvatar.Image = Image.FromFile(fixPath);
                            else pbAvatar.Image = null;
                        }

                        // ★★★ 刷新左上角总里程 (这里就是之前一直显示0的原因) ★★★
                        lblUserName.Text = $"{activeUser.Name}\n总里程: {activeUser.TotalDistance:F1} km";
                    }

                    // 3. 处理不同节点的绘图和右下角标签逻辑
                    if (node.Tag is UserProfile u2)
                    {
                        lengthText = $"用户总里程: {u2.TotalDistance:F1} km";
                        foreach (var archive in u2.Archives) tripsToDraw.AddRange(archive.Trips);
                    }
                    else if (node.Tag is DailyArchive a)
                    {
                        double archiveLen = 0;
                        foreach (var t in a.Trips) archiveLen += t.Length;
                        lengthText = $"档案里程: {(archiveLen):F2} km"; // 米转公里

                        tripsToDraw.AddRange(a.Trips);
                    }
                    else if (node.Tag is TripArchiveItem t)
                    {
                        lengthText = $"线路长度: {(t.Length):F2} km"; // 米转公里
                        highlightTrip = t;
                        if (node.Parent?.Tag is DailyArchive pa) tripsToDraw.AddRange(pa.Trips);
                    }
                }

                // 更新右下角标签
                toolStripStatusLabel2.Text = lengthText;

                // 绘制背景行程 (蓝色)
                XThematic normalStyle = new XThematic();
                normalStyle.LinePen = new Pen(Color.Orange, 4);
                foreach (var t in tripsToDraw)
                {
                    if (t != highlightTrip) t.Geometry?.draw(g, view, normalStyle);
                }

                // 绘制高亮行程 (红色)
                if (highlightTrip != null)
                {
                    XThematic highlightStyle = new XThematic();
                    highlightStyle.LinePen = new Pen(Color.Red, 4);
                    highlightTrip.Geometry?.draw(g, view, highlightStyle);
                    // (画起终点省略，参考之前代码即可)
                }
            }
            try { splitContainer1.Panel2.Invalidate(); } catch { }
        }

        // ==========================================
        // ★★★ 新增：更新用户里程统计显示 ★★★
        // ==========================================
        private void UpdateUserStats(UserProfile user)
        {
            if (user == null)
            {
                // 如果没选中用户，只显示基础部分（防止文字残留）
                if (lblUserName.Text.Contains("\n"))
                    lblUserName.Text = lblUserName.Text.Split('\n')[0];
                return;
            }

            double totalKm = 0;

            // 遍历该用户下所有档案
            foreach (var archive in user.Archives)
            {
                // 遍历档案下所有行程
                foreach (var trip in archive.Trips)
                {
                    totalKm += trip.Length;
                }
            }

            // 更新 UI (在用户名下方显示)
            // 假设 lblUserName 初始只显示名字，我们用换行符追加里程
            // 先获取纯名字部分（防止重复叠加）
            string baseName = lblUserName.Text.Contains("\n") ? lblUserName.Text.Split('\n')[0] : user.Name;
            lblUserName.Text = $"{baseName}\n累计里程：{totalKm:F2} km";
        }



        // ... (以下辅助方法如 InitLoadingControl, LoadBasemap 等保持不变，参考上一个版本的代码即可) ...
        // 为了篇幅，这里不重复粘贴未修改的部分
        // 请保留 LoadingControl, MapTools, MapPanel 事件等代码

        private void InitLoadingControl()
        {
            pbLoading = new PictureBox();
            pbLoading.Name = "pbLoading";
            pbLoading.SizeMode = PictureBoxSizeMode.Zoom;
            pbLoading.Size = new Size(100, 100);
            pbLoading.BackColor = Color.Transparent;
            pbLoading.Visible = false;
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "bus_loading.png");
            if (File.Exists(imgPath)) { try { pbLoading.Image = Image.FromFile(imgPath); } catch { } }
            splitContainer1.Panel2.Controls.Add(pbLoading);
            pbLoading.BringToFront();
            CenterLoading();
            splitContainer1.Panel2.SizeChanged += (s, e) => CenterLoading();
        }

        private Dictionary<string, int> CalculateStatsForNode(object tag)
        {
            Dictionary<string, int> totalStats = new Dictionary<string, int>();

            // 内部辅助函数：合并字典
            void MergeStats(Dictionary<string, int> source)
            {
                foreach (var kvp in source)
                {
                    if (totalStats.ContainsKey(kvp.Key)) totalStats[kvp.Key] += kvp.Value;
                    else totalStats[kvp.Key] = kvp.Value;
                }
            }

            // 根据节点类型分级处理
            if (tag is TripArchiveItem trip)
            {
                // 单个行程
                var stats = _calculator.AnalyzeDistrictsByLogic(trip.RouteName, trip.Direction, trip.StartStop, trip.EndStop);
                MergeStats(stats);
            }
            else if (tag is DailyArchive archive)
            {
                // 整个档案
                foreach (var t in archive.Trips)
                {
                    var stats = _calculator.AnalyzeDistrictsByLogic(t.RouteName, t.Direction, t.StartStop, t.EndStop);
                    MergeStats(stats);
                }
            }
            else if (tag is UserProfile user)
            {
                // 整个用户
                foreach (var a in user.Archives)
                {
                    foreach (var t in a.Trips)
                    {
                        var stats = _calculator.AnalyzeDistrictsByLogic(t.RouteName, t.Direction, t.StartStop, t.EndStop);
                        MergeStats(stats);
                    }
                }
            }

            return totalStats;
        }

        private void CenterLoading() { if (pbLoading != null) { pbLoading.Location = new Point((splitContainer1.Panel2.Width - pbLoading.Width) / 2, (splitContainer1.Panel2.Height - pbLoading.Height) / 2); pbLoading.BringToFront(); } }
        private void LoadBasemap() { try { string shpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "shanghai_district.shp"); if (File.Exists(shpPath)) { districtLayer = XShapefile.ReadShapefile(shpPath); districtLayer.LabelOrNot = false; districtLayer.UnselectedThematic = new XThematic(new Pen(Color.LightGray, 1), new Pen(Color.LightGray, 1), new SolidBrush(Color.WhiteSmoke), new Pen(Color.Black), new SolidBrush(Color.Black), 2); view = new XView(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle); } else { view = new XView(new XExtent(0, 100, 0, 100), splitContainer1.Panel2.ClientRectangle); } } catch { } }
        private void LoadBusData() { _dataManager.LoadAllData(); }
        private void InitRouteSearch()
        {
            // 1. 设置下拉模式
            cbRoutes.DropDownStyle = ComboBoxStyle.DropDown;

            // 2. 【关键修复】关闭“自动调整高度”，防止索引越界 Bug
            cbRoutes.IntegralHeight = false;

            // 3. 【关键修复】确保自动完成模式为 None，防止与手动筛选冲突
            cbRoutes.AutoCompleteMode = AutoCompleteMode.None;
            cbRoutes.AutoCompleteSource = AutoCompleteSource.None;

            // 4. 绑定事件
            cbRoutes.TextUpdate += CbRoutes_TextUpdate;
        }
        private void CbRoutes_TextUpdate(object sender, EventArgs e)
        {
            string input = cbRoutes.Text;
            int cursorPosition = cbRoutes.SelectionStart;

            // =================================================
            // 分支 1：输入少于2个字，清空列表并收起
            // =================================================
            if (input.Length < 2)
            {
                // 只有当列表里有东西，或者下拉框还开着的时候，才需要处理
                if (cbRoutes.Items.Count > 0 || cbRoutes.DroppedDown)
                {
                    cbRoutes.BeginUpdate(); // 暂停绘制
                    try
                    {
                        // 【关键修改顺序】
                        // 1. 先把下拉框关掉！防止它在空列表状态下尝试计算高度
                        cbRoutes.DroppedDown = false;

                        // 2. 再重置索引
                        cbRoutes.SelectedIndex = -1;

                        // 3. 最后清空列表
                        cbRoutes.Items.Clear();

                        // 4. 恢复光标 (因为关掉下拉框有时会重置光标)
                        cbRoutes.Text = input;
                        cbRoutes.SelectionStart = cursorPosition;
                    }
                    catch { } // 绝对防御
                    finally
                    {
                        cbRoutes.EndUpdate();
                    }
                }
                return;
            }

            // =================================================
            // 分支 2：正常筛选 (保持之前的修复)
            // =================================================
            var matches = _dataManager.AllRoutes
                .Where(r => r.RouteName.Contains(input))
                .Select(r => r.RouteName)
                .Distinct()
                .Take(20)
                .ToArray();

            cbRoutes.BeginUpdate();
            try
            {
                cbRoutes.SelectedIndex = -1;
                cbRoutes.Items.Clear();

                if (matches.Length > 0)
                {
                    cbRoutes.Items.AddRange(matches);
                }
            }
            finally
            {
                cbRoutes.EndUpdate();
            }

            cbRoutes.Text = input;
            cbRoutes.SelectionStart = cursorPosition;

            try
            {
                if (matches.Length > 0)
                {
                    if (!cbRoutes.DroppedDown) cbRoutes.DroppedDown = true;
                    cbRoutes.Cursor = Cursors.Default;
                }
                else
                {
                    if (cbRoutes.DroppedDown) cbRoutes.DroppedDown = false;
                }
            }
            catch { }

            cbRoutes.SelectionStart = cursorPosition;
        }

        // ==========================================
        // 新增功能：重命名、打开文件、拖拽排序
        // ==========================================

        // 1. 重命名档案
        private void RenameArchive_Click()
        {
            DailyArchive archive = GetSelectedArchive();
            if (archive == null)
            {
                FrmActionBox.Show("请先选择一个【档案】！", ActionType.Error);
                return;
            }

            // 使用系统自带 InputBox 获取新名字
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入新的档案名称：", "重命名", archive.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != archive.Name)
            {
                if (ProfileManager.Instance.RenameArchive(archive, newName))
                {
                    FrmActionBox.Show("重命名成功！", ActionType.Success);
                    RefreshTree();
                    SelectNodeByTag(archive); // 保持选中
                }
                else
                {
                    FrmActionBox.Show("重命名失败，名称可能重复。", ActionType.Error);
                }
            }
        }

        // 2. 打开原始数据 (.trj)
        private void OpenRawData_Click()
        {
            DailyArchive archive = GetSelectedArchive();
            if (archive == null)
            {
                FrmActionBox.Show("请先选择一个【档案】！", ActionType.Error);
                return;
            }

            string path = ProfileManager.Instance.GetArchiveFilePath(archive);
            if (File.Exists(path))
            {
                try
                {
                    // 调用记事本打开
                    System.Diagnostics.Process.Start("notepad.exe", path);
                }
                catch (Exception ex)
                {
                    FrmActionBox.Show("无法打开文件：" + ex.Message, ActionType.Error);
                }
            }
            else
            {
                FrmActionBox.Show("文件未找到！", ActionType.Error);
            }
        }

        // 3. TreeView 拖拽排序逻辑
        private void TvProfiles_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // 只有“行程”节点(Level 3)允许拖拽
            TreeNode node = e.Item as TreeNode;
            if (node != null && node.Tag is TripArchiveItem)
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void TvProfiles_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void TvProfiles_DragOver(object sender, DragEventArgs e)
        {
            // 拖拽时自动选中鼠标下的节点，提供视觉反馈
            Point targetPoint = tvProfiles.PointToClient(new Point(e.X, e.Y));
            tvProfiles.SelectedNode = tvProfiles.GetNodeAt(targetPoint);
        }

        private void TvProfiles_DragDrop(object sender, DragEventArgs e)
        {
            Point targetPoint = tvProfiles.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = tvProfiles.GetNodeAt(targetPoint); // 放置目标
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode)); // 被拖拽节点

            if (targetNode == null || draggedNode == null) return;

            // 校验：必须都在同一个档案下（即同一个父节点）
            if (targetNode.Parent != draggedNode.Parent) return;
            if (!(targetNode.Tag is TripArchiveItem) || !(draggedNode.Tag is TripArchiveItem)) return;

            // --- 开始调整数据顺序 ---
            DailyArchive parentArchive = targetNode.Parent.Tag as DailyArchive;
            TripArchiveItem draggedTrip = draggedNode.Tag as TripArchiveItem;
            TripArchiveItem targetTrip = targetNode.Tag as TripArchiveItem;

            int oldIndex = parentArchive.Trips.IndexOf(draggedTrip);
            int newIndex = parentArchive.Trips.IndexOf(targetTrip);

            if (oldIndex != newIndex)
            {
                // 1. 调整内存 List 顺序
                parentArchive.Trips.RemoveAt(oldIndex);
                parentArchive.Trips.Insert(newIndex, draggedTrip);

                // 2. ★ 立即保存到文件 (ProfileManager 需要有 SaveArchive 方法)
                ProfileManager.Instance.SaveArchive(parentArchive);

                // 3. 刷新界面
                RefreshTree();
                SelectNodeByTag(draggedTrip);
                UpdateMap();
            }
        }
        private void cbRoutes_SelectedIndexChanged(object sender, EventArgs e) { if (cbRoutes.SelectedItem == null) return; cbDirection.Items.Clear(); cbStartStop.Items.Clear(); cbEndStop.Items.Clear(); string selectedRoute = cbRoutes.SelectedItem.ToString(); var directions = _dataManager.AllRoutes.Where(r => r.RouteName == selectedRoute).Select(r => r.Direction).ToList(); cbDirection.Items.AddRange(directions.ToArray()); if (cbDirection.Items.Count > 0) cbDirection.SelectedIndex = 0; }
        private void cbDirection_SelectedIndexChanged(object sender, EventArgs e) { cbStartStop.Items.Clear(); cbEndStop.Items.Clear(); if (cbRoutes.SelectedItem == null || cbDirection.SelectedItem == null) return; string key = $"{cbRoutes.SelectedItem}_{cbDirection.SelectedItem}"; if (_dataManager.RoutePaths.ContainsKey(key)) { List<string> stops = _dataManager.RoutePaths[key]; cbStartStop.Items.AddRange(stops.ToArray()); cbEndStop.Items.AddRange(stops.ToArray()); if (cbStartStop.Items.Count > 0) { cbStartStop.SelectedIndex = 0; cbEndStop.SelectedIndex = cbEndStop.Items.Count - 1; } } }
        private void MapPanel_Paint(object sender, PaintEventArgs e) { if (backwindow == null) return; if (currentMouseAction == XExploreActions.pan && MouseButtons == MouseButtons.Left) { int dx = MouseMovingLocation.X - MouseDownLocation.X; int dy = MouseMovingLocation.Y - MouseDownLocation.Y; e.Graphics.DrawImage(backwindow, dx, dy); } else { e.Graphics.DrawImage(backwindow, 0, 0); } }
        private void MapPanel_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { MouseDownLocation = e.Location; currentMouseAction = XExploreActions.pan; } }
        private void MapPanel_MouseMove(object sender, MouseEventArgs e)
        {
            XVertex v = view.ToMapVertex(e.Location);

            // 【修改这里】 原来可能是 labelXY.Text 或 toolStripStatusLabel3.Text
            // 改为第1个格子：
            toolStripStatusLabel3.Text = $"X: {v.x:F3}, Y: {v.y:F3}";

            if (e.Button == MouseButtons.Left && currentMouseAction == XExploreActions.pan)
            {
                MouseMovingLocation = e.Location;
                splitContainer1.Panel2.Invalidate();
            }
        }
        private void MapPanel_MouseUp(object sender, MouseEventArgs e) { if (currentMouseAction == XExploreActions.pan) { view.OffsetCenter(view.ToMapVertex(MouseDownLocation), view.ToMapVertex(e.Location)); UpdateMap(); } currentMouseAction = XExploreActions.noaction; }
        private void MapPanel_MouseWheel(object sender, MouseEventArgs e) { if (e.Delta > 0) view.ChangeView(XExploreActions.zoomin); else view.ChangeView(XExploreActions.zoomout); UpdateMap(); }
        private void MapTools_Click(object sender, EventArgs e) { if (sender == bZoomIn) view.ChangeView(XExploreActions.zoomin); else if (sender == bZoomOut) view.ChangeView(XExploreActions.zoomout); else if (sender == bFullExtent && districtLayer != null) view.Update(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle); UpdateMap(); }
        private void MapPanel_SizeChanged(object sender, EventArgs e) { CenterLoading(); UpdateMap(); }
    }
}