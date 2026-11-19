using Google.Protobuf;
using LOP;

public sealed partial class TriGrabStateToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.TriGrabStateToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
