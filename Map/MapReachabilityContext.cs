namespace SayTheSpire2.Map;

public readonly record struct MapReachabilityContext(bool HasPermanentFreeTravel, int RemainingFreeTravelCharges)
{
    public bool CanUseFreeTravel => HasPermanentFreeTravel || RemainingFreeTravelCharges > 0;

    public int Strength => HasPermanentFreeTravel ? int.MaxValue : RemainingFreeTravelCharges;

    public bool BetterThan(MapReachabilityContext other)
    {
        return Strength > other.Strength;
    }

    public MapReachabilityContext ConsumeFreeTravel()
    {
        if (HasPermanentFreeTravel || RemainingFreeTravelCharges <= 0)
            return this;

        return new MapReachabilityContext(false, RemainingFreeTravelCharges - 1);
    }
}
