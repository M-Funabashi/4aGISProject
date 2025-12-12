using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using XGIS;

namespace GIS2025
{
    public partial class FormMap : Form
    {
        // ==========================================
        // 核心变量1
        // ==========================================
        XView view = null;
        Bitmap backwindow;
        XVectorLayer districtLayer;

        // 使用 BindingList 管理所有行程
        BindingList<TripArchiveItem> _tripHistory = new BindingList<TripArchiveItem>();

        BusDataManager _dataManager;
        JourneyCalculator _calculator;

        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;

        // 右键菜单
        ContextMenuStrip listContextMenu;

        // 在 FormMap 类中添加
        XWebTileLayer tiandituLayer;
        System.Windows.Forms.Timer refreshTimer;

        public FormMap()
        {
            InitializeComponent();
            DoubleBuffered = true;
            // 开启 Panel 双缓冲，防止闪烁
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, splitContainer1.Panel2, new object[] { true });

            string myKey = "52e398187b19acc51fec54eb09f085c1"; // 比如 "7b...xx"
            tiandituLayer = new XWebTileLayer(myKey);

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 800; // 0.8秒刷一次
            refreshTimer.Tick += (s, e) => {
                // 只有鼠标没在拖动时才刷新，防止闪烁
                if (currentMouseAction == XExploreActions.noaction)
                    UpdateMap();
            };
            refreshTimer.Start();

            _dataManager = new BusDataManager();
            _calculator = new JourneyCalculator(_dataManager);


            LoadBasemap();
            LoadBusData();
            InitRouteSearch();
            InitTripList();

            // 【新增】启动一个定时器，每秒刷新一次界面
            // 作用：当后台瓦片下载完成后，界面能自动显示出来
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 800;
            refreshTimer.Tick += (s, e) => {
                // 只有在没有进行鼠标操作时才刷新，避免干扰拖动
                if (currentMouseAction == XExploreActions.noaction)
                    UpdateMap();
            };
            refreshTimer.Start();



            UpdateMap();



        }

        private void InitTripList()
        {
            // 1. 绑定数据源
            lstTrips.DataSource = _tripHistory;

            // 2. 开启自定义格式化
            lstTrips.FormattingEnabled = true;
            lstTrips.Format += LstTrips_Format;

            // 3. 开启拖拽支持
            lstTrips.AllowDrop = true;
            lstTrips.MouseDown += LstTrips_MouseDown;
            lstTrips.DragOver += LstTrips_DragOver;
            lstTrips.DragDrop += LstTrips_DragDrop;

            // 4. 创建右键菜单
            listContextMenu = new ContextMenuStrip();
            ToolStripMenuItem itemDelete = new ToolStripMenuItem("删除选中 (Delete)");
            itemDelete.Click += (s, e) => DeleteSelectedTrip();
            ToolStripMenuItem itemClear = new ToolStripMenuItem("清空所有 (Clear All)");
            itemClear.Click += (s, e) => ClearAllTrips();

            listContextMenu.Items.Add(itemDelete);
            listContextMenu.Items.Add(new ToolStripSeparator());
            listContextMenu.Items.Add(itemClear);

            lstTrips.ContextMenuStrip = listContextMenu;

            // 列表变动时刷新地图
            lstTrips.SelectedIndexChanged += (s, e) => UpdateMap();
        }

