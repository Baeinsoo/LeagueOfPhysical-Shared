using Google.Protobuf;
using LOP;

public sealed partial class InputCommandToS : GameFramework.IMessage
{
    public ushort messageId => MessageIds.InputCommandToS;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
