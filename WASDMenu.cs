using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace ZombieEscapePractice
{
    // 菜单选项
    public class WASDMenuOption
    {
        public string Text { get; set; }
        public Action<CCSPlayerController> Action { get; set; }

        public WASDMenuOption(string text, Action<CCSPlayerController> action)
        {
            Text = text;
            Action = action;
        }
    }

    // 菜单
    public class WASDMenu
    {
        public string Title { get; set; }
        public List<WASDMenuOption> Options { get; set; } = new();
        public int SelectedIndex { get; set; } = 0;
        public int StartOffset { get; set; } = 0;
        private const int MaxVisibleOptions = 4;

        public string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<font class='fontSize-l' color='#3b62d9'>{Title}</font>");
            sb.AppendLine("<br>");
            for (int i = StartOffset; i < StartOffset + MaxVisibleOptions; i++)
            {
                if (i >= Options.Count)
                {
                    sb.AppendLine("<br>");
                    continue;
                }
                if (i == SelectedIndex)
                    sb.AppendLine($"<font color='#ccacfc'>⋆ {Options[i].Text}</font> <br>");
                else
                    sb.AppendLine($"<font color='white'>{Options[i].Text}</font> <br>");
            }
            sb.AppendLine("<br>");
            sb.AppendLine("<font class='fontSize-s' color='#9ee1f0'>[WASD] 移动光标 | [E] 选择 | [R] 关闭</font>");
            return sb.ToString();
        }

        public void ScrollUp()
        {
            if (Options.Count == 0) return;
            SelectedIndex = (SelectedIndex - 1 + Options.Count) % Options.Count;
            UpdateOffset();
        }

        public void ScrollDown()
        {
            if (Options.Count == 0) return;
            SelectedIndex = (SelectedIndex + 1) % Options.Count;
            UpdateOffset();
        }

        private void UpdateOffset()
        {
            if (SelectedIndex < StartOffset)
                StartOffset = SelectedIndex;
            else if (SelectedIndex >= StartOffset + MaxVisibleOptions)
                StartOffset = SelectedIndex - MaxVisibleOptions + 1;
        }
    }

    // 菜单管理器（每个玩家一个会话）
    public class WASDMenuManager
    {
        private readonly BasePlugin _plugin;
        private Dictionary<int, WASDMenu> _playerMenus = new();

        public WASDMenuManager(BasePlugin plugin)
        {
            _plugin = plugin;
        }

        public void OpenMenu(CCSPlayerController player, string title, List<WASDMenuOption> options)
        {
            if (!player.IsValid) return;
            var menu = new WASDMenu { Title = title, Options = options };
            _playerMenus[player.Slot] = menu;
            RenderMenu(player);
        }

        public void CloseMenu(CCSPlayerController player)
        {
            if (_playerMenus.ContainsKey(player.Slot))
            {
                _playerMenus.Remove(player.Slot);
                player.PrintToCenterHtml(""); // 清空中心HTML
            }
        }

        private void RenderMenu(CCSPlayerController player)
        {
            if (_playerMenus.TryGetValue(player.Slot, out var menu))
            {
                player.PrintToCenterHtml(menu.Render());
            }
        }

        public void OnTick()
        {
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (!player.IsValid || player.IsBot || player.IsHLTV) continue;
                if (!_playerMenus.TryGetValue(player.Slot, out var menu)) continue;

                var buttons = player.Buttons;
                // 检测按键按下（上升沿）
                if (!_prevButtons.ContainsKey(player.Slot))
                    _prevButtons[player.Slot] = PlayerButtons.Alt1;

                var prev = _prevButtons[player.Slot];
                var pressed = buttons & ~prev;

                if ((pressed & PlayerButtons.Forward) != 0)
                {
                    menu.ScrollUp();
                    RenderMenu(player);
                }
                else if ((pressed & PlayerButtons.Back) != 0)
                {
                    menu.ScrollDown();
                    RenderMenu(player);
                }
                else if ((pressed & PlayerButtons.Use) != 0) // E 键
                {
                    var selectedOption = menu.Options[menu.SelectedIndex];
                    selectedOption.Action(player);
                    CloseMenu(player);
                }
                else if ((pressed & PlayerButtons.Reload) != 0) // R 键
                {
                    CloseMenu(player);
                }

                _prevButtons[player.Slot] = buttons;
            }
        }

        private Dictionary<int, PlayerButtons> _prevButtons = new();
    }
}