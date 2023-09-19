namespace PlayerTrack.Models.Structs;

public struct ConfigValue<T>
{
    public InheritOverride InheritOverride;
    public T Value;

    public ConfigValue(InheritOverride inheritOverride, T value)
    {
        this.InheritOverride = inheritOverride;
        this.Value = value;
    }
}
