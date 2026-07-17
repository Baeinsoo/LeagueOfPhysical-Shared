using Google.Protobuf;
using LOP;

public sealed partial class WorldEventBatchToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.WorldEventBatchToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
