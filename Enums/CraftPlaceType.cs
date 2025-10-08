using System.ComponentModel;

namespace RegexCrafter.Enums
{
    public enum CraftPlaceType
    {
        [Description("Inventory")] Inventory = 0,
        [Description("Stash")] Stash = 1,
        [Description("Mouse Position")] MousePosition = 2
    }
}
