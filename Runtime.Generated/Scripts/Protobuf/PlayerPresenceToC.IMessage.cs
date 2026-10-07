using Google.Protobuf;
using LOP;

public sealed partial class PlayerPresenceToC : GameFramework.IMessage
{
    public ushort messageId => MessageIds.PlayerPresenceToC;

    public byte[] Serialize()
    {
        return this.ToByteArray();
    }

    public void Deserialize(byte[] data)
    {
        this.MergeFrom(data);
    }
}
