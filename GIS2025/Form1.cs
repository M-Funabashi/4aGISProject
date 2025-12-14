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
using XGIS;

namespace GIS2025
{
    public partial class FormMap : Form
    {
        // --- GIS 核心对象 ---
        private XView view = null;
        private Bitmap backwindow;
        private XVectorLayer districtLayer; // 行政区图层
        private XWebTileLayer tiandituLayer; // 天地图底图

        // --- 业务逻辑对象 ---
        private BusDataManager _dataManager;
        private JourneyCalculator _calculator;

        // --- 交互与状态 ---
        private Point MouseDownLocation, MouseMovingLocation;
        private XExploreActions currentMouseAction = XExploreActions.noaction;
        private System.Windows.Forms.Timer refreshTimer;

        // --- 统计分析状态 ---
        private bool _isHeatmapEnabled = false; // 热力图开关
        private Dictionary<string, int> _heatmapStats = null; // 统计缓存
        private FrmStatsReport _currentReportForm = null; // 报告窗口实例

        // --- UI 辅助控件 ---
        private PictureBox pbLoading; // 加载动画


        public FormMap()
        {
            InitializeComponent();
            EnableDoubleBuffering(); // 开启双缓冲

            // 1. 初始化核心业务类
            string tiandituKey = "52e398187b19acc51fec54eb09f085c1"; // 天地图密钥
            tiandituLayer = new XWebTileLayer(tiandituKey);
            _dataManager = new BusDataManager();
            _calculator = new JourneyCalculator(_dataManager);

            // 2. 执行系统初始化
            InitSystem();

            // 3. 恢复用户状态
            if (ProfileManager.Instance.CurrentUser != null)
            {
                ProfileManager.Instance.RestoreGeometries(_calculator);
                SelectNodeByTag(ProfileManager.Instance.CurrentUser);
            }

            // 4. 启动定时器 (用于平滑刷新)
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 800;
            refreshTimer.Tick += (s, e) => { if (currentMouseAction == XExploreActions.noaction) UpdateMap(); };
            refreshTimer.Start();

            // 5. 初次绘制
            UpdateMap();
        }

        // 统一初始化入口
        private void InitSystem()
        {
            LoadBasemap();       // 加载地图
            LoadBusData();       // 加载公交数据
            InitRouteSearch();   // 初始化搜索框
            InitLoadingControl();// 初始化Loading动画
            InitStatusLabels();  // 初始化底部状态栏
            InitProfileUI();     // 初始化档案树和工具栏
        }

        // 开启 Panel 的双缓冲以减少闪烁
        private void EnableDoubleBuffering()
        {
            DoubleBuffered = true;
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, splitContainer1.Panel2, new object[] { true });
        }


        // 初始化状态栏 (XY坐标, 长度, 信息)
        private void InitStatusLabels()
        {
            // Label1: XY坐标 (固定宽度/左对齐)
            toolStripStatusLabel1.Spring = false;
            toolStripStatusLabel1.AutoSize = true;
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.Text = "X: 0.0, Y: 0.0";

            // Label2: 长度信息 (固定宽度/左对齐)
            toolStripStatusLabel2.Spring = false;
            toolStripStatusLabel2.AutoSize = true;
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel2.Text = "准备就绪";

            // Label3: 统计对象 (Spring=True, 占据剩余空间)
            toolStripStatusLabel3.Spring = true;
            toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel3.Text = "";
        }

        // 初始化档案系统 UI
        private void InitProfileUI()
        {
            // 头像与树控件设置
            pbAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            pbAvatar.Cursor = Cursors.Default;
            pbAvatar.BorderStyle = BorderStyle.FixedSingle;

            tvProfiles.HideSelection = false;
            tvProfiles.AllowDrop = true;
            // 绑定树控件事件
            tvProfiles.AfterSelect += (s, e) => UpdateMap();
            tvProfiles.ItemDrag += TvProfiles_ItemDrag;
            tvProfiles.DragEnter += TvProfiles_DragEnter;
            tvProfiles.DragOver += TvProfiles_DragOver;
            tvProfiles.DragDrop += TvProfiles_DragDrop;

            InitBottomToolbar();
            RefreshTree();
        }

