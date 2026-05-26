using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace traitacquirermoddedclasses
{
    internal class GuiHandbookExtendedTraitPage : GuiHandbookPage
    {
        public string pageCode;

        public PageText pageText;
        public string categoryCode = "trait";
        public LoadedTexture? Texture;
        RichTextComponentBase[] comps;
        
        public override float SearchWeightOffset {get;}

        public override string PageCode => pageCode;

        public override string CategoryCode => categoryCode;

        public override bool IsDuplicate => false;

        public GuiHandbookExtendedTraitPage(ICoreClientAPI capi, ExtendedTrait trait)
        {
            pageText.Title = Lang.Get("traitname-" + trait.Code).ToSearchFriendly();

            pageCode = "ExtendedTraitInfo-" + trait.Code;

            Texture = new TextTextureUtil(capi).GenTextTexture(pageText.Title, CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2));

            comps = VtmlUtil.Richtextify(capi, PageInfo(capi,trait), CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2));
        }

        private string PageInfo(ICoreClientAPI capi,ExtendedTrait trait)
        {
            StringBuilder fulldesc = new StringBuilder();
            StringBuilder attributes = new StringBuilder();
            StringBuilder exclusivites = new StringBuilder();

            string colour;
            switch (trait.Type)
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

            fulldesc.AppendLine($"<font {colour} size=\"24\"><strong>" + pageText.Title + "</strong></font>\n");
            fulldesc.AppendLine(Lang.Get("traitacquirer-traitcode-text") + $": {trait.Code}");
            fulldesc.AppendLine(Lang.Get("traitacquirer-traittype-text") + $": <a href=\"handbook://TraitTypeInfo-{trait.Type}\">" + Lang.Get("traittypename-" + trait.Type) + "</a>");

            string desc = Lang.GetIfExists("traitdesc-" + trait.Code);
            if (desc != null)
            {
                fulldesc.AppendLine(Lang.Get("traitacquirer-desc-text") + ": ");
                fulldesc.AppendLine(desc);
            }
            
            if(trait.Attributes != null)
            {
                foreach (var val in trait.Attributes)
                {
                    if (attributes.Length > 0) attributes.Append(", ");
                    attributes.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", val.Key, val.Value)));
                }
            }
            else
            {
                capi.Logger.Warning($"{trait.Code} contains null Attributes");
            }
            if (attributes.Length > 0)
            {
                fulldesc.AppendLine(Lang.Get("traitacquirer-attr-text") + ": ");
                fulldesc.Append(attributes);
            }

            if (trait.ExclusiveWith != null) {
                foreach (var val in trait.ExclusiveWith)
                {
                    if (exclusivites.Length > 0) exclusivites.Append(", ");
                    exclusivites.Append($"<a href=\"handbook://ExtendedTraitInfo-{val}\">" + Lang.Get("traitname-" + val) + $"</a>");
                }
                fulldesc.AppendLine(Lang.Get("traitacquirer-exclusive-text") + ": ");
                fulldesc.Append(exclusivites);
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
            Texture = new TextTextureUtil(capi).GenTextTexture(pageText.Title, CairoFont.WhiteSmallText());
        }

        public override void Dispose() { Texture?.Dispose();}

        public float GetTextMatchWeight(string searchText)
        {
            if (pageText.Title.Equals(searchText, StringComparison.InvariantCultureIgnoreCase)) return 4;
            if (pageText.Title.StartsWith(searchText + " ", StringComparison.InvariantCultureIgnoreCase)) return 3.5f;
            if (pageText.Title.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase)) return 3f;
            if (pageText.Title.CaseInsensitiveContains(searchText)) return 2.75f;
            if (pageText.Text.CaseInsensitiveContains(searchText)) return 1.25f;
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

            if(Texture != null)
            {
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

        public override PageText GetPageText()
        {
            return pageText;
        }
        
    }
}
