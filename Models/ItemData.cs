using SharpDX;

namespace RegexCrafter.Models;

public readonly record struct ItemData(
    long Address,
    string BaseName,
    int StackSize,
    RectangleF ClickRect
);