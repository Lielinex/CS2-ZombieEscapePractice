# Zombie Escape Practice 插件

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![Metamod: Source](https://img.shields.io/badge/Metamod%3ASource)
![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-1.0+-blue)

**Zombie Escape Practice** 是一个为 CS2 Zombie Escape模式中的弹幕图、跳刀图设计的练习插件，允许玩家通过简单的菜单命令直接跳转到地图中的特定节点（跳刀、弹幕、BOSS 战），跳过冗长的跑图流程。基于 CounterStrikeSharp 开发，配置灵活，支持创意工坊地图 ID 自动识别。

## ✨ 特性

- **快速跳转**：一键传送所有玩家到配置好的挑战位置。
- **灵活配置**：每个地图可配置多个挑战节点，支持自定义触发命令（如激活实体、生成物体）。
- **智能地图识别**：优先使用创意工坊地图 ID 作为配置键，兼容普通地图名。
- **简单易用**：玩家通过聊天框菜单发送!prac或者!practice后选择，无需管理员权限

## 📦 安装

1. **安装依赖**：确保服务器已正确安装最新版[Metamod: Source](https://sourcemm.net/downloads.php?branch=dev)和[CounterStrikeSharp](https://docs.cssharp.dev/) (版本 ≥ 1.0)。
2. **下载插件**：从 [Releases](https://github.com/Lielinex/CS2-ZombieEscapePractice/releases) 下载最新版的 `ZombieEscapePractice.zip`。
3. **放置文件**：将 `ZombieEscapePractice.zip` 解压后放入服务器的 `csgo/addons/counterstrikesharp/plugins/` 目录。
4. **启动服务器**

首次启动后，插件会在插件目录自动生成 `challenges.json` 配置文件，您可以根据需要修改。或者使用我另外提供的文件。

可选依赖：很多ZombieEscape地图都需要[CS2Fixes](https://github.com/Source2ZE/CS2Fixes)插件才能正常运行，您可以考虑是否需要安装该插件。

## ⚙️ 配置

配置文件位于 `csgo/addons/counterstrikesharp/plugins/ZombieEscapePractice/challenges.json`，采用 JSON 格式。结构如下：

```json
{
  "maps": {
    "123456789": {                     // 创意工坊地图 ID 或地图名
      "challenges": [
        {
          "name": "跳刀练习 - 第一阶段", // 菜单显示的名称
          "position": {                // 传送坐标 (X, Y, Z)
            "x": -1234.5,
            "y": 567.8,
            "z": 900.1
          },
          "command": "ent_fire xxxxx trigger" // 服务器执行的命令，具体命令请自行配置
        },
        {
          "name": "弹幕练习 - 第二阶段",
          "position": { "x": 1000.0, "y": 2000.0, "z": 300.0 },
          "command": "ent_fire xxxxx_controller activate"
        }
      ]
    },
    "ze_example_map": {                 // 使用地图名作为键的示例
      "challenges": [
        {
          "name": "BOSS 战练习",
          "position": { "x": 500.0, "y": -500.0, "z": 128.0 },
          "command": "ent_fire boss_relay trigger"
        }
      ]
    }
    "987654321": {
      "challenges": [
        {
          "name": "跑路练习 - 传送点",
          "position": { "x": 2000, "y": 3000, "z": 512 },
          "command": ""                  // 留空不执行命令
        }
      ]
    },
    "ze_no_teleport": {
      "challenges": [
        {
          "name": "弹幕启动 - 仅命令",
                                          // 不写"position"不执行传送
          "command": "ent_fire barrage_controller activate"
        }
        ]
    }
  }
}
