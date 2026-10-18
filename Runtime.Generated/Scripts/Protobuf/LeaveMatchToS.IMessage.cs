using Google.Protobuf;
using LOP;

public sealed partial class LeaveMatchToS : GameFramework.IMessage
{
    public ushort messageId => MessageIds.LeaveMatchToS;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
