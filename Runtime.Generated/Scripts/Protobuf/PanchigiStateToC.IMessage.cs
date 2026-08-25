using Google.Protobuf;
using LOP;

public sealed partial class PanchigiStateToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.PanchigiStateToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
