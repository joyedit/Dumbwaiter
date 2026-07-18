using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
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
                    ActionLangCode = "dumbwaiter:blockhelp-send-cargo",
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

        // The hoist frame's top is a valid resting place for cargo. Answering this
        // directly (rather than relying on sidesolid JSON alone) satisfies every
        // requires-solid-ground placement check, including CarryOn's.
        public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
        {
            if (blockFace == BlockFacing.UP) return true;
            return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
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

                // Sneaking with anything else — or with empty hands — is not ours.
                // Pass it through so block placement and CarryOn's empty-handed
                // pickup/put-down keep working on and around the station.
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
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
            bool onPlatform =
                (int)Math.Floor(pos.X) == blockSel.Position.X &&
                (int)Math.Floor(pos.Y) == blockSel.Position.Y &&
                (int)Math.Floor(pos.Z) == blockSel.Position.Z;

            // Not riding? Then the station can still haul cargo: a container block
            // (chest, basket, vessel, ...) set on top of the hoist frame.
            BlockPos cargoPos = null;
            if (!onPlatform)
            {
                var candidatePos = blockSel.Position.UpCopy();
                if (world.BlockAccessor.GetBlockEntity(candidatePos) is not BlockEntityContainer)
                {
                    Msg(serverPlayer, "dumbwaiter-not-on-platform");
                    return true;
                }

                // Multiblock furniture (trunks) spans two cells; moving just the
                // principal half would leave a corrupt dummy behind.
                var cargoBlock = world.BlockAccessor.GetBlock(candidatePos);
                if (cargoBlock.Code.Path.Contains("trunk"))
                {
                    Msg(serverPlayer, "dumbwaiter-cargo-immovable");
                    return true;
                }

                // Reinforcement (and padlocks, which ride on it) is stored by
                // position — moving the block would strand it. Refuse instead.
                var reinforcement = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
                if (reinforcement?.IsReinforced(candidatePos) == true)
                {
                    Msg(serverPlayer, "dumbwaiter-cargo-reinforced");
                    return true;
                }

                cargoPos = candidatePos;
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
            if (cargoPos != null)
            {
                // The container needs the whole cell above the paired station.
                if (blockAbove.Replaceable < 6000)
                {
                    Msg(serverPlayer, "dumbwaiter-cargo-dest-blocked");
                    return true;
                }
            }
            else if (blockAbove.CollisionBoxes != null && blockAbove.CollisionBoxes.Length > 0)
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
                    Volume = cfg.SoundVolume,
                    SuppressFade = cargoPos != null
                }, serverPlayer);

            var destination = be.PairedPos.ToVec3d().Add(0.5, 0.13, 0.5);
            var sourcePos = blockSel.Position.Copy();
            var pairedPos = be.PairedPos.Copy();
            int teleportDelayMs = (int)(duration * 500);

            world.Api.Event.RegisterCallback(_ =>
            {
                if (cargoPos != null)
                {
                    MoveCargo(world, cargoPos, pairedPos.UpCopy(), serverPlayer);
                }
                else if (byPlayer?.Entity?.Alive == true)
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

        // Relocate a container block (with its inventory) from the cell above one
        // station to the cell above the other. The classic move-a-block-entity
        // pattern: serialize the BE to a tree, retarget the position fields, remove
        // the source block (no drops — SetBlock never calls OnBlockBroken), place
        // the same block at the destination, restore the tree onto its fresh BE.
        private static void MoveCargo(IWorldAccessor world, BlockPos from, BlockPos to, IServerPlayer player)
        {
            var block = world.BlockAccessor.GetBlock(from);
            var be = world.BlockAccessor.GetBlockEntity(from);
            var destBlock = world.BlockAccessor.GetBlock(to);

            // Re-verify at departure time: the container may have been broken, or
            // the destination cell filled, during the transition delay.
            if (be is not BlockEntityContainer || destBlock.Replaceable < 6000)
            {
                Msg(player, "dumbwaiter-cargo-jammed");
                return;
            }

            var tree = new TreeAttribute();
            be.ToTreeAttributes(tree);
            tree.SetInt("posx", to.X);
            tree.SetInt("posy", to.Y);
            tree.SetInt("posz", to.Z);

            world.BlockAccessor.SetBlock(0, from);
            world.BlockAccessor.SetBlock(block.BlockId, to);

            var newBE = world.BlockAccessor.GetBlockEntity(to);
            if (newBE != null)
            {
                newBE.FromTreeAttributes(tree, world);
                newBE.MarkDirty(true);
            }

            Msg(player, "dumbwaiter-cargo-sent");
        }

        // Scan the vertical column strictly between the two stations. A solid block
        // sitting in the shaft (e.g. someone walls it off, or builds across the gap)
        // jams the mechanism for both ends until it's cleared. The trip is essentially
        // vertical, so we probe the column above the lower station. The cell directly
        // above the lower station is excluded: it's the loading cell — it legitimately
        // holds the payload when sending cargo up, and anything else sitting there is
        // already caught by the destination clearance checks.
        private static bool ShaftBlocked(IWorldAccessor world, BlockPos a, BlockPos b)
        {
            BlockPos bottom = a.Y <= b.Y ? a : b;
            int topY = Math.Max(a.Y, b.Y);
            var probe = bottom.Copy();
            for (int y = bottom.Y + 2; y < topY; y++)
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
