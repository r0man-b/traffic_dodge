using System;

[Flags]
public enum PaintTraits : int
{
    None = 0,
    UniquePearlescentPrimary = 1 << 0,
    UniqueMetalPrimary = 1 << 1,
    UniquePearlescentSecondary = 1 << 2,
    UniqueMetalSecondary = 1 << 3,
    UniquePearlescentRims = 1 << 4,
    UniqueMetalRims = 1 << 5
}

public struct PaintFlags
{
    public bool UniquePearlescent;
    public bool UniqueMetal;

    public PaintFlags(bool pearlescent, bool metal)
    {
        UniquePearlescent = pearlescent;
        UniqueMetal = metal;
    }
}
