using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Dumbwaiter.Config;

namespace Dumbwaiter
{
    public class DumbwaiterMod : ModSystem
    {
        public const string NetworkChannel = "dumbwaiter";

        public static DumbwaiterConfig Config { get; private set; }

        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;
        private LiftTransition transition;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Config = DumbwaiterConfig.LoadOrCreate(api);

            api.RegisterBlockClass("BlockDumbwaiterStation", typeof(BlockDumbwaiterStation));
            api.RegisterBlockEntityClass("BEDumbwaiterStation", typeof(BlockEntityDumbwaiterStation));
            api.RegisterItemClass("ItemDumbwaiterLinker", typeof(ItemDumbwaiterLinker));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            sapi.Network.RegisterChannel(NetworkChannel)
                .RegisterMessageType<TransitionPacket>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            transition = new LiftTransition(api);
            api.Event.RegisterRenderer(transition, EnumRenderStage.Ortho, "dumbwaiter-transition");

            capi.Network.RegisterChannel(NetworkChannel)
                .RegisterMessageType<TransitionPacket>()
                .SetMessageHandler<TransitionPacket>(OnTransitionPacket);
        }

        private void OnTransitionPacket(TransitionPacket packet)
        {
            transition?.Begin(
                packet.DurationSeconds,
                new Vec3f(packet.SourceX + 0.5f, packet.SourceY + 0.5f, packet.SourceZ + 0.5f),
                packet.Volume,
                packet.SuppressFade);
        }
    }
}
