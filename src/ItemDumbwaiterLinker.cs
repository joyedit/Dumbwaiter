using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Dumbwaiter.Config;

namespace Dumbwaiter
{
    public class ItemDumbwaiterLinker : Item
    {
        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handling)
        {
            if (!firstEvent || blockSel == null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            var world = byEntity.World;
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityDumbwaiterStation;
            if (be == null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            handling = EnumHandHandling.PreventDefault;

            if (world.Side != EnumAppSide.Server) return;

            var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
            if (player == null) return;

            var attrs = slot.Itemstack.Attributes;
            BlockPos anchor = null;
            if (attrs.HasAttribute("anchorX"))
            {
                anchor = new BlockPos(
                    attrs.GetInt("anchorX"),
                    attrs.GetInt("anchorY"),
                    attrs.GetInt("anchorZ"));
            }

            if (anchor == null || anchor.Equals(blockSel.Position))
            {
                SetAnchor(slot, blockSel.Position);
                SendMsg(player, Lang.Get("dumbwaiter:dumbwaiter-anchor-set"));
                return;
            }

            var anchorBE = world.BlockAccessor.GetBlockEntity(anchor) as BlockEntityDumbwaiterStation;
            if (anchorBE == null)
            {
                ClearAnchor(slot);
                SendMsg(player, Lang.Get("dumbwaiter:dumbwaiter-link-failed"));
                return;
            }

            var cfg = DumbwaiterMod.Config;
            int dy = Math.Abs(blockSel.Position.Y - anchor.Y);
            int dx = Math.Abs(blockSel.Position.X - anchor.X);
            int dz = Math.Abs(blockSel.Position.Z - anchor.Z);

            if (dy > cfg.MaxVerticalDistance || Math.Max(dx, dz) > cfg.MaxHorizontalOffset)
            {
                SendMsg(player, Lang.Get("dumbwaiter:dumbwaiter-link-too-far"));
                return;
            }

            BlockEntityDumbwaiterStation topBE, bottomBE;
            BlockPos topPos, bottomPos;
            if (blockSel.Position.Y >= anchor.Y)
            {
                topBE = be; topPos = blockSel.Position;
                bottomBE = anchorBE; bottomPos = anchor;
            }
            else
            {
                topBE = anchorBE; topPos = anchor;
                bottomBE = be; bottomPos = blockSel.Position;
            }

            topBE.PairedPos = bottomPos.Copy();
            topBE.StationRole = "top";
            topBE.MarkDirty(true);

            bottomBE.PairedPos = topPos.Copy();
            bottomBE.StationRole = "bottom";
            bottomBE.MarkDirty(true);

            ClearAnchor(slot);
            slot.TakeOut(1);
            slot.MarkDirty();

            SendMsg(player, Lang.Get("dumbwaiter:dumbwaiter-linked", dy));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            var attrs = inSlot.Itemstack?.Attributes;
            if (attrs != null && attrs.HasAttribute("anchorX"))
            {
                dsc.AppendLine(Lang.Get("dumbwaiter:dumbwaiter-linker-anchor-info",
                    attrs.GetInt("anchorX"),
                    attrs.GetInt("anchorY"),
                    attrs.GetInt("anchorZ")));
            }
        }

        private static void SetAnchor(ItemSlot slot, BlockPos pos)
        {
            var attrs = slot.Itemstack.Attributes;
            attrs.SetInt("anchorX", pos.X);
            attrs.SetInt("anchorY", pos.Y);
            attrs.SetInt("anchorZ", pos.Z);
            slot.MarkDirty();
        }

        private static void ClearAnchor(ItemSlot slot)
        {
            var attrs = slot.Itemstack.Attributes;
            attrs.RemoveAttribute("anchorX");
            attrs.RemoveAttribute("anchorY");
            attrs.RemoveAttribute("anchorZ");
            slot.MarkDirty();
        }

        private static void SendMsg(IServerPlayer player, string message)
        {
            player.SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
        }
    }
}
