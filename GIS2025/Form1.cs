using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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

        // UI 控件 (如果你是手动拖的控件，请确保名字一致，或者在这里声明)
        // private TreeView tvProfiles; 
        // private PictureBox pbAvatar;
        // private ContextMenuStrip cmsProfile;
        PictureBox pbLoading;

        public FormMap()
        {
            InitializeComponent();
            DoubleBuffered = true;
            // 双缓冲
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, splitContainer1.Panel2, new object[] { true });

            string myKey = "ded9721e66da88e4420647e0ef229c87";
            tiandituLayer = new XWebTileLayer(myKey);

            _dataManager = new BusDataManager();
            _calculator = new JourneyCalculator(_dataManager);

            LoadBasemap();
            LoadBusData();
            InitRouteSearch();
            InitLoadingControl();

            ProfileManager.Instance.Load();
            // 【新增】 恢复所有历史行程的红线 (这一步可能需要几秒，最好也做个Loading，这里暂且同步执行)
            ProfileManager.Instance.RestoreGeometries(_calculator);

            ProfileManager.Instance.GetOrCreateDefaultUser();

            InitProfileUI();

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 800;
            refreshTimer.Tick += (s, e) => { if (currentMouseAction == XExploreActions.noaction) UpdateMap(); };
            refreshTimer.Start();

            UpdateMap();
        }

        // ==========================================
        // 【新增】 档案系统 UI 初始化
        // ==========================================
        private void InitProfileUI()
        {
            // 1. 设置头像框
            pbAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbAvatar.Cursor = Cursors.Hand;
            pbAvatar.Click += PbAvatar_Click;
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
            var lastUser = ProfileManager.Instance.Users.LastOrDefault();
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
        }

        private void InitBottomToolbar()
        {
            // 辅助函数：安全加载图片
            void LoadIcon(PictureBox pb, string fileName)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "icon", fileName);
                if (File.Exists(path))
                {
                    pb.Image = Image.FromFile(path);
                }
                else
                {
                    // 如果找不到图片，给个背景色示意一下
                    pb.BackColor = Color.LightGray;
                }
            }

            // 1. 加载图片 (请确保文件名一致)
            LoadIcon(pbAddUser, "add_user.png");
            LoadIcon(pbAddArchive, "add_route.png");
            LoadIcon(pbImport, "load.png");
            LoadIcon(pbExport, "save.png");
            LoadIcon(pbAnalysis, "anal.png");
            LoadIcon(pbDelete, "delete.png");

            // 2. 绑定点击事件 (复用之前的逻辑方法)
            pbAddUser.Click += (s, e) => CreateNewUser();
            pbAddArchive.Click += (s, e) => CreateNewArchive();
            pbImport.Click += (s, e) => ImportTrj_Click();
            pbExport.Click += (s, e) => ExportTrj_Click();
            pbDelete.Click += (s, e) => DeleteCurrentNode();

            // 统计分析按钮需要特殊处理 (状态切换)
            pbAnalysis.Click += ToggleHeatmap_Click;
        }


        // 点击开关时
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
            }
            else
            {
                // 开启功能
                if (tvProfiles.SelectedNode == null)
                {
                    //MessageBox.Show("请先选择一个档案或用户！");
                    FrmActionBox.Show("请先选择一个档案或用户！", ActionType.Error);
                    return;
                }

                // 计算数据
                _heatmapStats = CalculateStatsForNode(tvProfiles.SelectedNode.Tag);
                _isHeatmapEnabled = true;

                // 【UI 反馈】 按钮背景变色，表示“按下/激活”状态
                pbAnalysis.BackColor = Color.LightSkyBlue;
                pbAnalysis.BorderStyle = BorderStyle.FixedSingle;
            }
            // 刷新地图
            UpdateMap();
        }

        // 刷新 TreeView 显示
        private void RefreshTree()
        {
            tvProfiles.Nodes.Clear();
            foreach (var user in ProfileManager.Instance.Users)
            {
                // 1级节点：用户
                TreeNode userNode = new TreeNode(user.Name);
                userNode.Tag = user; // 绑定数据对象
                userNode.ImageKey = "user";

                foreach (var archive in user.Archives)
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
            }
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
                ProfileManager.Instance.Save(); // 保存更改
            }
        }

        // 右键点击节点时自动选中它
        private void TvProfiles_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right) tvProfiles.SelectedNode = e.Node;
        }

        private void CreateNewUser()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入新用户名:", "新建用户", "新用户");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newUser = new UserProfile(name);
                ProfileManager.Instance.Users.Add(newUser);
                ProfileManager.Instance.Save();
                RefreshTree();
            }
        }

        private void CreateNewArchive()
        {
            // 必须选中一个用户或其子节点，才能创建档案
            UserProfile user = GetSelectedUser();
            if (user == null) { FrmActionBox.Show("请先选择一个档案或用户！", ActionType.Error); return; }

            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入档案名称:", "新建档案", DateTime.Now.ToString("yyyy-MM-dd") + " 出游");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newArchive = new DailyArchive(name);
                user.Archives.Add(newArchive);
                ProfileManager.Instance.Save();
                RefreshTree();
            }
        }

        private void DeleteCurrentNode()
        {
            TreeNode node = tvProfiles.SelectedNode;
            if (node == null) return;

            if (MessageBox.Show($"确定要删除 '{node.Text}' 吗？此操作不可恢复。", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (node.Tag is UserProfile u) ProfileManager.Instance.Users.Remove(u);
                else if (node.Tag is DailyArchive a)
                {
                    // 找到父用户并删除该档案
                    var parentUser = node.Parent.Tag as UserProfile;
                    parentUser?.Archives.Remove(a);
                }
                else if (node.Tag is TripArchiveItem t)
                {
                    var parentArchive = node.Parent.Tag as DailyArchive;
                    parentArchive?.Trips.Remove(t);
                }

                ProfileManager.Instance.Save();
                RefreshTree();
                UpdateMap();
            }
        }

        // ==========================================
        // .trj 导入导出交互逻辑
        // ==========================================

        private void ImportTrj_Click()
        {
            // 1. 必须选中一个“用户”节点，才知道要把行程导给谁
            UserProfile targetUser = GetSelectedUser();
            if (targetUser == null)
            {
                //MessageBox.Show("请先在列表中选中一个【用户】(或其下属节点)，以便导入数据！");
                FrmActionBox.Show("请先在列表中选中一个【用户】(或其下属节点)，以便导入数据！", ActionType.Error);
                return;
            }

            // 2. 选择文件
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "公交行程文件 (*.trj)|*.trj";
            dlg.Title = "导入行程记录";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // 3. 显示 Loading (因为重建轨迹可能需要几秒钟)
                pbLoading.Visible = true;
                lblStats.Text = "正在解析并重建轨迹...";
                Application.DoEvents(); // 强制刷新界面

                // 4. 调用管理器进行导入 (传入 calculator 以便计算红线)
                try
                {
                    ProfileManager.Instance.ImportTrj(dlg.FileName, targetUser, _calculator);

                    // 5. 刷新界面
                    RefreshTree();
                    tvProfiles.ExpandAll(); // 展开看新导入的东西
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("导入过程中发生错误：" + ex.Message);
                    FrmActionBox.Show("导入过程中发生错误：" + ex.Message, ActionType.Error);
                }
                finally
                {
                    pbLoading.Visible = false;
                    lblStats.Text = "导入完成";
                }
            }
        }

        private void ExportTrj_Click()
        {
            // 1. 必须选中一个“档案”节点
            DailyArchive targetArchive = GetSelectedArchive();
            if (targetArchive == null)
            {
                //MessageBox.Show("请先选中一个【档案】(如 '12月12日出游') 进行导出！\n(不能导出整个用户，只能导出单次档案)");
                FrmActionBox.Show("请先选中一个【档案】(如 '12月12日出游') 进行导出！\n(不能导出整个用户，只能导出单次档案)", ActionType.Error);
                return;
            }

            // 2. 找到这个档案所属的用户 (作为作者名)
            UserProfile author = null;
            // 简单反查：遍历所有用户找这个档案
            foreach (var u in ProfileManager.Instance.Users)
            {
                if (u.Archives.Contains(targetArchive)) { author = u; break; }
            }
            string authorName = author?.Name ?? "未知用户";

            // 3. 选择保存位置
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "公交行程文件 (*.trj)|*.trj";
            dlg.FileName = targetArchive.Name + ".trj"; // 默认文件名
            dlg.Title = "导出行程记录";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // 4. 调用管理器导出
                ProfileManager.Instance.ExportTrj(targetArchive, dlg.FileName, authorName);
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
            // ==========================================
            // 1. 【关键】先获取并检查档案 (定义在这里)
            // ==========================================
            DailyArchive targetArchive = GetSelectedArchive();
            if (targetArchive == null)
            {
                //MessageBox.Show("请先在左侧列表中选择一个【档案】(或档案下的行程)，以便存放新路线！\n(右键可新建档案)", "未选择档案");
                FrmActionBox.Show("请先在左侧列表中选择一个【档案】(或档案下的行程)，以便存放新路线！\n(右键可新建档案)", ActionType.Error);
                return;
            }

            // 2. 检查输入完整性
            if (cbStartStop.SelectedItem == null || cbEndStop.SelectedItem == null)
            {

                //MessageBox.Show("请选择完整的起止站点！");
                FrmActionBox.Show("请选择完整的起止站点！", ActionType.Error);
                return;
            }

            string route = cbRoutes.Text;
            string dir = cbDirection.SelectedItem.ToString();
            string start = cbStartStop.SelectedItem.ToString();
            string end = cbEndStop.SelectedItem.ToString();

            // ==========================================
            // 3. 启动 Loading 动画
            // ==========================================
            pbLoading.Visible = true;
            pbLoading.BringToFront();
            this.Refresh(); // 强制重绘

            btnAddTrip.Enabled = false;
            lblStats.Text = "正在规划路线...";
            refreshTimer.Stop();

            // ==========================================
            // 4. 后台计算
            // ==========================================
            XLineSpatial tripGeometry = null;
            Dictionary<string, int> stats = null;

            await Task.Run(() =>
            {
                try
                {
                    // 重建几何 (耗时)
                    tripGeometry = _calculator.ReconstructTrip(route, dir, start, end);
                    // 统计行政区 (极快)
                    stats = _calculator.AnalyzeDistrictsByLogic(route, dir, start, end);
                }
                catch { }
            });

            // ==========================================
            // 5. 恢复界面 & 保存结果
            // ==========================================
            pbLoading.Visible = false;
            btnAddTrip.Enabled = true;
            refreshTimer.Start();

            if (tripGeometry != null && stats != null)
            {
                // 【修复】直接使用最上面定义的 targetArchive，不要重新定义

                // 创建行程对象
                var newItem = new TripArchiveItem(route, dir, start, end, tripGeometry);

                // 加入档案
                targetArchive.Trips.Add(newItem);

                // 保存 (现在只存元数据，速度极快)
                ProfileManager.Instance.Save();

                // 刷新界面
                RefreshTree();
                // ==========================================
                // 【修改】 自动选中刚刚新建的行程
                // ==========================================
                SelectNodeByTag(newItem);

                // 注意：SelectNodeByTag 设置 SelectedNode 后，
                // 会自动触发 tvProfiles.AfterSelect 事件，从而调用 UpdateMap()。
                // 所以这里不需要手动调用 UpdateMap() 了，避免重复刷新。

                string report = $"行程：{start} -> {end}\n(共 {stats.Values.Sum()} 站)\n";
                foreach (var kvp in stats)
                {
                    report += $"{kvp.Key}: {kvp.Value}站 ";
                }
                lblStats.Text = report;
            }
            else
            {
                lblStats.Text = "路线生成失败，请检查数据完整性。";
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

                if (node != null)
                {
                    if (node.Tag is UserProfile u)
                    {
                        // 选中用户：画该用户所有档案的所有行程
                        foreach (var a in u.Archives) tripsToDraw.AddRange(a.Trips);

                        // 选中用户时，更新头像显示
                        if (File.Exists(u.AvatarPath)) pbAvatar.Image = Image.FromFile(u.AvatarPath);
                        else pbAvatar.Image = null; // 默认图
                        lblUserName.Text = u.Name;
                    }
                    else if (node.Tag is DailyArchive a)
                    {
                        // 选中档案：画该档案下的行程
                        tripsToDraw.AddRange(a.Trips);
                    }
                    else if (node.Tag is TripArchiveItem t)
                    {
                        // 选中行程：高亮它，背景画同档案的其他行程
                        highlightTrip = t;
                        var parentArchive = node.Parent.Tag as DailyArchive;
                        if (parentArchive != null) tripsToDraw.AddRange(parentArchive.Trips);
                    }
                }

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
        private void cbRoutes_SelectedIndexChanged(object sender, EventArgs e) { if (cbRoutes.SelectedItem == null) return; cbDirection.Items.Clear(); cbStartStop.Items.Clear(); cbEndStop.Items.Clear(); string selectedRoute = cbRoutes.SelectedItem.ToString(); var directions = _dataManager.AllRoutes.Where(r => r.RouteName == selectedRoute).Select(r => r.Direction).ToList(); cbDirection.Items.AddRange(directions.ToArray()); if (cbDirection.Items.Count > 0) cbDirection.SelectedIndex = 0; }
        private void cbDirection_SelectedIndexChanged(object sender, EventArgs e) { cbStartStop.Items.Clear(); cbEndStop.Items.Clear(); if (cbRoutes.SelectedItem == null || cbDirection.SelectedItem == null) return; string key = $"{cbRoutes.SelectedItem}_{cbDirection.SelectedItem}"; if (_dataManager.RoutePaths.ContainsKey(key)) { List<string> stops = _dataManager.RoutePaths[key]; cbStartStop.Items.AddRange(stops.ToArray()); cbEndStop.Items.AddRange(stops.ToArray()); if (cbStartStop.Items.Count > 0) { cbStartStop.SelectedIndex = 0; cbEndStop.SelectedIndex = cbEndStop.Items.Count - 1; } } }
        private void MapPanel_Paint(object sender, PaintEventArgs e) { if (backwindow == null) return; if (currentMouseAction == XExploreActions.pan && MouseButtons == MouseButtons.Left) { int dx = MouseMovingLocation.X - MouseDownLocation.X; int dy = MouseMovingLocation.Y - MouseDownLocation.Y; e.Graphics.DrawImage(backwindow, dx, dy); } else { e.Graphics.DrawImage(backwindow, 0, 0); } }
        private void MapPanel_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { MouseDownLocation = e.Location; currentMouseAction = XExploreActions.pan; } }
        private void MapPanel_MouseMove(object sender, MouseEventArgs e) { XVertex v = view.ToMapVertex(e.Location); labelXY.Text = $"X: {v.x:F3}, Y: {v.y:F3}"; if (e.Button == MouseButtons.Left && currentMouseAction == XExploreActions.pan) { MouseMovingLocation = e.Location; splitContainer1.Panel2.Invalidate(); } }
        private void MapPanel_MouseUp(object sender, MouseEventArgs e) { if (currentMouseAction == XExploreActions.pan) { view.OffsetCenter(view.ToMapVertex(MouseDownLocation), view.ToMapVertex(e.Location)); UpdateMap(); } currentMouseAction = XExploreActions.noaction; }
        private void MapPanel_MouseWheel(object sender, MouseEventArgs e) { if (e.Delta > 0) view.ChangeView(XExploreActions.zoomin); else view.ChangeView(XExploreActions.zoomout); UpdateMap(); }
        private void MapTools_Click(object sender, EventArgs e) { if (sender == bZoomIn) view.ChangeView(XExploreActions.zoomin); else if (sender == bZoomOut) view.ChangeView(XExploreActions.zoomout); else if (sender == bFullExtent && districtLayer != null) view.Update(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle); UpdateMap(); }
        private void MapPanel_SizeChanged(object sender, EventArgs e) { CenterLoading(); UpdateMap(); }
    }
}