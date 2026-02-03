

namespace EasySave.UI.Menu
{
    internal class MenuConfig
    {
        public string[] Items { get; }
        public Dictionary<int, Action> Actions { get; }
        public string Label { get; }
        public bool ClearAferMove { get; }

        public MenuConfig(string[] items, Dictionary<int, Action> actions, string label = "menu", bool clearAfterMove = true)
        {
            Items = items;
            Actions = actions;
            Label = label;
            ClearAferMove = clearAfterMove;
        }
    }
}
