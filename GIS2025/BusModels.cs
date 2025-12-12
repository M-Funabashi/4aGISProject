using System;
using System.Collections.Generic;
using XGIS; // 引用你的底层 GIS 库
using Newtonsoft.Json; // 【新增】

namespace GIS2025
{
    /// <summary>
    /// 公交站点实体类 (对应 stops_geo.csv)
    /// </summary>
    public class BusStop
    {
        public string Name { get; set; }      // 站点名称
        public string District { get; set; }  // 行政区
        public string Street { get; set; }    // 街道
        public XVertex Location { get; set; } // 坐标 (X, Y)

        public BusStop(string name, string district, string street, double x, double y)
        {
            Name = name;
            District = district;
            Street = street;
            Location = new XVertex(x, y);
        }
    }

    /// <summary>
    /// 线路基础信息 (对应 routes.csv)
    /// 用于下拉菜单显示
    /// </summary>
    public class BusRouteInfo
    {
        public string RouteName { get; set; } // 线路名 (如 "1路")
        public string Direction { get; set; } // 走向 (如 "上行")
        public int StopCount { get; set; }    // 站点数

        // 方便 UI 显示的属性：返回 "1路 (上行)"
        public string DisplayName
        {
            get { return $"{RouteName} ({Direction})"; }
        }

        // 生成唯一的 Key，用于在字典中查找
        public string GetKey()
        {
            return $"{RouteName}_{Direction}";
        }
    }
}

// 在 GIS2025 命名空间内，BusRouteInfo 类下方添加：

/// <summary>
/// 行程档案项：将显示信息与几何对象打包
/// </summary>
public class TripArchiveItem
{
    public string RouteName { get; set; }
    public string Direction { get; set; }
    public string StartStop { get; set; }
    public string EndStop { get; set; }

    // 【关键修改】 Geometry 对象比较复杂，不适合直接存 JSON
    // 我们加一个 [JsonIgnore] 标签，让保存时忽略它
    [JsonIgnore]
    public XLineSpatial Geometry { get; set; }

    // 【新增】 专门用于存取数据的“影子属性”
    // 保存时：把 Geometry 里的点取出来变成 List
    // 读取时：把 List 塞回去重建 XLineSpatial 对象

    public TripArchiveItem() { } // 序列化需要无参构造函数

    public TripArchiveItem(string route, string dir, string start, string end, XLineSpatial line)
    {
        RouteName = route;
        Direction = dir;
        StartStop = start;
        EndStop = end;
        Geometry = line;
    }

    public override string ToString()
    {
        return $"{RouteName}: {StartStop} -> {EndStop}";
    }
}