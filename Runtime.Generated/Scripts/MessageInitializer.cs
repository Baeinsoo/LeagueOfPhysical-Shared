using UnityEngine;

namespace LOP
{
    public class MessageInitializer
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            MessageFactory.RegisterCreator(MessageIds.EntityDespawnToC, () => new EntityDespawnToC());
            MessageFactory.RegisterCreator(MessageIds.EntitySnapsToC, () => new EntitySnapsToC());
            MessageFactory.RegisterCreator(MessageIds.EntitySpawnToC, () => new EntitySpawnToC());
            MessageFactory.RegisterCreator(MessageIds.GameInfoToC, () => new GameInfoToC());
            MessageFactory.RegisterCreator(MessageIds.GameInfoToS, () => new GameInfoToS());
            MessageFactory.RegisterCreator(MessageIds.InputCommandToS, () => new InputCommandToS());
            MessageFactory.RegisterCreator(MessageIds.InputSequenceToC, () => new InputSequenceToC());
            MessageFactory.RegisterCreator(MessageIds.InputTimingToC, () => new InputTimingToC());
            MessageFactory.RegisterCreator(MessageIds.MatchEndedToC, () => new MatchEndedToC());
            MessageFactory.RegisterCreator(MessageIds.StatAllocationToC, () => new StatAllocationToC());
            MessageFactory.RegisterCreator(MessageIds.StatAllocationToS, () => new StatAllocationToS());
            MessageFactory.RegisterCreator(MessageIds.UserEntitySnapToC, () => new UserEntitySnapToC());
            MessageFactory.RegisterCreator(MessageIds.WorldEventBatchToC, () => new WorldEventBatchToC());
        }
    }
}
