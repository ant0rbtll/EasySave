using EasySave.Localization;

namespace EasySave.UI.Menu
{
    /// <summary>
    /// Configuration for a console menu including items, actions, and display options.
    /// </summary>
    internal class MenuConfig
    {
        public LocalizationKey[]? Items { get; }
        public string[]? ItemsAsStrings { get; }
        public Dictionary<int, Action> Actions { get; }
        public LocalizationKey Label { get; }
        public Action? RenderHeader { get; }

        public MenuConfig(LocalizationKey[] items, Dictionary<int, Action> actions, LocalizationKey label = LocalizationKey.menu, Action? renderHeader = null)
        {
            Items = items;
            Actions = actions;
            Label = label;
            RenderHeader = renderHeader;
        }

        public MenuConfig(string[] items, Dictionary<int, Action> actions, LocalizationKey label = LocalizationKey.menu, Action? renderHeader = null)
        {
            ItemsAsStrings = items;
            Actions = actions;
            Label = label;
            RenderHeader = renderHeader;
        }
    }
}
