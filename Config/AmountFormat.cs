namespace SmartCraftStorage.Config
{
    // How the available amount is written next to the requirement. The slot is only so
    // wide and the label spills over its neighbour once the text outgrows it, so each
    // step down this list trades a bit of precision or emphasis for width.
    internal enum AmountFormat
    {
        // 4(1.1k) — abbreviated past a thousand, brackets at 60% of the label size.
        Compact,

        // 4(1087) — the number in full, brackets still at 60%.
        Exact,

        // 4 (1087) — the number in full at the game's own text size, spaced off the
        // requirement. The widest of the three.
        Spaced,
    }
}
