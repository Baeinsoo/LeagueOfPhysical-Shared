using Google.Protobuf;
using LOP;

public sealed partial class EntityInputsToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.EntityInputsToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
