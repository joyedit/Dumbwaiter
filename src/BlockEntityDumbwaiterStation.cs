using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Dumbwaiter
{
    public class BlockEntityDumbwaiterStation : BlockEntity
    {
        public const int MaxLabelLines = 4;
        public const int MaxLabelLength = 200;

        public BlockPos PairedPos { get; set; }
        public string StationRole { get; set; } = "bottom"; // "top" | "bottom"
        public string Label { get; set; }
        public bool InUse { get; set; }
        public double LastUsedTime { get; set; }

        public bool IsLinked => PairedPos != null;

        private GuiDialogBlockEntityTextInput editDialog; // client only
        private ItemStack tempStack; // server only: ink & quill held back while the dialog is open

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (PairedPos != null)
            {
                tree.SetInt("pairedX", PairedPos.X);
                tree.SetInt("pairedY", PairedPos.Y);
                tree.SetInt("pairedZ", PairedPos.Z);
            }
            tree.SetString("stationRole", StationRole ?? "bottom");
            if (Label != null) tree.SetString("label", Label);
            // InUse and LastUsedTime are session-state (tied to world.ElapsedMilliseconds
            // which resets on restart) — intentionally not persisted.
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            PairedPos = tree.HasAttribute("pairedX")
                ? new BlockPos(tree.GetInt("pairedX"), tree.GetInt("pairedY"), tree.GetInt("pairedZ"))
                : null;
            StationRole = tree.GetString("stationRole", "bottom");
            Label = tree.GetString("label");
            InUse = false;
            LastUsedTime = 0;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (!string.IsNullOrWhiteSpace(Label))
            {
                // Emphasized so it stands out from the meta lines below (bold + size,
                // not color alone). The block info HUD renders VTML.
                dsc.AppendLine("<font size=\"18\" color=\"#e3d6a9\"><strong>" + StripMarkup(Label) + "</strong></font>");
            }

            string linkLine;
            if (PairedPos == null)
            {
                linkLine = Lang.Get("dumbwaiter:dumbwaiter-info-unlinked");
            }
            else
            {
                int dy = PairedPos.Y - Pos.Y;
                var pairedBE = Api?.World.BlockAccessor.GetBlockEntity(PairedPos) as BlockEntityDumbwaiterStation;
                string pairedLabel = StripMarkup(FirstLine(pairedBE?.Label));

                if (dy == 0)
                {
                    linkLine = pairedLabel != null
                        ? Lang.Get("dumbwaiter:dumbwaiter-info-linked-named-level", pairedLabel)
                        : Lang.Get("dumbwaiter:dumbwaiter-info-linked-level");
                }
                else
                {
                    string dir = dy > 0 ? "above" : "below";
                    linkLine = pairedLabel != null
                        ? Lang.Get("dumbwaiter:dumbwaiter-info-linked-named-" + dir, pairedLabel, Math.Abs(dy))
                        : Lang.Get("dumbwaiter:dumbwaiter-info-linked-" + dir, Math.Abs(dy));
                }
            }

            dsc.AppendLine("<font color=\"#a8a49c\">" + linkLine + "</font>");
        }

        // Server side: ink & quill (writingTool) is a reusable tool and is never
        // consumed. Pigment items like charcoal follow the vanilla labeled-chest
        // economy: one is taken while writing, returned on cancel, 85% chance
        // returned on save.
        public void BeginEdit(IServerPlayer player, ItemSlot slot)
        {
            bool isWritingTool = slot?.Itemstack?.ItemAttributes?["writingTool"].AsBool() == true;
            if (!isWritingTool)
            {
                tempStack = slot.TakeOut(1);
                slot.MarkDirty();
            }
            (Api as ICoreServerAPI)?.Network.SendBlockEntityPacket(player, Pos, (int)EnumSignPacketId.OpenDialog);
        }

        private void RefundTempStack(IPlayer player)
        {
            if (tempStack == null) return;
            if (!player.InventoryManager.TryGiveItemstack(tempStack))
            {
                Api.World.SpawnItemEntity(tempStack, player.Entity.Pos.XYZ);
            }
            tempStack = null;
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.OpenDialog)
            {
                OpenLabelDialog();
                return;
            }

            base.OnReceivedServerPacket(packetid, data);
        }

        private void OpenLabelDialog()
        {
            if (Api is not ICoreClientAPI capi) return;
            if (editDialog != null && editDialog.IsOpened()) return;

            editDialog = new GuiDialogBlockEntityTextInput(
                Lang.Get("dumbwaiter:dumbwaiter-label-dialog-title"),
                Pos,
                Label ?? "",
                capi,
                new TextAreaConfig() { MaxWidth = 200, MaxHeight = 96 });
            editDialog.OnCloseCancel = () =>
            {
                capi.Network.SendBlockEntityPacket(Pos, (int)EnumSignPacketId.CancelEdit, null);
            };
            editDialog.TryOpen();
        }

        // The vanilla text input dialog sends its result as a sign SaveText packet
        // straight to this block entity.
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.SaveText && data != null)
            {
                var packet = SerializerUtil.Deserialize<EditSignPacket>(data);
                Label = SanitizeLabel(packet.Text);
                MarkDirty(true);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();

                if (Api.World.Rand.NextDouble() < 0.85)
                {
                    RefundTempStack(fromPlayer);
                }
                else
                {
                    tempStack = null; // the pigment is used up
                }
                return;
            }

            if (packetid == (int)EnumSignPacketId.CancelEdit)
            {
                RefundTempStack(fromPlayer);
                return;
            }

            base.OnReceivedClientPacket(fromPlayer, packetid, data);
        }

        private static string SanitizeLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string text = raw.Replace("\r", "").Replace("<", "").Replace(">", "").Trim();
            string[] lines = text.Split('\n');
            if (lines.Length > MaxLabelLines)
            {
                text = string.Join("\n", lines, 0, MaxLabelLines);
            }
            if (text.Length > MaxLabelLength)
            {
                text = text.Substring(0, MaxLabelLength);
            }
            return text;
        }

        // Labels are sanitized on save, but strip again at display time so legacy
        // data can never inject VTML into the info HUD.
        private static string StripMarkup(string text)
        {
            return text?.Replace("<", "").Replace(">", "");
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            int nl = text.IndexOf('\n');
            return (nl >= 0 ? text.Substring(0, nl) : text).Trim();
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            // TODO notify paired station to clear its PairedPos
            base.OnBlockBroken(byPlayer);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            editDialog?.TryClose();
            editDialog?.Dispose();
            editDialog = null;
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            editDialog?.TryClose();
            editDialog?.Dispose();
            editDialog = null;
        }
    }
}
