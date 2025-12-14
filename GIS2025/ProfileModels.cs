using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using XGIS;

namespace GIS2025
{
    // 用户配置文件 info.json
    public class UserProfile
    {
        public string Name { get; set; }
        public string AvatarPath { get; set; }
        // 登录界面只读 info.json显示里程
        public double TotalDistance { get; set; } = 0;
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


    // 每日档案 档案名.trj)
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
}