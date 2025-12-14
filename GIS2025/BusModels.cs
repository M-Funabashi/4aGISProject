using System;
using System.Collections.Generic;
using XGIS; // 引用你的底层 GIS 库
using Newtonsoft.Json; // 【新增】

namespace GIS2025
{
    // 公交站点实体类 (对应 stops_geo.csv)
    public class BusStop
    {
        public string Name { get; set; } // 站点名称
        public string District { get; set; } // 行政区
        public string Street { get; set; }  // 街道
        public XVertex Location { get; set; } // 坐标 (X, Y)

        public BusStop(string name, string district, string street, double x, double y)
        {
            Name = name;
            District = district;
            Street = street;
            Location = new XVertex(x, y);
        }
    }

    // 线路基础信息 (对应 routes.csv)
    public class BusRouteInfo
    {
        public string RouteName { get; set; } // 线路名
        public string Direction { get; set; } // 走向
        public int StopCount { get; set; } // 站点数
        public string DisplayName
        {
            get { return $"{RouteName} ({Direction})"; }
        }

        // 生成唯一的Key，用于在字典中查找
        public string GetKey()
        {
            return $"{RouteName}_{Direction}";
        }
    }

    // 行程档案项：将显示信息与几何对象打包
    public class TripArchiveItem
    {
        public string RouteName { get; set; }
        public string Direction { get; set; }
        public string StartStop { get; set; }
        public string EndStop { get; set; }

        public double Length { get; set; } = 0;
        [JsonIgnore]
        public XLineSpatial Geometry { get; set; }

        // 保存时把 Geometry 里的点取出来变成 List
        // 读取时把 List 塞回去重建 XLineSpatial 对象
        public TripArchiveItem(string route, string dir, string start, string end, XLineSpatial line)
        {
            RouteName = route;
            Direction = dir;
            StartStop = start;
            EndStop = end;
            Geometry = line;
        }
    }
}