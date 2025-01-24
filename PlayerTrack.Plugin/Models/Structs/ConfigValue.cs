namespace PlayerTrack.Models.Structs;

public struct ConfigValue<T>
{
    public InheritOverride InheritOverride;
    public T Value;

    public ConfigValue(InheritOverride inheritOverride, T value)
    {
        InheritOverride = inheritOverride;
        Value = value;
    }
}
