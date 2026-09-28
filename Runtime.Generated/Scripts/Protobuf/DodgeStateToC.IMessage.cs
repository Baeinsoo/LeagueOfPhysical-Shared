using Google.Protobuf;
using LOP;

public sealed partial class DodgeStateToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.DodgeStateToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
