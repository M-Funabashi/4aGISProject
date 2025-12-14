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
        // 站点字典 (Key: 站点名, Value: 站点对象)
        public Dictionary<string, BusStop> AllStops = new Dictionary<string, BusStop>();

        // 线路轨迹字典 (Key: "线路名_方向", Value: 有序的站点名列表)
        public Dictionary<string, List<string>> RoutePaths = new Dictionary<string, List<string>>();

        // 线路列表
        public List<BusRouteInfo> AllRoutes = new List<BusRouteInfo>();

        // 站点-线路索引 (Key: 站点名, Value: 经过该站的所有线路名列表)
        public Dictionary<string, List<string>> StopToRoutes = new Dictionary<string, List<string>>();

        // Key: 线路名称, Value: 该线路对应的所有几何线
        public Dictionary<string, List<XLineSpatial>> RealRouteGeometries = new Dictionary<string, List<XLineSpatial>>();

        // 加载逻辑
        // 主加载方法，程序启动时调用
        public void LoadAllData()
        {
            try
            {
                // 获取运行目录下的data文件夹
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

        // 读取站点坐标 stops_geo.csv
        private void LoadStops(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); 
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(',');
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

        // 读取线路基础信息 routes.csv
        private void LoadRoutes(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); 
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(',');
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

        // 读取线路-站点轨迹 route_stops.csv
        private void LoadRouteStops(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine(); 
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(',');
                    if (parts.Length >= 4)
                    {
                        string routeName = parts[0];
                        string direction = parts[1];
                        string stopName = parts[3];
                        string key = $"{routeName}_{direction}"; 
                        if (!RoutePaths.ContainsKey(key))
                        {
                            RoutePaths[key] = new List<string>();
                        }
                        RoutePaths[key].Add(stopName);
                    }
                }
            }
        }

        // 读取站点-线路对应关系 stop_routes.csv
        private void LoadStopRoutes(string path)
        {
            if (!File.Exists(path)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(',');
                    if (parts.Length >= 1)
                    {
                        string stopName = parts[0];
                        List<string> routes = new List<string>();

                        // 遍历后面的所有线路
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
                    string rawName = f.getAttribute(0).ToString();

                    // 名称清洗
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
            catch (Exception) {}
        }
    }
}