        private void LstTrips_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is TripArchiveItem item)
            {
                int index = _tripHistory.IndexOf(item) + 1; // 序号从1开始
                e.Value = $"{index}. {item.RouteName} ({item.StartStop}-{item.EndStop})";
            }
        }

        // ==========================================
        // 【核心修改点 1】 修复右键菜单与拖拽冲突
        // ==========================================
        private void LstTrips_MouseDown(object sender, MouseEventArgs e)
        {
            // 如果是右键：手动选中当前鼠标下的项，不触发拖拽
            if (e.Button == MouseButtons.Right)
            {
                int index = lstTrips.IndexFromPoint(e.Location);
                if (index >= 0 && index < lstTrips.Items.Count)
                {
                    lstTrips.SelectedIndex = index;
                }
                return; // 直接返回，让 ContextMenu 正常弹出
            }

            // 如果是左键：才开始拖拽
            if (e.Button == MouseButtons.Left)
            {
                if (lstTrips.SelectedItem == null) return;
                lstTrips.DoDragDrop(lstTrips.SelectedIndex, DragDropEffects.Move);
            }
        }

        private void LstTrips_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void LstTrips_DragDrop(object sender, DragEventArgs e)
        {
            Point point = lstTrips.PointToClient(new Point(e.X, e.Y));
            int index = lstTrips.IndexFromPoint(point);
            if (index < 0) index = _tripHistory.Count - 1;

            int oldIndex = (int)e.Data.GetData(typeof(int));

            if (oldIndex != index)
            {
                TripArchiveItem item = _tripHistory[oldIndex];
                _tripHistory.RemoveAt(oldIndex);
                _tripHistory.Insert(index, item);

                lstTrips.SelectedIndex = index;
                lstTrips.Invalidate();
                UpdateMap();
            }
        }

        private void DeleteSelectedTrip()
        {
            if (lstTrips.SelectedItem != null)
            {
                _tripHistory.Remove((TripArchiveItem)lstTrips.SelectedItem);
                UpdateMap();
            }
        }

        private void ClearAllTrips()
        {
            if (MessageBox.Show("确定要清空所有行程记录吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _tripHistory.Clear();
                lblStats.Text = "记录已清空";
                UpdateMap();
            }
        }

        private void btnAddTrip_Click(object sender, EventArgs e)
        {
            if (cbStartStop.SelectedItem == null || cbEndStop.SelectedItem == null)
            {
                MessageBox.Show("请选择完整的起止站点！");
                return;
            }

            string route = cbRoutes.Text;
            string dir = cbDirection.SelectedItem.ToString();
            string start = cbStartStop.SelectedItem.ToString();
            string end = cbEndStop.SelectedItem.ToString();

            XLineSpatial tripGeometry = _calculator.ReconstructTrip(route, dir, start, end);

            if (tripGeometry != null)
            {
                var stats = _calculator.AnalyzeDistricts(tripGeometry, districtLayer);

                string report = $"新增行程：{start} -> {end}\n";
                foreach (var kvp in stats) report += $"{kvp.Key}: {kvp.Value}站 ";
                lblStats.Text = report;

                var newItem = new TripArchiveItem(route, start, end, tripGeometry);
                _tripHistory.Add(newItem);

                // 自动滚动到底部
                lstTrips.SelectedIndex = _tripHistory.Count - 1;
                UpdateMap();
            }
        }

        // ==========================================
        // 【核心修改点 2】 地图绘制：始终高亮最后一条
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

                if (tiandituLayer != null)
                {
                    tiandituLayer.Draw(g, view);
                }

                // 2. 画行政区 (修改样式为透明填充)
                if (districtLayer != null)
                {
                    // 临时修改样式：空心填充，灰色边框
                    XThematic transparentStyle = new XThematic(
                        new Pen(Color.FromArgb(100, 0, 20, 80), 1),
                        new Pen(Color.FromArgb(100, 0, 20, 80), 1), // <--- 这里控制面要素的边框粗细
                        new SolidBrush(Color.FromArgb(30, 200, 200, 200)), // 填充变得更透明一点(30)
                        new Pen(Color.Black), new SolidBrush(Color.Black), 2);

                    // 强制使用这个透明样式绘制所有要素
                    for (int i = 0; i < districtLayer.FeatureCount(); i++)
                    {
                        if (districtLayer.GetFeature(i).spatial.extent.IntersectOrNot(view.CurrentMapExtent))
                            districtLayer.GetFeature(i).draw(g, view, false, 0, transparentStyle);
                    }
                }

                // 2. 确定高亮对象：列表中的最后一个元素（序号最大的）
                TripArchiveItem highlightItem = null;
                if (_tripHistory.Count > 0)
                {
                    highlightItem = _tripHistory[_tripHistory.Count - 1];
                }

                // 3. 绘制行程
                XThematic normalStyle = new XThematic();
                normalStyle.LinePen = new Pen(Color.CornflowerBlue, 2);

                // 先画背景层（所有非高亮的）
                foreach (var item in _tripHistory)
                {
                    if (item != highlightItem)
                    {
                        item.Geometry.draw(g, view, normalStyle);
                    }
                }

                // 后画高亮层（红粗线）
                if (highlightItem != null)
                {
                    XThematic highlightStyle = new XThematic();
                    highlightStyle.LinePen = new Pen(Color.Red, 4);
                    highlightItem.Geometry.draw(g, view, highlightStyle);

                    // 画起终点
                    var points = highlightItem.Geometry.vertexes;
                    if (points.Count > 0)
                    {
                        Point pStart = view.ToScreenPoint(points[0]);
                        Point pEnd = view.ToScreenPoint(points.Last());
                        g.FillEllipse(Brushes.Green, pStart.X - 5, pStart.Y - 5, 10, 10);
                        g.FillEllipse(Brushes.Red, pEnd.X - 5, pEnd.Y - 5, 10, 10);
                    }
                }
            }
            // 触发 Panel2 重绘
            try { splitContainer1.Panel2.Invalidate(); } catch { }
        }

        // ==========================================
        // 地图交互部分（保持之前的优化：双缓冲+偏移绘制）
        // ==========================================
        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow == null) return;
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

        private void MapPanel_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { MouseDownLocation = e.Location; currentMouseAction = XExploreActions.pan; } }
        private void MapPanel_MouseMove(object sender, MouseEventArgs e)
        {
            XVertex v = view.ToMapVertex(e.Location);
            labelXY.Text = $"X: {v.x:F3}, Y: {v.y:F3}";
            if (e.Button == MouseButtons.Left && currentMouseAction == XExploreActions.pan)
            {
                MouseMovingLocation = e.Location;
                splitContainer1.Panel2.Invalidate();
            }
        }
        private void MapPanel_MouseUp(object sender, MouseEventArgs e) { if (currentMouseAction == XExploreActions.pan) { view.OffsetCenter(view.ToMapVertex(MouseDownLocation), view.ToMapVertex(e.Location)); UpdateMap(); } currentMouseAction = XExploreActions.noaction; }
        private void MapPanel_MouseWheel(object sender, MouseEventArgs e) { if (e.Delta > 0) view.ChangeView(XExploreActions.zoomin); else view.ChangeView(XExploreActions.zoomout); UpdateMap(); }

        // 这里的 MapTools_Click 是通用的，请确保 Form1.Designer.cs 里全图按钮绑定的是这个事件
        private void MapTools_Click(object sender, EventArgs e) { if (sender == bZoomIn) view.ChangeView(XExploreActions.zoomin); else if (sender == bZoomOut) view.ChangeView(XExploreActions.zoomout); else if (sender == bFullExtent && districtLayer != null) view.Update(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle); UpdateMap(); }
        private void MapPanel_SizeChanged(object sender, EventArgs e) { UpdateMap(); }

        // ==========================================
        // 杂项
        // ==========================================
        private void InitRouteSearch()
        {
            cbRoutes.DropDownStyle = ComboBoxStyle.DropDown;
            cbRoutes.TextUpdate += CbRoutes_TextUpdate;
        }

        private void CbRoutes_TextUpdate(object sender, EventArgs e)
        {
            string input = cbRoutes.Text;
            if (input.Length < 2)
            {
                if (cbRoutes.Items.Count > 0)
                {
                    cbRoutes.Items.Clear();
                    cbRoutes.DroppedDown = false;
                    cbRoutes.SelectionStart = input.Length;
                }
                return;
            }

            var matches = _dataManager.AllRoutes
                .Where(r => r.RouteName.Contains(input))
                .Select(r => r.RouteName).Distinct().Take(20).ToArray();

            cbRoutes.Items.Clear();
            if (matches.Length > 0)
            {
                cbRoutes.Items.AddRange(matches);
                cbRoutes.DroppedDown = true;
            }
            else
            {
                cbRoutes.DroppedDown = false;
            }
            cbRoutes.SelectionStart = input.Length;
        }

        private void LoadBasemap()
        {
            try
            {
                string shpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "shanghai_district.shp");
                if (File.Exists(shpPath))
                {
                    districtLayer = XShapefile.ReadShapefile(shpPath);
                    districtLayer.LabelOrNot = false;
                    districtLayer.UnselectedThematic = new XThematic(new Pen(Color.LightGray, 1), new Pen(Color.LightGray, 1), new SolidBrush(Color.WhiteSmoke), new Pen(Color.Black), new SolidBrush(Color.Black), 2);
                    view = new XView(districtLayer.Extent, splitContainer1.Panel2.ClientRectangle);
                }
                else
                {
                    view = new XView(new XExtent(0, 100, 0, 100), splitContainer1.Panel2.ClientRectangle);
                }
            }
            catch { }
        }

        private void LoadBusData() { _dataManager.LoadAllData(); }

        private void cbRoutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbRoutes.SelectedItem == null) return;
            cbDirection.Items.Clear(); cbStartStop.Items.Clear(); cbEndStop.Items.Clear();
            string selectedRoute = cbRoutes.SelectedItem.ToString();
            var directions = _dataManager.AllRoutes.Where(r => r.RouteName == selectedRoute).Select(r => r.Direction).ToList();
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
                List<string> stops = _dataManager.RoutePaths[key];
                cbStartStop.Items.AddRange(stops.ToArray());
                cbEndStop.Items.AddRange(stops.ToArray());
                if (cbStartStop.Items.Count > 0) { cbStartStop.SelectedIndex = 0; cbEndStop.SelectedIndex = cbEndStop.Items.Count - 1; }
            }
        }
    }
}