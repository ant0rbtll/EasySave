using EasySave.Localization;

namespace EasySave.UI.Menu
{
    internal class MenuConfig
    {
        public LocalizationKey[] Items { get; }
        public Dictionary<int, Action> Actions { get; }
        public LocalizationKey Label { get; }

        public MenuConfig(LocalizationKey[] items, Dictionary<int, Action> actions, LocalizationKey label = LocalizationKey.menu)
        {
            Items = items;
            Actions = actions;
            Label = label;
        }
    }
}
