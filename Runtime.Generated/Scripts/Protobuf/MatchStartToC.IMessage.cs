using Google.Protobuf;
using LOP;

public sealed partial class MatchStartToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.MatchStartToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
