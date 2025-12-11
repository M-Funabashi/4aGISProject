using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using XGIS;

namespace GIS2025
{
    public class BusDataManager
    {
        // ==========================================
        // 核心数据仓库
        // ==========================================

        // 1. 所有站点字典 (Key: 站点名, Value: 站点对象)
        // 作用：通过名字快速查坐标
        public Dictionary<string, BusStop> AllStops = new Dictionary<string, BusStop>();

        // 2. 线路轨迹字典 (Key: "线路名_方向", Value: 有序的站点名列表)
        // 作用：知道 "1路_上行" 依次经过哪些站
        public Dictionary<string, List<string>> RoutePaths = new Dictionary<string, List<string>>();

        // 3. 所有线路列表
        // 作用：填充 UI 的下拉菜单
        public List<BusRouteInfo> AllRoutes = new List<BusRouteInfo>();

        // 4. 站点-线路索引 (Key: 站点名, Value: 经过该站的所有线路名列表)
        // 作用：查询某站能坐哪些车
        public Dictionary<string, List<string>> StopToRoutes = new Dictionary<string, List<string>>();

        // 1. 在类成员变量区域添加：
        // Key: 线路名称 (如 "1路"), Value: 该线路对应的所有几何线 (因为可能有上行和下行两条线，或者破碎的多段线)
        public Dictionary<string, List<XLineSpatial>> RealRouteGeometries = new Dictionary<string, List<XLineSpatial>>();



        // ==========================================
        // 加载逻辑
        // ==========================================

        /// <summary>
        /// 主加载方法，程序启动时调用
        /// </summary>
        public void LoadAllData()
        {
            try
            {
                // 获取 exe 运行目录下的 data 文件夹
                string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

                if (!Directory.Exists(dataPath))
                {
                    MessageBox.Show($"未找到数据文件夹：{dataPath}\n请确保已将 data 文件夹复制到输出目录。", "数据缺失", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LoadStops(Path.Combine(dataPath, "stops_geo.csv"));
                LoadRoutes(Path.Combine(dataPath, "routes.csv"));
                LoadRouteStops(Path.Combine(dataPath, "route_stops.csv"));
                LoadStopRoutes(Path.Combine(dataPath, "stop_routes.csv"));
                LoadRealBusLines(Path.Combine(dataPath, "shanghai_busline.shp"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据加载失败：" + ex.Message);
            }
        }

        // 1. 读取站点坐标 (stops_geo.csv)
        private void LoadStops(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); // 跳过表头
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(',');

                    // 格式: StopName, District, Street, X, Y
                    if (parts.Length >= 5)
                    {
                        string name = parts[0];
                        string district = parts[1];
                        string street = parts[2];
                        double x = double.Parse(parts[3]);
                        double y = double.Parse(parts[4]);

                        if (!AllStops.ContainsKey(name))
                        {
                            AllStops.Add(name, new BusStop(name, district, street, x, y));
                        }
                    }
                }
            }
        }

        // 2. 读取线路基础信息 (routes.csv)
        private void LoadRoutes(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); // 跳过表头
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(',');

                    // 格式: RouteName, Direction, StopCount
                    if (parts.Length >= 3)
                    {
                        BusRouteInfo info = new BusRouteInfo
                        {
                            RouteName = parts[0],
                            Direction = parts[1],
                            StopCount = int.Parse(parts[2])
                        };
                        AllRoutes.Add(info);
                    }
                }
            }
        }

        // 3. 读取线路-站点轨迹 (route_stops.csv)
        private void LoadRouteStops(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); // 跳过表头
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(',');

                    // 格式: RouteName, Direction, Sequence, StopName
                    if (parts.Length >= 4)
                    {
                        string routeName = parts[0];
                        string direction = parts[1];
                        string stopName = parts[3];

                        string key = $"{routeName}_{direction}"; // 组合键

                        if (!RoutePaths.ContainsKey(key))
                        {
                            RoutePaths[key] = new List<string>();
                        }
                        RoutePaths[key].Add(stopName);
                    }
                }
            }
        }

        // 4. 读取站点-线路对应关系 (stop_routes.csv)
        // 这是一个变长 CSV，需要特殊处理
        private void LoadStopRoutes(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); // 跳过表头
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(',');

                    // 格式: StopName, Route1, Route2, ...
                    if (parts.Length >= 1)
                    {
                        string stopName = parts[0];
                        List<string> routes = new List<string>();

                        // 从第1个元素开始遍历后面的所有线路
                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(parts[i]))
                            {
                                routes.Add(parts[i]);
                            }
                        }

                        if (!StopToRoutes.ContainsKey(stopName))
                        {
                            StopToRoutes.Add(stopName, routes);
                        }
                    }
                }
            }
        }

        private void LoadRealBusLines(string shpPath)
        {
            if (!File.Exists(shpPath)) return;
            try
            {
                XVectorLayer lineLayer = XShapefile.ReadShapefile(shpPath);

                for (int i = 0; i < lineLayer.FeatureCount(); i++)
                {
                    XFeature f = lineLayer.GetFeature(i);

                    // 1. 【修改】使用你刚才找到的正确索引 (这里假设是 INDEX，请替换为你的数字)1
                    // 假如你发现名字在第 3 列，就写 getAttribute(3)
                    string rawName = f.getAttribute(0).ToString();

                    // 2. 【新增】名称清洗 (去除多余的空格、括号等，提高匹配率)
                    // 这样 "1路(上行)" 和 "1路" 都能被识别为 "1路"
                    string routeName = rawName.Split('(', '（')[0].Trim();

                    if (!RealRouteGeometries.ContainsKey(routeName))
                    {
                        RealRouteGeometries[routeName] = new List<XLineSpatial>();
                    }

                    if (f.spatial is XLineSpatial line)
                    {
                        RealRouteGeometries[routeName].Add(line);
                    }
                }
            }
            catch (Exception) { /* 忽略错误，防止底图损坏影响主流程 */ }
        }
    }
}