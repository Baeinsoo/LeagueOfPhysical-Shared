using Google.Protobuf;
using LOP;

public sealed partial class MatchReadyToS : GameFramework.IMessage
{
    public ushort messageId => MessageIds.MatchReadyToS;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
