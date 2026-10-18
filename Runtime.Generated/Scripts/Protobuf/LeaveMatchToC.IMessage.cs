using Google.Protobuf;
using LOP;

public sealed partial class LeaveMatchToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.LeaveMatchToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
