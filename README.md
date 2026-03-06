# Zombie Escape Practice 插件

**Zombie Escape Practice** 是一个为 CS2 Zombie Escape 模式中的弹幕图、跳刀图设计的练习插件，允许玩家通过简单的菜单命令直接跳转到地图中的特定节点（跳刀、弹幕、BOSS 战），跳过冗长的跑图流程。基于 CounterStrikeSharp 开发，配置灵活，支持创意工坊地图 ID 自动识别，并提供命令块（重复、随机、延时）和投票功能。

## ✨ 特性

- **快速跳转**：一键传送所有玩家到配置好的位置，并可设置视角朝向。
- **灵活配置**：每个地图可配置多个训练节点，支持自定义触发命令，支持混合延时、重复、随机执行。
- **动态命令块**：可为命令块指定 `targetname`，通过 `!zep` 命令动态启用/禁用/触发。
- **投票功能**：内置投票系统，支持 `!vote_for` 和 `!vote_execute`。
- **功能开关**：可通过插件配置文件 `ZEPconfig.json` 单独启用/关闭练习、投票、调试功能。
- **地图识别**：优先使用创意工坊地图 ID 作为配置键，兼容完整地图名。

## 📦 安装

1. **安装依赖**：确保服务器已正确安装最新版 [Metamod: Source](https://sourcemm.net/downloads.php?branch=dev) 和 [CounterStrikeSharp](https://docs.cssharp.dev/)。
2. **下载插件**：从 [Releases](https://github.com/Lielinex/CS2-ZombieEscapePractice/releases) 下载最新版的 `ZombieEscapePractice.zip`。
3. **放置文件**：将 `ZombieEscapePractice.zip` 解压后放入服务器的 `csgo/addons/counterstrikesharp/plugins/` 目录。
4. **启动服务器**：首次启动会自动生成插件配置文件夹和示例地图配置文件。

**可选依赖**：很多 Zombie Escape 地图需要 [CS2Fixes](https://github.com/Source2ZE/CS2Fixes) 插件才能正常运行（若使用我提供的配置文件则需要此插件）。

## ⚙️ 配置

### 1. 插件功能配置
位于 `csgo/addons/counterstrikesharp/configs/plugins/ZombieEscapePractice/ZEPconfig.json`：
```json
{
  "EnablePractice": true,   // 开启练习菜单 (!prac)
  "EnableVote": true,       // 开启投票功能 (!vote_for, !vote_execute)
  "EnableDebug": false      // 调试模式：开启后玩家可使用 !zep 控制命令块；关闭后仅服务器控制台可用
}
```
### 2. 地图配置
地图配置文件存放在 csgo/addons/counterstrikesharp/configs/plugins/ZombieEscapePractice/maps/ 下，每个地图一个 JSON 文件，文件名任意（便于识别），但文件内必须以地图标识符（Workshop ID 或完整地图名）为键。基本结构如下：
```json
{
  "ze_example_map": [          // 地图名（全名）或 Workshop ID
    {
      "name": "example",
      "position": { "x": -1234.5, "y": 567.8, "z": 900.1 },
      "angle": { "pitch": 0, "yaw": 90, "roll": 0 },
      "blocks": [               // 命令块列表（顺序执行）
        {
          "type": "once",
          "commands": [
            "ent_fire knife_spawner enable",
            "[delay=2]say 2 sec"
          ]
        },
        {
          "type": "delay",
          "interval": 5,
          "commands": [ "say execute commands after 5 sec" ]
        }
      ]
    },
    {
      "name": "example_repeat",
      "blocks": [
        {
          "targetname": "skill_repeat",   // 可命名，用于动态控制
          "type": "repeat",
          "interval": 2,
          "count": 10,                     // 重复10次，省略或 -1 表示无限
          "commands": [ "c_entfire skill1" ]
        },
        {
          "targetname": "random_skill",
          "type": "random",
          "mode": "PickRandomShuffle",      // 或 "PickRandom"
          "commands": [ "say skillA", "say skillB", "say skillC" ]
        }
      ]
    }
  ]
}
```
块类型说明
once：普通命令块，commands 中的每条命令执行一次，支持 [delay=X] 前缀。

delay：统一延迟块，内部所有命令在 interval 秒后执行。

repeat：重复执行块，enable 后每隔 interval 秒执行一次内部命令，count 为重复次数（-1 或省略为无限）。

random：随机选择块，mode 为 PickRandom（每次随机）或 PickRandomShuffle（洗牌循环），每次从 commands 中选一条执行。

所有块均可选 targetname 用于动态控制，以及 startdisabled 属性（默认 false）控制初始状态。

## 🎮 使用命令

练习功能
!practice 或 !prac — 打开当前地图的训练菜单（需要 EnablePractice: true）。

命令块调试
!zep enable <targetname> — 启用指定的命令块（若为重复块则开始定时执行）。

!zep disable <targetname> — 禁用指定的命令块（若为重复块则停止定时器）。

!zep trigger <targetname> — 触发一次随机块（仅对 random 类型有效）。

注意：当 EnableDebug 为 false 时，玩家无法使用 !zep 命令，但服务器控制台仍可使用css_zep <command>控制命令块。

投票功能
!vote_for <内容> — 发起一个是否投票，内容会显示在投票界面。

!vote_execute "<命令>" [说明] — 发起一个是否执行指定命令的投票，通过后服务器执行命令。命令需用引号括起（支持单引号或双引号），说明可选。

服务器控制台也可使用上述命令（前缀改为 css_，例如 css_vote_execute "say hello"）。

## 📋 投票所需 ConVar

确保服务器开启以下 ConVar（可在 server.cfg 中添加）：
sv_allow_votes 1
sv_vote_allow_in_warmup 1
sv_vote_allow_spectators 1
sv_vote_count_spectator_votes 1

## 📁 文件结构
```json
csgo/addons/counterstrikesharp/
├── plugins/
│   └── ZombieEscapePractice/
│       └── ZombieEscapePractice.dll
└── configs/
    └── plugins/
        └── ZombieEscapePractice/
            ├── ZEPconfig.json               # 插件功能配置
            └── maps/                         # 地图配置文件
                ├── ze_example.json
                ├── luciddream.json
                └── ...
```
## ⚠️ 注意事项

服务器和客户端都需要有对应的resource/platform_<language>本地化文件支持投票界面显示（简体中文为platform_schinese.txt，放置于game/csgo/resource文件夹中）。
我还在验证通过创意工坊订阅获取本地化键的可行性，若不可行，我可能会大幅修改投票功能。

坐标获取：游戏内开启控制台输入 cl_showpos 1 可查看当前位置。

创意工坊 ID：可在控制台输入 host_workshop_map 查看当前地图 ID。

命令块名称：targetname 在配置文件中必须唯一，否则后加载的会覆盖前者。

完成地图练习配置后可在服务器控制台输入css_plugins_reload ZombieEscapePractice重新加载配置。

定时器清理：每回合结束会自动取消所有未执行的延时和重复定时器，确保下回合不受干扰。

## 参考项目

[SLAYER_PanoramaVote](https://github.com/zakriamansoor47/SLAYER_PanoramaVote)

## 📄 许可证

本项目使用 MIT 许可证。详情请参见 [LICENSE](https://github.com/Lielinex/CS2-ZombieEscapePractice/tree/master?tab=MIT-1-ov-file) 文件。

祝您早日成为ZE大神！ 🎉
