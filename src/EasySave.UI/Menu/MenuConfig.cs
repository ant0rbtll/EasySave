using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.UI.Menu
{
    internal class MenuConfig
    {
        public string[] Items { get; set; }
        public Dictionary<int, Action> Actions { get; set; }
        public string Label { get; set; }

        public MenuConfig(string[] items, Dictionary<int, Action> actions, string label = "menu")
        {
            Items = items;
            Actions = actions;
            Label = label;
        }
    }
}
