namespace Engine;

public readonly struct PickedObject
{
    public byte SceneId { get; }
    public RenderID24 ObjectId { get; }

    public PickedObject(byte sceneId, RenderID24 objectId)
    {
        SceneId = sceneId;
        ObjectId = objectId;
    }

    public PickedObject(uint packedId)
    {
        SceneId = (byte)((packedId >> 24) & 0xFF);
        ObjectId = new RenderID24(packedId & 0xFFFFFF);
    }

    public static PickedObject FromColor(byte r, byte g, byte b, byte a)
    {
        uint packedId = (uint)(r | (g << 8) | (b << 16) | (a << 24));
        return new PickedObject(packedId);
    }

    public uint PackedId => ((uint)SceneId << 24) | ObjectId.Value;

    public bool IsValid => ObjectId.Value != 0 && SceneId != 0;

    public override string ToString() => $"Scene: {SceneId}, Object: {ObjectId.Value}";
}