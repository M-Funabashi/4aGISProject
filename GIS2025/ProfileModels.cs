using System;
using System.Collections.Generic;

namespace GIS2025
{
    // ==========================================
    // 1. 系统内部使用的运行时模型 (Runtime Models)
    // ==========================================

    /// <summary>
    /// 用户资料 (根节点)
    /// </summary>
    public class UserProfile
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "新用户";
        public string AvatarPath { get; set; } // 头像本地路径

        // 用户拥有的所有档案
        public List<DailyArchive> Archives { get; set; } = new List<DailyArchive>();

        public UserProfile() { }
        public UserProfile(string name) { Name = name; }
    }

    /// <summary>
    /// 每日档案 (二级节点)
    /// </summary>
    public class DailyArchive
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        // 档案内的所有行程
        public List<TripArchiveItem> Trips { get; set; } = new List<TripArchiveItem>();

        public DailyArchive() { }
        public DailyArchive(string name) { Name = name; }
    }

    // ==========================================
    // 2. .trj 文件专用模型 (用于导入导出)
    // ==========================================

    /// <summary>
    /// .trj 文件根结构
    /// </summary>
    public class TrjFileModel
    {
        public string ArchiveName { get; set; } // 档案名
        public DateTime CreateTime { get; set; } // 创建时间
        public string Author { get; set; } // 作者(可选)
        public List<TrjTripItem> Trips { get; set; } = new List<TrjTripItem>();
    }

    /// <summary>
    /// .trj 单条行程记录 (只存元数据，不存坐标)
    /// </summary>
    public class TrjTripItem
    {
        public int Sequence { get; set; }
        public string RouteName { get; set; } // 如 "1路"
        public string Direction { get; set; } // 如 "上行"
        public string StartStop { get; set; }
        public string EndStop { get; set; }
    }
}