namespace PlayerTrack.Models.Structs;

public struct ExtractedProperty<T>
{
    public PlayerConfigType PlayerConfigType;
    public T PropertyValue;
    public int CategoryId;
}
