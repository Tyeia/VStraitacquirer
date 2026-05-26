using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace traitacquirermoddedclasses
{
    public class ItemTraitManual : Item
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return secondsUsed < 2;
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed < 1.9) return;

            if (byEntity.World.Side == EnumAppSide.Server)
            {
                IPlayer? byPlayer = null;
                if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                if (byPlayer == null) return;

                if (!(byPlayer is IServerPlayer)) return;

                
                TreeAttribute tree = new TreeAttribute();
                tree.SetString("playeruid", byPlayer?.PlayerUID);
                if (itemslot.Itemstack == null) return;
                tree.SetStringArray("addtraits", itemslot.Itemstack.ItemAttributes["traitdata"]["add"].AsArray<string>());
                tree.SetStringArray("removetraits", itemslot.Itemstack.ItemAttributes["traitdata"]["remove"].AsArray<string>());
                tree.SetInt("itemslotId", itemslot.Inventory.GetSlotId(itemslot));
                
                api.Event.PushEvent("traitItem", tree);
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            if (inSlot.Itemstack == null) return;
            dsc.Append(Lang.Get("traitacquirer:manualtype-" + inSlot.Itemstack.Item.Variant["class"].ToString()));
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "heldhelp-read",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }

    }
}
