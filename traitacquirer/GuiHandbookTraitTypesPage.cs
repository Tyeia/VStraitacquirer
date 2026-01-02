using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace traitacquirermoddedclasses
{
    internal class GuiHandbookTraitTypesPage : GuiHandbookPage
    {
        private ICoreClientAPI capi;
        private int type;
        private string typeName;
        private List<ExtendedTrait> traits;

        public string pageCode;
        public string Title;
        public string categoryCode = "trait";
        string Text = "";
        public LoadedTexture Texture;
        RichTextComponentBase[] comps;

        public override string PageCode => pageCode;

        public override string CategoryCode => categoryCode;

        public override bool IsDuplicate => false;

        public GuiHandbookTraitTypesPage(ICoreClientAPI capi, int type, List<ExtendedTrait> traits)
        {
            this.capi = capi;
            this.type = type;
            this.typeName = ((EnumTraitType)type).ToString();
            this.traits = traits;

            Title = Lang.Get("traittypename-" + typeName).ToSearchFriendly();

            pageCode = "TraitTypeInfo-" + typeName;

            comps = VtmlUtil.Richtextify(capi, PageInfo(), CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2));
        }

        private string PageInfo()
        {
            StringBuilder fulldesc = new StringBuilder();

            string colour;
            switch ((EnumTraitType)type)
            {
                case EnumTraitType.Positive:
                    colour = "color=\"#84ff84\""; //Green
                    break;
                case EnumTraitType.Negative:
                    colour = "color=\"#ff8484\""; //Red
                    break;
                default: //Default case for "Mixed" traits
                    colour = "color=\"#e6e600\""; //Yellow
                    break;
            }

            fulldesc.AppendLine($"<font size=\"24\" {colour}><strong>" + Title + "</strong></font>\n");
            //fulldesc.AppendLine($"PageCode: {pageCode}");
            foreach (ExtendedTrait trait in traits)
            {
                if (trait.Type == (EnumTraitType)type)
                {
                    fulldesc.AppendLine($"<a href=\"handbook://ExtendedTraitInfo-{trait.Code}\">" + Lang.Get("traitname-" + trait.Code) + "</a>");
                }
            }
            return fulldesc.ToString();
        }

        public override void ComposePage(GuiComposer detailViewGui, ElementBounds textBounds, ItemStack[] allstacks, ActionConsumable<string> openDetailPageFor)
        {
            detailViewGui.AddRichtext(comps, textBounds, "richtext");
        }

        public void Recompose(ICoreClientAPI capi)
        {
            Texture?.Dispose();
            Texture = new TextTextureUtil(capi).GenTextTexture(Title, CairoFont.WhiteSmallText());
        }

        public override void Dispose() { Texture?.Dispose(); Texture = null; }

        public override float GetTextMatchWeight(string searchText)
        {
            if (Title.Equals(searchText, StringComparison.InvariantCultureIgnoreCase)) return 4;
            if (Title.StartsWith(searchText + " ", StringComparison.InvariantCultureIgnoreCase)) return 3.5f;
            if (Title.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase)) return 3f;
            if (Title.CaseInsensitiveContains(searchText)) return 2.75f;
            if (Text.CaseInsensitiveContains(searchText)) return 1.25f;
            return 0;
        }

        public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
        {
            float size = (float)GuiElement.scaled(25);
            float pad = (float)GuiElement.scaled(10);

            if (Texture == null)
            {
                Recompose(capi);
            }

            capi.Render.Render2DTexturePremultipliedAlpha(
                Texture.TextureId,
                (x + pad),
                y + size / 4 - 3,
                Texture.Width,
                Texture.Height,
                50
            );
        }
    }
}