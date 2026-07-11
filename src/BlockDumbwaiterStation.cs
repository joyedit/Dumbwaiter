using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Dumbwaiter.Config;

namespace Dumbwaiter
{
    public class BlockDumbwaiterStation : Block
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var writingStacks = new List<ItemStack>();
            foreach (string code in new[] { "game:inkandquill", "game:charcoal" })
            {
                var item = api.World.GetItem(new AssetLocation(code));
                if (item != null) writingStacks.Add(new ItemStack(item));
            }

            interactions = new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "dumbwaiter:blockhelp-operate",
                    MouseButton = EnumMouseButton.Right
                },
                new WorldInteraction
                {
                    ActionLangCode = "dumbwaiter:blockhelp-label",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = writingStacks.Count > 0 ? writingStacks.ToArray() : null
                }
            };
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // Sneak + right-click holding an ink and quill (any pigment item, same rule
            // as vanilla signs) edits the station label. Works any time — no need to be
            // linked or standing on the platform.
            if (byPlayer.Entity.Controls.ShiftKey)
            {
                var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

                // Ink & quill carries writingTool in current releases; pigment.color is
                // the newer/labeled-chest convention. Accept either.
                var stackAttrs = hotbarSlot?.Itemstack?.ItemAttributes;
                if (stackAttrs?["writingTool"].AsBool() == true ||
                    stackAttrs?["pigment"]?["color"].Exists == true)
                {
                    if (world.Side == EnumAppSide.Server &&
                        world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityDumbwaiterStation labelBE)
                    {
                        labelBE.BeginEdit(byPlayer as IServerPlayer, hotbarSlot);
                    }
                    return true;
                }

                if (hotbarSlot?.Empty ?? true)
                {
                    if (world.Side == EnumAppSide.Server)
                    {
                        Msg(byPlayer as IServerPlayer, "dumbwaiter-label-needs-ink");
                    }
                    return true;
                }
            }

            if (world.Side != EnumAppSide.Server) return true;

            var serverPlayer = byPlayer as IServerPlayer;
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityDumbwaiterStation;
            if (be == null)
            {
                Msg(serverPlayer, "dumbwaiter-broken-be");
                return true;
            }

            var cfg = DumbwaiterMod.Config;
            double now = world.ElapsedMilliseconds / 1000.0;

            var pos = byPlayer.Entity.Pos;
            if ((int)Math.Floor(pos.X) != blockSel.Position.X ||
                (int)Math.Floor(pos.Y) != blockSel.Position.Y ||
                (int)Math.Floor(pos.Z) != blockSel.Position.Z)
            {
                Msg(serverPlayer, "dumbwaiter-not-on-platform");
                return true;
            }

            if (!be.IsLinked)
            {
                Msg(serverPlayer, "dumbwaiter-not-linked");
                return true;
            }

            if (be.InUse)
            {
                Msg(serverPlayer, "dumbwaiter-in-use");
                return true;
            }

            // Both the transition time and the settling cooldown scale with how far
            // apart the two stations are, so short hops feel quick all round.
            float distanceRatio = DistanceRatio(cfg, blockSel.Position, be.PairedPos);
            float cooldown = Scale(cfg.MinUseCooldown, cfg.UseCooldown, distanceRatio);

            if (now - be.LastUsedTime < cooldown)
            {
                Msg(serverPlayer, "dumbwaiter-settling");
                return true;
            }

            var pairedBE = world.BlockAccessor.GetBlockEntity(be.PairedPos) as BlockEntityDumbwaiterStation;
            if (pairedBE == null)
            {
                Msg(serverPlayer, "dumbwaiter-too-far");
                return true;
            }

            if (pairedBE.InUse)
            {
                Msg(serverPlayer, "dumbwaiter-in-use");
                return true;
            }

            if (now - pairedBE.LastUsedTime < cooldown)
            {
                Msg(serverPlayer, "dumbwaiter-settling");
                return true;
            }

            var aboveDest = be.PairedPos.UpCopy();
            var blockAbove = world.BlockAccessor.GetBlock(aboveDest);
            if (blockAbove.CollisionBoxes != null && blockAbove.CollisionBoxes.Length > 0)
            {
                Msg(serverPlayer, "dumbwaiter-obstructed");
                return true;
            }

            if (ShaftBlocked(world, blockSel.Position, be.PairedPos))
            {
                Msg(serverPlayer, "dumbwaiter-shaft-blocked");
                return true;
            }

            be.InUse = true;
            pairedBE.InUse = true;
            be.MarkDirty(true);
            pairedBE.MarkDirty(true);

            float duration = Scale(cfg.MinTransitionDuration, cfg.TransitionDuration, distanceRatio);

            var sapi = world.Api as ICoreServerAPI;
            sapi?.Network.GetChannel(DumbwaiterMod.NetworkChannel)
                .SendPacket(new TransitionPacket
                {
                    DurationSeconds = duration,
                    SourceX = blockSel.Position.X,
                    SourceY = blockSel.Position.Y,
                    SourceZ = blockSel.Position.Z,
                    Volume = cfg.SoundVolume
                }, serverPlayer);

            var destination = be.PairedPos.ToVec3d().Add(0.5, 0.13, 0.5);
            var sourcePos = blockSel.Position.Copy();
            var pairedPos = be.PairedPos.Copy();
            int teleportDelayMs = (int)(duration * 500);

            world.Api.Event.RegisterCallback(_ =>
            {
                if (byPlayer?.Entity?.Alive == true)
                {
                    byPlayer.Entity.TeleportTo(destination);
                }

                double endTime = world.ElapsedMilliseconds / 1000.0;
                var sourceBE = world.BlockAccessor.GetBlockEntity(sourcePos) as BlockEntityDumbwaiterStation;
                var destBE = world.BlockAccessor.GetBlockEntity(pairedPos) as BlockEntityDumbwaiterStation;

                if (sourceBE != null)
                {
                    sourceBE.InUse = false;
                    sourceBE.LastUsedTime = endTime;
                    sourceBE.MarkDirty(true);
                }
                if (destBE != null)
                {
                    destBE.InUse = false;
                    destBE.LastUsedTime = endTime;
                    destBE.MarkDirty(true);
                }
            }, teleportDelayMs);

            return true;
        }

        // Scan the vertical column strictly between the two stations. A solid block
        // sitting in the shaft (e.g. someone walls it off, or builds across the gap)
        // jams the mechanism for both ends until it's cleared. The trip is essentially
        // vertical, so we probe the column above the lower station.
        private static bool ShaftBlocked(IWorldAccessor world, BlockPos a, BlockPos b)
        {
            BlockPos bottom = a.Y <= b.Y ? a : b;
            int topY = Math.Max(a.Y, b.Y);
            var probe = bottom.Copy();
            for (int y = bottom.Y + 1; y < topY; y++)
            {
                probe.Y = y;
                var block = world.BlockAccessor.GetBlock(probe);
                if (block.CollisionBoxes != null && block.CollisionBoxes.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // How far apart the two stations are, as a 0..1 fraction of the max link
        // distance. The trip is essentially vertical, so we measure on the Y span.
        private static float DistanceRatio(DumbwaiterConfig cfg, BlockPos a, BlockPos b)
        {
            int span = Math.Abs(a.Y - b.Y);
            return cfg.MaxVerticalDistance > 0
                ? GameMath.Clamp((float)span / cfg.MaxVerticalDistance, 0f, 1f)
                : 1f;
        }

        // Interpolate from the floor (closest) to the max (furthest) by the ratio,
        // guarding against a configured floor that exceeds the max.
        private static float Scale(float min, float max, float ratio)
        {
            float floor = Math.Min(min, max);
            return floor + (max - floor) * ratio;
        }

        private static void Msg(IServerPlayer player, string langKey)
        {
            player?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("dumbwaiter:" + langKey), EnumChatType.Notification);
        }
    }
}