        // 初始化底部图标按钮栏
        private void InitBottomToolbar()
        {
            // 内部辅助：安全加载图标
            void LoadIcon(PictureBox pb, string fileName)
            {
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.BackColor = Color.Transparent;
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "icon", fileName);
                // 兼容开发环境路径
                if (!File.Exists(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\data\pic\icon", fileName);

                if (File.Exists(path)) pb.Image = Image.FromFile(path);
                else pb.BackColor = Color.Red; // 错误提示
            }

            // 加载图标资源
            LoadIcon(pbAddUser, "add_user.png");
            LoadIcon(pbAddArchive, "add_route.png");
            LoadIcon(pbImport, "load.png");
            LoadIcon(pbExport, "save.png");
            LoadIcon(pbAnalysis, "anal.png");
            LoadIcon(pbDelete, "delete.png");
            LoadIcon(pbRename, "edit.png");
            LoadIcon(pbOpenRaw, "file.png");

            // 绑定点击事件
            pbAddUser.Click += (s, e) => SwitchUser();
            pbAddArchive.Click += (s, e) => CreateNewArchive();
            pbImport.Click += (s, e) => ImportTrj_Click();
            pbExport.Click += (s, e) => ExportTrj_Click();
            pbDelete.Click += (s, e) => DeleteCurrentNode();
            pbRename.Click += (s, e) => RenameArchive_Click();
            pbOpenRaw.Click += (s, e) => OpenRawData_Click();
            pbAnalysis.Click += ToggleHeatmap_Click;

            // 设置提示
            toolTip1.SetToolTip(pbAddUser, "切换用户");
            toolTip1.SetToolTip(pbAddArchive, "新建档案");
            toolTip1.SetToolTip(pbImport, "导入行程 (.trj)");
            toolTip1.SetToolTip(pbExport, "导出行程 (.trj)");
            toolTip1.SetToolTip(pbAnalysis, "开启/关闭 统计分析");
            toolTip1.SetToolTip(pbDelete, "删除选中项");
            toolTip1.SetToolTip(pbRename, "重命名档案");
            toolTip1.SetToolTip(pbOpenRaw, "查看原始文件(.trj)");
        }

        private void InitLoadingControl()
        {
            pbLoading = new PictureBox
            {
                Name = "pbLoading",
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(100, 100),
                BackColor = Color.Transparent,
                Visible = false
            };
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "bus_loading.png");
            if (File.Exists(imgPath)) try { pbLoading.Image = Image.FromFile(imgPath); } catch { }
            splitContainer1.Panel2.Controls.Add(pbLoading);
            pbLoading.BringToFront();
            CenterLoading();
            splitContainer1.Panel2.SizeChanged += (s, e) => CenterLoading();
        }

        private void CenterLoading()
        {
            if (pbLoading != null)
                pbLoading.Location = new Point((splitContainer1.Panel2.Width - pbLoading.Width) / 2, (splitContainer1.Panel2.Height - pbLoading.Height) / 2);
        }


        // 核心绘制方法：负责渲染所有图层
        private void UpdateMap()
        {
            if (view == null || splitContainer1.Panel2.Width == 0) return;

            // 1. 更新视图窗口
            view.UpdateMapWindow(splitContainer1.Panel2.ClientRectangle);
            if (backwindow != null) backwindow.Dispose();
            backwindow = new Bitmap(splitContainer1.Panel2.Width, splitContainer1.Panel2.Height);

            using (Graphics g = Graphics.FromImage(backwindow))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // 2. 绘制天地图底图
                if (tiandituLayer != null) tiandituLayer.Draw(g, view);

                // 3. 绘制行政区 (含热力图逻辑)
                DrawDistricts(g);

                // 4. 绘制路线与档案 (含高亮逻辑)
                DrawRoutes(g);
            }

