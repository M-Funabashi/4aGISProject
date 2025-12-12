using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            // 【新】初始化档案系统UI
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
            tvProfiles.ContextMenuStrip = cmsProfile;
            tvProfiles.AfterSelect += (s, e) => UpdateMap();
            tvProfiles.NodeMouseClick += TvProfiles_NodeMouseClick;

            // 3. 【修改】设置右键菜单 (加入导入导出功能)
            cmsProfile.Items.Clear();
            cmsProfile.Items.Add("新建用户", null, (s, e) => CreateNewUser());
            cmsProfile.Items.Add("新建档案", null, (s, e) => CreateNewArchive());

            cmsProfile.Items.Add(new ToolStripSeparator());
            cmsProfile.Items.Add("导入行程 (.trj)", null, (s, e) => ImportTrj_Click());
            cmsProfile.Items.Add("导出行程 (.trj)", null, (s, e) => ExportTrj_Click());

            cmsProfile.Items.Add(new ToolStripSeparator());
            cmsProfile.Items.Add("删除", null, (s, e) => DeleteCurrentNode());

            // 4. 加载数据并刷新
            ProfileManager.Instance.Load();
            ProfileManager.Instance.GetOrCreateDefaultUser();
            RefreshTree();
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

        // ==========================================
        // 【新增】 档案系统交互逻辑
        // ==========================================

        // 点击头像更换图片
        private void PbAvatar_Click(object sender, EventArgs e)
        {
            // 获取当前选中的用户
            UserProfile currentUser = GetSelectedUser();
            if (currentUser == null) { MessageBox.Show("请先选择一个用户！"); return; }

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
            if (user == null) { MessageBox.Show("请先选择所属用户！"); return; }

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
                MessageBox.Show("请先在列表中选中一个【用户】(或其下属节点)，以便导入数据！");
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
                    MessageBox.Show("导入过程中发生错误：" + ex.Message);
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
                MessageBox.Show("请先选中一个【档案】(如 '12月12日出游') 进行导出！\n(不能导出整个用户，只能导出单次档案)");
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
            // 1. 检查是否有选中的档案
            DailyArchive targetArchive = GetSelectedArchive();
            if (targetArchive == null)
            {
                MessageBox.Show("请先在左侧列表中选择一个【档案】(或档案下的行程)，以便存放新路线！\n(右键可新建档案)", "未选择档案");
                return;
            }

            if (cbStartStop.SelectedItem == null || cbEndStop.SelectedItem == null) { MessageBox.Show("请选择起止站！"); return; }

            string route = cbRoutes.Text;
            string dir = cbDirection.SelectedItem.ToString();
            string start = cbStartStop.SelectedItem.ToString();
            string end = cbEndStop.SelectedItem.ToString();

            pbLoading.Visible = true;
            btnAddTrip.Enabled = false;
            lblStats.Text = "计算中...";
            Application.DoEvents();

            XLineSpatial tripGeometry = null;
            Dictionary<string, int> stats = null;

            await Task.Run(() =>
            {
                try
                {
                    tripGeometry = _calculator.ReconstructTrip(route, dir, start, end);
                    if (tripGeometry != null) stats = _calculator.AnalyzeDistricts(tripGeometry, districtLayer);
                }
                catch { }
            });

            pbLoading.Visible = false;
            btnAddTrip.Enabled = true;

            if (tripGeometry != null)
            {
                // 2. 创建对象并添加到选中的档案中
                var newItem = new TripArchiveItem(route, dir, start, end, tripGeometry);
                targetArchive.Trips.Add(newItem); // 加到档案里

                // 3. 保存并刷新
                ProfileManager.Instance.Save();
                RefreshTree();

                // 选中新生成的节点
                // (这里简化处理，直接展开最后一个)
                tvProfiles.ExpandAll();
                UpdateMap();

                // 显示统计
                string report = "已保存至: " + targetArchive.Name + "\n";
                foreach (var kvp in stats) report += $"{kvp.Key}: {kvp.Value}站 ";
                lblStats.Text = report;
            }
            else
            {
                lblStats.Text = "计算失败";
            }
        }

        // ==========================================
        // 地图绘制 (支持多层级显示)
        // ==========================================
        private void UpdateMap()
        {
            if (view == null || splitContainer1.Panel2.Width == 0) return;
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
                    XThematic style = new XThematic(new Pen(Color.Silver, 1), new Pen(Color.Silver, 1), new SolidBrush(Color.FromArgb(30, 200, 200, 200)), new Pen(Color.Black), new SolidBrush(Color.Black), 2);
                    for (int i = 0; i < districtLayer.FeatureCount(); i++)
                        if (districtLayer.GetFeature(i).spatial.extent.IntersectOrNot(view.CurrentMapExtent))
                            districtLayer.GetFeature(i).draw(g, view, false, 0, style);
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
                normalStyle.LinePen = new Pen(Color.CornflowerBlue, 2);
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
            CenterLoading();
            splitContainer1.Panel2.SizeChanged += (s, e) => CenterLoading();
        }
        private void CenterLoading() { if (pbLoading != null) { pbLoading.Location = new Point((splitContainer1.Panel2.Width - pbLoading.Width) / 2, (splitContainer1.Panel2.Height - pbLoading.Height) / 2); pbLoading.BringToFront(); } }
        private void LoadBasemap() { try { string shpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "shanghai_district.shp"); if (File.Exists(shpPath)) { districtLayer = XShapefile.ReadShapefile(shpPath); districtLayer.LabelOrNot = false; districtLayer.UnselectedThematic = new XThematic(new Pen(Color.LightGray, 1), new Pen(Color.LightGray, 1), new SolidBrush(Color.WhiteSmoke), new Pen(Color.Black), new SolidBrush(Color.Black), 2); view = new XView(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle); } else { view = new XView(new XExtent(0, 100, 0, 100), splitContainer1.Panel2.ClientRectangle); } } catch { } }
        private void LoadBusData() { _dataManager.LoadAllData(); }
        private void InitRouteSearch() { cbRoutes.DropDownStyle = ComboBoxStyle.DropDown; cbRoutes.TextUpdate += CbRoutes_TextUpdate; }
        private void CbRoutes_TextUpdate(object sender, EventArgs e) { string input = cbRoutes.Text; if (input.Length < 2) { if (cbRoutes.Items.Count > 0) { cbRoutes.Items.Clear(); cbRoutes.DroppedDown = false; cbRoutes.SelectionStart = input.Length; } return; } var matches = _dataManager.AllRoutes.Where(r => r.RouteName.Contains(input)).Select(r => r.RouteName).Distinct().Take(20).ToArray(); cbRoutes.Items.Clear(); if (matches.Length > 0) { cbRoutes.Items.AddRange(matches); cbRoutes.DroppedDown = true; } else { cbRoutes.DroppedDown = false; } cbRoutes.SelectionStart = input.Length; }
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