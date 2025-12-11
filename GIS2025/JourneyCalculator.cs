using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using XGIS; // 引用底层 GIS 库

namespace GIS2025
{
    /// <summary>
    /// 核心业务逻辑：负责计算行程轨迹和行政区分析
    /// </summary>
    public class JourneyCalculator
    {
        private BusDataManager _dataManager;

        public JourneyCalculator(BusDataManager dataManager)
        {
            _dataManager = dataManager;
        }

        /// <summary>
        /// 1. 轨迹重建算法
        /// 根据用户选择的线路和起止站，构建一条地理空间线段 (XLineSpatial)
        /// </summary>
        public XLineSpatial ReconstructTrip(string routeName, string direction, string startStop, string endStop)
        {
            // 1. 获取起止站点对象
            if (!_dataManager.AllStops.ContainsKey(startStop) || !_dataManager.AllStops.ContainsKey(endStop))
            {
                MessageBox.Show("站点坐标缺失！");
                return null;
            }
            XVertex pStart = _dataManager.AllStops[startStop].Location;
            XVertex pEnd = _dataManager.AllStops[endStop].Location;

            // ==========================================
            // 尝试方案 A: 基于真实路网截取 (True Geometry)
            // ==========================================
            if (_dataManager.RealRouteGeometries.ContainsKey(routeName))
            {
                List<XLineSpatial> candidates = _dataManager.RealRouteGeometries[routeName];

                // 难点：Shapefile 里可能有好几条叫 "1路" 的线（比如上行一条、下行一条，或者碎线）
                // 我们需要找到那条“离起点和终点都很近”的线

                XLineSpatial bestLine = null;
                double minTotalDist = double.MaxValue;

                foreach (var line in candidates)
                {
                    // 计算起点和终点到这条线的距离
                    line.GetClosestPointInfo(pStart, out _, out _, out double d1);
                    line.GetClosestPointInfo(pEnd, out _, out _, out double d2);

                    // 如果两个点离这条线都比较近 (比如 < 500米，这里用地图单位，假设是经纬度，大约 0.005)
                    // 简单起见，取距离之和最小的那条线
                    if (d1 + d2 < minTotalDist)
                    {
                        minTotalDist = d1 + d2;
                        bestLine = line;
                    }
                }

                // 如果找到了合适的线，且距离在合理范围内 (防止匹配到十万八千里外的同名线路)
                // 0.01 度大约是 1公里
                if (bestLine != null && minTotalDist < 0.02)
                {
                    // 执行截取！
                    return bestLine.ExtractSection(pStart, pEnd);
                }
            }

            // ==========================================
            // 尝试方案 B: 降级方案 (Connect Stops)
            // 如果没有真实路网，或者匹配失败，就用原来的“连点成线”
            // ==========================================
            string key = $"{routeName}_{direction}";
            if (!_dataManager.RoutePaths.ContainsKey(key)) return null;

            List<string> allStops = _dataManager.RoutePaths[key];
            int startIndex = allStops.IndexOf(startStop);
            int endIndex = allStops.IndexOf(endStop);

            if (startIndex > endIndex) return null; // 简单校验

            List<XVertex> points = new List<XVertex>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                string name = allStops[i];
                if (_dataManager.AllStops.ContainsKey(name))
                {
                    // 必须深拷贝点，防止绘图修改原数据
                    XVertex v = _dataManager.AllStops[name].Location;
                    points.Add(new XVertex(v.x, v.y));
                }
            }

            if (points.Count < 2) return null;
            return new XLineSpatial(points);
        }

        /// <summary>
        /// 2. 行政区分析算法
        /// 计算这条轨迹经过了哪些行政区，以及经过的站点数量
        /// </summary>
        /// <param name="tripLine">生成的轨迹线</param>
        /// <param name="districtLayer">行政区图层 (shanghai_district.shp)</param>
        /// <returns>字典：行政区名 -> 经过的站点数</returns>
        public Dictionary<string, int> AnalyzeDistricts(XLineSpatial tripLine, XVectorLayer districtLayer)
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            // 遍历轨迹上的每一个点（站点）
            foreach (XVertex p in tripLine.vertexes)
            {
                // 遍历每一个行政区面
                for (int i = 0; i < districtLayer.FeatureCount(); i++)
                {
                    XFeature districtFeature = districtLayer.GetFeature(i);

                    // 利用底层 Distance 方法判断点是否在面内
                    // Distance <= 0 表示在面内或边界上
                    if (districtFeature.spatial.Distance(p) <= 0)
                    {
                        // 获取行政区名称 (假设是第1个字段，根据你的shp实际情况调整)
                        // 通常 shapfile 第一个字段是 Name
                        string districtName = districtFeature.getAttribute(0).ToString();

                        if (stats.ContainsKey(districtName))
                            stats[districtName]++;
                        else
                            stats[districtName] = 1;

                        // 一个点只能属于一个区，找到后就可以跳出内层循环
                        break;
                    }
                }
            }

            return stats;
        }
    }
}