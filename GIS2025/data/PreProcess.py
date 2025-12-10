import pandas as pd
import geopandas as gpd

# ==========================================
# 1. 读取原始数据
# ==========================================
print("正在读取数据...")

# 读取 Excel (线路站点关系)
df_raw = pd.read_excel('line2stop.xlsx')

# 读取 Shapefile (站点位置信息)
gdf_stops = gpd.read_file('shanghai_busstop.shp')

# ==========================================
# 2. 数据清洗 (剔除 "D" 站点)
# ==========================================
print("正在清洗数据 (剔除异常值 'D')...")
# 剔除站点名称为 "D" 的行
df_clean = df_raw[df_raw['站点名称'] != 'D'].copy()

# ==========================================
# 3. 生成线路-站点顺序表 (route_stops.csv)
# ==========================================
print("正在生成线路顺序表 (route_stops.csv)...")

# 确保站序是数字
df_clean['站序'] = pd.to_numeric(df_clean['站序'], errors='coerce')

# 排序：线路名 -> 走向 -> 站序
df_sorted = df_clean.sort_values(by=['线路名称', '走向', '站序'], ascending=[True, True, True])

# 重置站序 (防止出现 1, 3, 4 断号)
df_sorted['NewSequence'] = df_sorted.groupby(['线路名称', '走向']).cumcount() + 1

# 导出
df_route_stops_export = df_sorted[['线路名称', '走向', 'NewSequence', '站点名称']]
df_route_stops_export.columns = ['RouteName', 'Direction', 'Sequence', 'StopName']
df_route_stops_export.to_csv('route_stops.csv', index=False, encoding='utf-8-sig')

# ==========================================
# 4. 生成线路汇总表 (routes.csv)
# ==========================================
print("正在生成线路汇总表 (routes.csv)...")
df_routes_export = df_route_stops_export.groupby(['RouteName', 'Direction']).size().reset_index(name='StopCount')
df_routes_export.to_csv('routes.csv', index=False, encoding='utf-8-sig')

# ==========================================
# 5. 生成站点坐标表 (stops_geo.csv) - 用于 GIS 画图
# ==========================================
print("正在生成站点坐标表 (stops_geo.csv)...")

gdf_stops['X'] = gdf_stops.geometry.x
gdf_stops['Y'] = gdf_stops.geometry.y

# 这里保留坐标和行政区，专门用于地图打点
# 请根据实际 SHP 字段名修改 '站点名称', '所在行政区', '街道名称'
df_geo_export = gdf_stops[['StationNam', 'AREA', 'STREET', 'X', 'Y']].copy()
df_geo_export.columns = ['StopName', 'District', 'Street', 'X', 'Y']
df_geo_export = df_geo_export.drop_duplicates(subset=['StopName']) # 去重

df_geo_export.to_csv('stops_geo.csv', index=False, encoding='utf-8-sig')

# ... (前面的代码保持不变) ...

# ==========================================
# 6. 生成站点-线路对应表 (stop_routes.csv) - [优化版：无尾部逗号]
# ==========================================
print("正在生成站点-线路对应表 (stop_routes.csv)...")

# 逻辑：按站点分组，获取经过该站点的所有线路
# stop_groups 是一个 Series，索引是站点名，值是线路名列表(numpy array)
stop_groups = df_clean.groupby('站点名称')['线路名称'].unique()

# 手动写入文件，实现“变长”格式
with open('stop_routes.csv', 'w', encoding='utf-8-sig') as f:
    # 写入表头 (因为每行长度不一样，表头其实只起个标识作用)
    f.write("StopName,Routes...\n")
    
    # 遍历每个站点
    for stop_name, routes in stop_groups.items():
        # routes 是一个列表，我们用逗号把它拼起来
        # routes_str 结果类似: "1路,2路,3路"
        routes_str = ",".join(routes)
        
        # 写入一行: 站点名,线路1,线路2...
        f.write(f"{stop_name},{routes_str}\n")

print("="*30)
print("处理完成！stop_routes.csv 现在已去除了多余逗号。")
# ... (后面的打印说明保持不变) ...

print("="*30)
print("处理完成！生成文件说明：")
print("1. [route_stops.csv]: 线路画线用 (有序，无D站)")
print("2. [routes.csv]:      线路下拉菜单用")
print("3. [stops_geo.csv]:   地图画点用 (含X,Y坐标)")
print("4. [stop_routes.csv]: 站点查询用 (A站, 1路, 2路...)")