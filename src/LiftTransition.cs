using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Dumbwaiter
{
    // Client-side smooth fade-to-black + looped sound during the lift transition.
    // Registered as an Ortho renderer in StartClientSide so Render2DTexture runs
    // under the GUI ortho projection. A full-screen opaque-black texture is drawn
    // with a per-frame alpha (via the Vec4f color overload, which sets ApplyColor=1
    // and multiplies the texture), ramping 0->1->0 so the teleport (at mid-duration)
    // lands while the screen is fully black.
    public class LiftTransition : IRenderer
    {
        private readonly ICoreClientAPI capi;

        // Draw on top of the rest of the Ortho stage.
        public double RenderOrder => 1.0;
        public int RenderRange => 100;

        private float elapsed;
        private float totalDuration;
        private bool active;
        private ILoadedSound sound;
        private int blackTexId;
        private readonly Vec4f color = new Vec4f(1f, 1f, 1f, 0f);
        private bool fadeEnabled;

        public LiftTransition(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        public void Begin(float durationSeconds, Vec3f soundPos, float volume, bool suppressFade = false)
        {
            totalDuration = durationSeconds;
            elapsed = 0f;
            active = true;
            fadeEnabled = !suppressFade && (DumbwaiterMod.Config?.EnableScreenFade ?? true);

            if (fadeEnabled) EnsureTexture();

            StopSound();
            sound = capi.World.LoadSound(new SoundParams
            {
                Location = new AssetLocation("dumbwaiter:sounds/lift-operate"),
                ShouldLoop = true,
                Position = soundPos,
                DisposeOnFinish = false,
                Volume = volume,
                Range = 32f
            });
            sound?.Start();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!active) return;

            elapsed += deltaTime;
            if (elapsed >= totalDuration)
            {
                active = false;
                StopSound();
                return;
            }

            if (!fadeEnabled || blackTexId == 0) return;

            // Smooth alpha ramp: fade in over the first 30% of the duration, hold
            // fully black through the middle (where the teleport fires at 50%), then
            // fade back out over the final 30%.
            float fadeIn = 0.15f * totalDuration;
            float fadeOut = 0.85f * totalDuration;
            float alpha;
            if (elapsed < fadeIn) alpha = elapsed / fadeIn;
            else if (elapsed > fadeOut) alpha = 1f - (elapsed - fadeOut) / (totalDuration - fadeOut);
            else alpha = 1f;

            color.W = GameMath.Clamp(alpha, 0f, 1f);
            if (color.W <= 0f) return;

            capi.Render.Render2DTexture(
                blackTexId,
                0, 0,
                capi.Render.FrameWidth,
                capi.Render.FrameHeight,
                50f,
                color);
        }

        private void EnsureTexture()
        {
            if (blackTexId != 0) return;
            blackTexId = capi.Render.GetOrLoadTexture(new AssetLocation("dumbwaiter:textures/gui/fade-black.png"));
            if (blackTexId == 0)
                capi.Logger.Warning("[Dumbwaiter] fade texture failed to load (id=0); screen fade will not render.");
            else
                capi.Logger.Notification("[Dumbwaiter] fade texture loaded: id={0}", blackTexId);
        }

        private void StopSound()
        {
            if (sound == null) return;
            sound.Stop();
            sound.Dispose();
            sound = null;
        }

        public void Dispose()
        {
            StopSound();
            blackTexId = 0;
        }
    }
}
