using System;
using System.Collections.Generic;
using Newtonsoft.Json; // 【必须引用】用于标记 [JsonIgnore]
using XGIS;

namespace GIS2025
{
    /// <summary>
    /// 用户配置文件 (对应 user/xxx/info.json)
    /// </summary>
    public class UserProfile
    {
        public string Name { get; set; }
        public string AvatarPath { get; set; }

        // 【新增】累计里程缓存 (单位: km)
        // 作用：登录界面只读 info.json 就能显示里程，不用去读那几十个行程文件，速度极快
        public double TotalDistance { get; set; } = 0;

        // 【关键修改】加上 [JsonIgnore] 标签
        // 作用：保存 info.json 时，忽略 Archives 列表。
        // Archives 列表现在由 ProfileManager 扫描文件夹里的 .trj 文件来动态填入。
        [JsonIgnore]
        public List<DailyArchive> Archives { get; set; } = new List<DailyArchive>();

        public UserProfile() { }

        public UserProfile(string name)
        {
            Name = name;
        }

        // 辅助方法：遍历内存中的行程，重新计算总里程
        public double RecalculateTotalDistance()
        {
            double totalMeters = 0;
            if (Archives != null)
            {
                foreach (var archive in Archives)
                {
                    foreach (var trip in archive.Trips)
                    {
                        totalMeters += trip.Length;
                    }
                }
            }
            return totalMeters / 1000.0;
        }
    }

    /// <summary>
    /// 每日档案 (对应 user/xxx/档案名.trj)
    /// 这个类基本没变，但它是被单独存成文件的
    /// </summary>
    public class DailyArchive
    {
        public string Name { get; set; }
        public List<TripArchiveItem> Trips { get; set; } = new List<TripArchiveItem>();

        public DailyArchive() { }
        public DailyArchive(string name)
        {
            Name = name;
        }
    }

    // TripArchiveItem 类定义在 BusModels.cs 里，这里不需要动，保持引用即可
}