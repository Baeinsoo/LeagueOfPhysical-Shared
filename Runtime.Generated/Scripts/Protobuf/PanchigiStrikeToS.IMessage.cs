using Google.Protobuf;
using LOP;

public sealed partial class PanchigiStrikeToS : GameFramework.IMessage
{
    public ushort messageId => MessageIds.PanchigiStrikeToS;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
