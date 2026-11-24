using Google.Protobuf;
using LOP;

public sealed partial class TriGrabCardsToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.TriGrabCardsToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
