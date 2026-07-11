using System;
using Vintagestory.API.Common;

namespace Dumbwaiter.Config
{
    public class DumbwaiterConfig
    {
        public int MaxVerticalDistance { get; set; } = 192;
        public int MaxHorizontalOffset { get; set; } = 4;
        // TransitionDuration is the time for a maximum-distance trip. Shorter trips
        // scale down linearly toward MinTransitionDuration, which is the floor needed
        // for the fade-to-black in/out to still read smoothly.
        public float TransitionDuration { get; set; } = 3.0f;
        public float MinTransitionDuration { get; set; } = 1.0f;
        // UseCooldown is the settling time after a maximum-distance trip. Shorter trips
        // scale down linearly toward MinUseCooldown.
        public float UseCooldown { get; set; } = 2.0f;
        public float MinUseCooldown { get; set; } = 0.5f;
        public bool EnableScreenFade { get; set; } = true;
        public float SoundVolume { get; set; } = 0.8f;

        private const string ConfigFile = "dumbwaiter.json";

        public static DumbwaiterConfig LoadOrCreate(ICoreAPI api)
        {
            try
            {
                var cfg = api.LoadModConfig<DumbwaiterConfig>(ConfigFile);
                if (cfg == null)
                {
                    cfg = new DumbwaiterConfig();
                    api.StoreModConfig(cfg, ConfigFile);
                }
                return cfg;
            }
            catch (Exception e)
            {
                api.Logger.Warning("[Dumbwaiter] Could not load config, using defaults: {0}", e.Message);
                return new DumbwaiterConfig();
            }
        }
    }
}
