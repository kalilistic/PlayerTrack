using System.Runtime.InteropServices;

namespace PlayerTrack.Models.Structs;

[StructLayout(LayoutKind.Explicit)]
public readonly struct CharaCustomizeData
{
    [FieldOffset((int)CustomizeIndex.BustSize)]
    public readonly byte BustSize;

    [FieldOffset((int)CustomizeIndex.Eyebrows)]
    public readonly byte Eyebrows;

    [FieldOffset((int)CustomizeIndex.EyeColor)]
    public readonly byte EyeColor;

    [FieldOffset((int)CustomizeIndex.EyeColor2)]
    public readonly byte EyeColor2;

    [FieldOffset((int)CustomizeIndex.EyeShape)]
    public readonly byte EyeShape;

    [FieldOffset((int)CustomizeIndex.FaceFeatures)]
    public readonly byte FaceFeatures;

    [FieldOffset((int)CustomizeIndex.FaceFeaturesColor)]
    public readonly byte FaceFeaturesColor;

    [FieldOffset((int)CustomizeIndex.Facepaint)]
    public readonly byte Facepaint;

    [FieldOffset((int)CustomizeIndex.FacepaintColor)]
    public readonly byte FacepaintColor;

    [FieldOffset((int)CustomizeIndex.FaceType)]
    public readonly byte FaceType;

    [FieldOffset((int)CustomizeIndex.Gender)]
    public readonly byte Gender;

    [FieldOffset((int)CustomizeIndex.HairColor)]
    public readonly byte HairColor;

    [FieldOffset((int)CustomizeIndex.HairColor2)]
    public readonly byte HairColor2;

    [FieldOffset((int)CustomizeIndex.HairStyle)]
    public readonly byte HairStyle;

    [FieldOffset((int)CustomizeIndex.HasHighlights)]
    public readonly byte HasHighlights;

    [FieldOffset((int)CustomizeIndex.Height)]
    public readonly byte Height;

    [FieldOffset((int)CustomizeIndex.JawShape)]
    public readonly byte JawShape;

    [FieldOffset((int)CustomizeIndex.LipColor)]
    public readonly byte LipColor;

    [FieldOffset((int)CustomizeIndex.LipStyle)]
    public readonly byte LipStyle;

    [FieldOffset((int)CustomizeIndex.ModelType)]
    public readonly byte ModelType;

    [FieldOffset((int)CustomizeIndex.NoseShape)]
    public readonly byte NoseShape;

    [FieldOffset((int)CustomizeIndex.Race)]
    public readonly byte Race;

    [FieldOffset((int)CustomizeIndex.RaceFeatureSize)]
    public readonly byte RaceFeatureSize;

    [FieldOffset((int)CustomizeIndex.RaceFeatureType)]
    public readonly byte RaceFeatureType;

    [FieldOffset((int)CustomizeIndex.SkinColor)]
    public readonly byte SkinColor;

    [FieldOffset((int)CustomizeIndex.Tribe)]
    public readonly byte Tribe;

    public static CharaCustomizeData MapCustomizeData(byte[] customizeIndex)
    {
        var handle = GCHandle.Alloc(customizeIndex, GCHandleType.Pinned);
        CharaCustomizeData charaCustomizeData;
        try
        {
            charaCustomizeData = (CharaCustomizeData)(Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(CharaCustomizeData)) ?? 0);
        }
        finally
        {
            handle.Free();
        }

        return charaCustomizeData;
    }
}
