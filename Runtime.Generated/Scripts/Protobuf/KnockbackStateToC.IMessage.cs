using Google.Protobuf;
using LOP;

public sealed partial class KnockbackStateToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.KnockbackStateToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
