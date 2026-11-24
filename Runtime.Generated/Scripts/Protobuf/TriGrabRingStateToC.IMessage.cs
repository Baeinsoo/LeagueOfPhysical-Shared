using Google.Protobuf;
using LOP;

public sealed partial class TriGrabRingStateToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.TriGrabRingStateToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
