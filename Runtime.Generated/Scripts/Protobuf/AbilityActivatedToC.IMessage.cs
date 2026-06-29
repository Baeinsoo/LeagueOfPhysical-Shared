using Google.Protobuf;
using LOP;

public sealed partial class AbilityActivatedToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.AbilityActivatedToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