            // 触发控件重绘
            try { splitContainer1.Panel2.Invalidate(); } catch { }
        }

        // 辅助绘制：行政区与热力图
        private void DrawDistricts(Graphics g)
        {
            if (districtLayer == null) return;

            // 默认样式
            XThematic defaultStyle = new XThematic(
                new Pen(Color.Silver, 1), new Pen(Color.Silver, 1),
                new SolidBrush(Color.FromArgb(30, 200, 200, 200)),
                new Pen(Color.Black), new SolidBrush(Color.Black), 2);

            for (int i = 0; i < districtLayer.FeatureCount(); i++)
            {
                XFeature feature = districtLayer.GetFeature(i);
                if (!feature.spatial.extent.IntersectOrNot(view.CurrentMapExtent)) continue;

                XThematic styleToUse = defaultStyle;

                // 热力图渲染逻辑
                if (_isHeatmapEnabled && _heatmapStats != null)
                {
                    string name = feature.getAttribute(0).ToString().Trim();
                    if (_heatmapStats.ContainsKey(name))
                    {
                        int count = _heatmapStats[name];
                        int alpha = Math.Min(220, 50 + count * 20); // 根据次数计算透明度
                        styleToUse = new XThematic(
                            new Pen(Color.Gray, 1), new Pen(Color.Gray, 1),
                            new SolidBrush(Color.FromArgb(alpha, 0, 100, 255)),
                            new Pen(Color.Black), new SolidBrush(Color.Black), 2);
                    }
                }
                feature.draw(g, view, false, 0, styleToUse);
            }
        }

        // 辅助绘制：路线与用户状态
        private void DrawRoutes(Graphics g)
        {
            TreeNode node = tvProfiles.SelectedNode;
            List<TripArchiveItem> tripsToDraw = new List<TripArchiveItem>();
            TripArchiveItem highlightTrip = null;
            string lengthText = "就绪";

            if (node != null)
            {
                // 1. 获取关联的用户对象，更新左上角信息
                UserProfile activeUser = GetUserFromNode(node);
                if (activeUser != null)
                {
                    UpdateUserAvatar(activeUser);
                    lblUserName.Text = $"{activeUser.Name}\n总里程: {activeUser.TotalDistance:F1} km";
                }

                // 2. 确定要绘制的路线集合
                if (node.Tag is UserProfile u)
                {
                    lengthText = $"用户总里程: {u.TotalDistance:F1} km";
                    foreach (var archive in u.Archives) tripsToDraw.AddRange(archive.Trips);
                }
                else if (node.Tag is DailyArchive a)
                {
                    double archiveLen = a.Trips.Sum(t => t.Length);
                    lengthText = $"档案里程: {archiveLen:F2} km";
                    tripsToDraw.AddRange(a.Trips);
                }
                else if (node.Tag is TripArchiveItem t)
                {
                    lengthText = $"线路长度: {t.Length:F2} km";
                    highlightTrip = t;
                    // 同时绘制同一档案下的背景路线
                    if (node.Parent?.Tag is DailyArchive pa) tripsToDraw.AddRange(pa.Trips);
                }
            }

            // 更新底部状态栏 (长度信息)
            toolStripStatusLabel2.Text = lengthText;

            // 绘制普通路线 (橙色)
            XThematic normalStyle = new XThematic { LinePen = new Pen(Color.Orange, 4) };
            foreach (var t in tripsToDraw)
            {
                if (t != highlightTrip) t.Geometry?.draw(g, view, normalStyle);
            }

            // 绘制高亮路线 (红色)
            if (highlightTrip != null)
            {
                XThematic highlightStyle = new XThematic { LinePen = new Pen(Color.Red, 4) };
                highlightTrip.Geometry?.draw(g, view, highlightStyle);
            }
        }

        // --- 地图交互事件 ---

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow == null) return;
            // 漫游时的快速重绘
            if (currentMouseAction == XExploreActions.pan && MouseButtons == MouseButtons.Left)
            {
                int dx = MouseMovingLocation.X - MouseDownLocation.X;
                int dy = MouseMovingLocation.Y - MouseDownLocation.Y;
                e.Graphics.DrawImage(backwindow, dx, dy);
            }
            else
            {
                e.Graphics.DrawImage(backwindow, 0, 0);
            }
        }

        private void MapPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MouseDownLocation = e.Location;
                currentMouseAction = XExploreActions.pan;
            }
        }

        private void MapPanel_MouseMove(object sender, MouseEventArgs e)
        {
            XVertex v = view.ToMapVertex(e.Location);
            // 更新 Label1 (XY坐标)
            toolStripStatusLabel1.Text = $"X: {v.x:F3}, Y: {v.y:F3}";

            if (e.Button == MouseButtons.Left && currentMouseAction == XExploreActions.pan)
            {
                MouseMovingLocation = e.Location;
                splitContainer1.Panel2.Invalidate();
            }
        }

        private void MapPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (currentMouseAction == XExploreActions.pan)
            {
                view.OffsetCenter(view.ToMapVertex(MouseDownLocation), view.ToMapVertex(e.Location));
                UpdateMap();
            }
            currentMouseAction = XExploreActions.noaction;
        }

        private void MapPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0) view.ChangeView(XExploreActions.zoomin);
            else view.ChangeView(XExploreActions.zoomout);
            UpdateMap();
        }

        // 缩放工具栏点击
        private void MapTools_Click(object sender, EventArgs e)
        {
            if (sender == bZoomIn) view.ChangeView(XExploreActions.zoomin);
            else if (sender == bZoomOut) view.ChangeView(XExploreActions.zoomout);
            else if (sender == bFullExtent && districtLayer != null) view.Update(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle);
            else if (sender == bPan) { /* 漫游模式默认开启 */ }
            UpdateMap();
        }

        private void MapPanel_SizeChanged(object sender, EventArgs e)
        {
            CenterLoading();
            UpdateMap();
        }


        // 刷新树形控件
        private void RefreshTree()
        {
            tvProfiles.Nodes.Clear();
            var currentUser = ProfileManager.Instance.CurrentUser;
            if (currentUser == null) return;

            TreeNode userNode = new TreeNode(currentUser.Name) { Tag = currentUser, ImageKey = "user" };

            foreach (var archive in currentUser.Archives)
            {
                TreeNode archiveNode = new TreeNode(archive.Name) { Tag = archive };
                foreach (var trip in archive.Trips)
                {
                    TreeNode tripNode = new TreeNode($"{trip.RouteName} ({trip.StartStop}-{trip.EndStop})") { Tag = trip };
                    archiveNode.Nodes.Add(tripNode);
                }
                userNode.Nodes.Add(archiveNode);
            }
            tvProfiles.Nodes.Add(userNode);
            tvProfiles.ExpandAll();
        }

        // 切换用户
        private void SwitchUser()
        {
            if (FrmActionBox.Show("确定要退出当前用户并切换账号吗？", ActionType.Confirm) == DialogResult.Yes)
            {
                Application.Restart();
            }
        }

        // 新建档案
        private void CreateNewArchive()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入档案名称:", "新建档案", DateTime.Now.ToString("yyyy-MM-dd") + " 出游");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var newArchive = new DailyArchive(name);
                ProfileManager.Instance.CurrentUser.Archives.Add(newArchive);
                ProfileManager.Instance.SaveArchive(newArchive); // 立即保存文件

                RefreshTree();
                SelectNodeByTag(newArchive);
                FrmActionBox.Show("创建成功", ActionType.Success);
            }
        }

        // 重命名档案
        private void RenameArchive_Click()
        {
            DailyArchive archive = GetSelectedArchive();
            if (archive == null) { FrmActionBox.Show("请先选择一个【档案】！", ActionType.Error); return; }

            string newName = Microsoft.VisualBasic.Interaction.InputBox("请输入新的档案名称：", "重命名", archive.Name);
            if (!string.IsNullOrWhiteSpace(newName) && newName != archive.Name)
            {
                if (ProfileManager.Instance.RenameArchive(archive, newName))
                {
                    FrmActionBox.Show("重命名成功！", ActionType.Success);
                    RefreshTree();
                    SelectNodeByTag(archive);
                }
                else FrmActionBox.Show("重命名失败，名称可能重复。", ActionType.Error);
            }
        }

        // 删除节点
        private void DeleteCurrentNode()
        {
            TreeNode node = tvProfiles.SelectedNode;
            if (node == null || node.Tag is UserProfile) { FrmActionBox.Show("无法删除用户或未选择节点", ActionType.Error); return; }

            if (FrmActionBox.Show($"确定要删除 '{node.Text}' 吗？此操作不可恢复。", ActionType.Confirm) == DialogResult.Yes)
            {
                if (node.Tag is DailyArchive archive)
                {
                    ProfileManager.Instance.DeleteArchive(archive);
                }
                else if (node.Tag is TripArchiveItem trip && node.Parent.Tag is DailyArchive parent)
                {
                    parent.Trips.Remove(trip);
                    ProfileManager.Instance.SaveArchive(parent);
                }
                RefreshTree();
                UpdateMap();
            }
        }

        // 导入.trj文件
        private void ImportTrj_Click()
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "公交行程文件 (*.trj)|*.trj", Title = "导入公交行程文件" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string userDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", ProfileManager.Instance.CurrentUser.Name);
                    string destPath = Path.Combine(userDir, Path.GetFileName(dlg.FileName));
                    File.Copy(dlg.FileName, destPath, true);

                    // 重新加载用户数据
                    ProfileManager.Instance.LoginUser(ProfileManager.Instance.CurrentUser.Name);
                    ProfileManager.Instance.RestoreGeometries(_calculator);

                    RefreshTree();
                    FrmActionBox.Show("导入成功！", ActionType.Success);
                }
                catch (Exception ex) { FrmActionBox.Show("导入失败: " + ex.Message, ActionType.Error); }
            }
        }

        // 导出.trj文件
        private void ExportTrj_Click()
        {
            DailyArchive target = GetSelectedArchive();
            if (target == null) { FrmActionBox.Show("请先选中一个档案进行导出", ActionType.Error); return; }

            SaveFileDialog dlg = new SaveFileDialog { Filter = "公交行程文件 (*.trj)|*.trj", FileName = target.Name + ".trj", Title = "分享我的行程" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(target, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(dlg.FileName, json);
                    FrmActionBox.Show("导出成功！", ActionType.Success);
                }
                catch (Exception ex) { FrmActionBox.Show("导出失败: " + ex.Message, ActionType.Error); }
            }
        }

        // 查看原始JSON数据
        private void OpenRawData_Click()
        {
            DailyArchive archive = GetSelectedArchive();
            if (archive == null) return;
            string path = ProfileManager.Instance.GetArchiveFilePath(archive);
            if (File.Exists(path)) System.Diagnostics.Process.Start("notepad.exe", path);
        }

        // 拖拽排序逻辑
        private void TvProfiles_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag is TripArchiveItem)
                DoDragDrop(e.Item, DragDropEffects.Move);
        }
        private void TvProfiles_DragEnter(object sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;
        private void TvProfiles_DragOver(object sender, DragEventArgs e)
        {
            Point pt = tvProfiles.PointToClient(new Point(e.X, e.Y));
            tvProfiles.SelectedNode = tvProfiles.GetNodeAt(pt);
        }
        private void TvProfiles_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode targetNode = tvProfiles.GetNodeAt(tvProfiles.PointToClient(new Point(e.X, e.Y)));
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

            if (targetNode == null || draggedNode == null) return;
            if (targetNode.Parent != draggedNode.Parent || !(targetNode.Tag is TripArchiveItem targetTrip) || !(draggedNode.Tag is TripArchiveItem draggedTrip)) return;

            DailyArchive parent = targetNode.Parent.Tag as DailyArchive;
            int oldIdx = parent.Trips.IndexOf(draggedTrip);
            int newIdx = parent.Trips.IndexOf(targetTrip);

            if (oldIdx != newIdx)
            {
                parent.Trips.RemoveAt(oldIdx);
                parent.Trips.Insert(newIdx, draggedTrip);
                ProfileManager.Instance.SaveArchive(parent);
                RefreshTree();
                SelectNodeByTag(draggedTrip);
                UpdateMap();
            }
        }

        // 搜索栏下拉逻辑
        private void InitRouteSearch()
        {
            cbRoutes.DropDownStyle = ComboBoxStyle.DropDown;
            cbRoutes.IntegralHeight = false; 
            cbRoutes.AutoCompleteMode = AutoCompleteMode.None;
            cbRoutes.TextUpdate += CbRoutes_TextUpdate;
        }

        // 动态搜索过滤
        private void CbRoutes_TextUpdate(object sender, EventArgs e)
        {
            string input = cbRoutes.Text;
            int cursorPosition = cbRoutes.SelectionStart;

            if (input.Length < 2)
            {
                if (cbRoutes.Items.Count > 0 || cbRoutes.DroppedDown)
                {
                    cbRoutes.BeginUpdate();
                    cbRoutes.DroppedDown = false;
                    cbRoutes.SelectedIndex = -1;
                    cbRoutes.Items.Clear();
                    cbRoutes.Text = input;
                    cbRoutes.SelectionStart = cursorPosition;
                    cbRoutes.EndUpdate();
                }
                return;
            }

            var matches = _dataManager.AllRoutes
                .Where(r => r.RouteName.Contains(input))
                .Select(r => r.RouteName).Distinct().Take(20).ToArray();

            cbRoutes.BeginUpdate();
            cbRoutes.SelectedIndex = -1;
            cbRoutes.Items.Clear();
            if (matches.Length > 0) cbRoutes.Items.AddRange(matches);
            cbRoutes.EndUpdate();

            cbRoutes.Text = input;
            cbRoutes.SelectionStart = cursorPosition;
            if (matches.Length > 0)
            {
                cbRoutes.DroppedDown = true;
                cbRoutes.Cursor = Cursors.Default;
            }
        }

        // 级联选择：线路 -> 方向 -> 站点
        private void cbRoutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbRoutes.SelectedItem == null) return;
            cbDirection.Items.Clear(); cbStartStop.Items.Clear(); cbEndStop.Items.Clear();
            var directions = _dataManager.AllRoutes.Where(r => r.RouteName == cbRoutes.Text).Select(r => r.Direction).ToList();
            cbDirection.Items.AddRange(directions.ToArray());
            if (cbDirection.Items.Count > 0) cbDirection.SelectedIndex = 0;
        }

        private void cbDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbStartStop.Items.Clear(); cbEndStop.Items.Clear();
            if (cbRoutes.SelectedItem == null || cbDirection.SelectedItem == null) return;
            string key = $"{cbRoutes.SelectedItem}_{cbDirection.SelectedItem}";
            if (_dataManager.RoutePaths.ContainsKey(key))
            {
                var stops = _dataManager.RoutePaths[key];
                cbStartStop.Items.AddRange(stops.ToArray());
                cbEndStop.Items.AddRange(stops.ToArray());
                if (cbStartStop.Items.Count > 0)
                {
                    cbStartStop.SelectedIndex = 0;
                    cbEndStop.SelectedIndex = cbEndStop.Items.Count - 1;
                }
            }
        }

        // 生成行程并保存
        private async void btnAddTrip_Click(object sender, EventArgs e)
        {
            DailyArchive targetArchive = GetSelectedArchive();
            if (targetArchive == null) { FrmActionBox.Show("请先在左侧选择一个【档案】存放新路线！", ActionType.Error); return; }
            if (cbStartStop.SelectedItem == null || cbEndStop.SelectedItem == null) { FrmActionBox.Show("请选择完整的起止站点！", ActionType.Error); return; }

            // 锁定界面
            pbLoading.Visible = true; pbLoading.BringToFront(); this.Refresh();
            btnAddTrip.Enabled = false; lblStats.Text = "正在规划路线..."; refreshTimer.Stop();

            string route = cbRoutes.Text;
            string dir = cbDirection.Text;
            string start = cbStartStop.Text;
            string end = cbEndStop.Text;

            XLineSpatial tripGeometry = null;
            Dictionary<string, int> stats = null;

            // 异步计算
            await Task.Run(() =>
            {
                try
                {
                    tripGeometry = _calculator.ReconstructTrip(route, dir, start, end);
                    stats = _calculator.AnalyzeDistrictsByLogic(route, dir, start, end);
                }
                catch { }
            });

            // 恢复界面
            pbLoading.Visible = false; btnAddTrip.Enabled = true; refreshTimer.Start();

            if (tripGeometry != null && stats != null)
            {
                var newItem = new TripArchiveItem(route, dir, start, end, tripGeometry) { Length = tripGeometry.length };
                targetArchive.Trips.Add(newItem);
                ProfileManager.Instance.SaveArchive(targetArchive); // 保存

                RefreshTree();
                SelectNodeByTag(newItem);

                // 更新UI反馈
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"行程：{start} -> {end}");
                sb.AppendLine($"里程：{newItem.Length} km (共 {stats.Values.Sum()} 站)");
                foreach (var kvp in stats) sb.Append($"{kvp.Key}: {kvp.Value}站 ");
                lblStats.Text = sb.ToString();
            }
        }

        private void ToggleHeatmap_Click(object sender, EventArgs e)
        {
            if (_isHeatmapEnabled)
            {
                // 关闭分析
                _isHeatmapEnabled = false;
                _heatmapStats = null;
                pbAnalysis.BackColor = Color.Transparent;
                pbAnalysis.BorderStyle = BorderStyle.None;
                toolStripStatusLabel3.Text = ""; // 清空信息栏
            }
            else
            {
                // 开启分析
                if (tvProfiles.SelectedNode == null) { FrmActionBox.Show("请先选择一个档案或用户！", ActionType.Error); return; }

                _heatmapStats = CalculateStatsForNode(tvProfiles.SelectedNode.Tag);
                _isHeatmapEnabled = true;
                pbAnalysis.BackColor = Color.LightSkyBlue;
                pbAnalysis.BorderStyle = BorderStyle.FixedSingle;

                // 更新 Label3 (当前分析对象)
                toolStripStatusLabel3.Text = $"分析对象: {tvProfiles.SelectedNode.Text}";

                ShowCoverageReport(_heatmapStats);
            }
            UpdateMap();
        }

        // 递归计算统计数据
        private Dictionary<string, int> CalculateStatsForNode(object tag)
        {
            Dictionary<string, int> totalStats = new Dictionary<string, int>();
            void Merge(Dictionary<string, int> src)
            {
                foreach (var kvp in src)
                {
                    if (totalStats.ContainsKey(kvp.Key)) totalStats[kvp.Key] += kvp.Value;
                    else totalStats[kvp.Key] = kvp.Value;
                }
            }

            if (tag is TripArchiveItem trip) Merge(_calculator.AnalyzeDistrictsByLogic(trip.RouteName, trip.Direction, trip.StartStop, trip.EndStop));
            else if (tag is DailyArchive archive) foreach (var t in archive.Trips) Merge(_calculator.AnalyzeDistrictsByLogic(t.RouteName, t.Direction, t.StartStop, t.EndStop));
            else if (tag is UserProfile user) foreach (var a in user.Archives) foreach (var t in a.Trips) Merge(_calculator.AnalyzeDistrictsByLogic(t.RouteName, t.Direction, t.StartStop, t.EndStop));

            return totalStats;
        }

        // 显示覆盖率报表弹窗
        private void ShowCoverageReport(Dictionary<string, int> visitedStats)
        {
            if (districtLayer == null) return;
            List<string> allRegions = new List<string>();
            for (int i = 0; i < districtLayer.FeatureCount(); i++)
            {
                string name = districtLayer.GetFeature(i).getAttribute(0).ToString().Trim();
                if (!allRegions.Contains(name)) allRegions.Add(name);
            }

            if (_currentReportForm != null && !_currentReportForm.IsDisposed) _currentReportForm.BringToFront();
            else
            {
                _currentReportForm = new FrmStatsReport(visitedStats, allRegions);
                _currentReportForm.Show(this);
            }
        }

        // 加载shp行政区
        private void LoadBasemap()
        {
            try
            {
                string shpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "shanghai_district.shp");
                if (File.Exists(shpPath))
                {
                    districtLayer = XShapefile.ReadShapefile(shpPath);
                    districtLayer.LabelOrNot = false;
                    view = new XView(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle);
                }
                else view = new XView(new XExtent(0, 100, 0, 100), splitContainer1.Panel2.ClientRectangle);
            }
            catch { }
        }

        private void LoadBusData() => _dataManager.LoadAllData();

        private void SelectNodeByTag(object tag)
        {
            if (tag == null) return;
            foreach (TreeNode userNode in tvProfiles.Nodes)
            {
                if (userNode.Tag == tag) { tvProfiles.SelectedNode = userNode; return; }
                foreach (TreeNode archiveNode in userNode.Nodes)
                {
                    if (archiveNode.Tag == tag) { tvProfiles.SelectedNode = archiveNode; return; }
                    foreach (TreeNode tripNode in archiveNode.Nodes)
                    {
                        if (tripNode.Tag == tag) { tvProfiles.SelectedNode = tripNode; return; }
                    }
                }
            }
        }

        private UserProfile GetUserFromNode(TreeNode node)
        {
            while (node != null)
            {
                if (node.Tag is UserProfile u) return u;
                node = node.Parent;
            }
            return null;
        }

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

        private void UpdateUserAvatar(UserProfile user)
        {
            if (!string.IsNullOrEmpty(user.AvatarPath) && File.Exists(user.AvatarPath))
                pbAvatar.Image = Image.FromFile(user.AvatarPath);
            else
            {
                string fixPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "chr", Path.GetFileName(user.AvatarPath));
                if (File.Exists(fixPath)) pbAvatar.Image = Image.FromFile(fixPath);
                else pbAvatar.Image = null;
            }
        }
    }
}