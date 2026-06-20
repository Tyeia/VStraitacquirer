using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System.Linq;
using System;
using System.Data;
using System.Text;
using System.Numerics;

namespace traitacquirermoddedclasses
{
    public class traitacquirerModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        ICoreAPI? api;
        ICoreClientAPI? capi;
        ICoreServerAPI? sapi;
        
        public List<ExtendedTrait> traits = new List<ExtendedTrait>();
        public List<CharacterClass> characterClasses = new List<CharacterClass>();
        public Dictionary<string, ExtendedTrait> TraitsByCode = new Dictionary<string, ExtendedTrait>();
        public Dictionary<string, CharacterClass> characterClassesByCode = new Dictionary<string, CharacterClass>();
        GuiDialogCharacterBase? charDlg;
        
        GuiElementRichtext? richtextElem;
        ElementBounds? clippingBounds;
        ElementBounds? scrollbarBounds;
        public override void Start(ICoreAPI api)
        {
            this.api = api;
            //Register Classes
            api.RegisterItemClass(Mod.Info.ModID + ".ItemTraitManual", typeof(ItemTraitManual));
            //Load Config
            traitacquirerConfig.loadConfig(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            loadCharacterClasses();
            api.Event.RegisterEventBusListener(AcquireTraitEventHandler, 0.5, "traitItem");
            acquireTraitCommand();
            giveTraitCommand();
            //listTraitsCommand();
        }

        public void acquireTraitCommand()
        {
            if (sapi == null || api == null) return;
            var parsers = sapi.ChatCommands.Parsers;
            sapi.ChatCommands.GetOrCreate("acquireTrait")
            .WithAlias("at")
            .WithDescription(Lang.Get("traitacquirer-acquiretraitcommand-desc"))//"Gives the caller the given Trait, removes with the rm flag, overrides exclusivity with the force flag")
            .RequiresPrivilege(this.api.World.Config.GetString("acquireCmdPrivilege"))
            .RequiresPlayer()
            .WithArgs(parsers.Word("trait name"), parsers.OptionalBool("remove flag", "rm"), parsers.OptionalBool("force flag", "f"))
            .HandleWith((args) =>
            {
                var byEntity = args.Caller.Entity;
                string exitMessage;
                string? traitName = args[0]?.ToString();
                if (string.IsNullOrWhiteSpace(traitName))
                {
                    return TextCommandResult.Error("No Trait specified");
                }
                bool success;
                bool remove = false;
                bool force = false;
                if (!args.Parsers[1].IsMissing) { remove = (bool)args[1]; }
                if (!args.Parsers[2].IsMissing) { force = (bool)args[2]; }
                if (traits.Find(x => x.Code == traitName) == null)
                {
                    return TextCommandResult.Error("Trait does not exist");
                }
                IPlayer? byPlayer = null;
                if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                if(byPlayer == null)
                {
                    return TextCommandResult.Error("No player found for caller");
                }
                if(traitName == null)
                {
                    return TextCommandResult.Error("No Trait specified");
                }
                if (remove)
                {
                    success = processTraits(byPlayer.PlayerUID, new string[0], new string[] { traitName }, force);
                    exitMessage = "Trait Removed";
                }
                else
                {
                    success = processTraits(byPlayer.PlayerUID, new string[] { traitName }, new string[0], force);
                    exitMessage = "Trait given";
                }
                if (!success)
                {
                    return TextCommandResult.Error("Unable to execute Command");
                }
                return TextCommandResult.Success(exitMessage);
            });
        }

        public void giveTraitCommand()
        {
            if (sapi == null || api == null) return;
            var parsers = sapi.ChatCommands.Parsers;
            sapi.ChatCommands.GetOrCreate("giveTrait")
            .WithAlias("gt")
            .WithDescription(Lang.Get("traitacquirer-givecommand-desc"))//"Gives the given Trait to the chosen player, removes with the rm flag, overrides exclusivity with the force flag")
            .RequiresPrivilege(this.api.World.Config.GetString("giveCmdPrivilege"))
            .RequiresPlayer()
            .WithArgs(parsers.Word("trait name"), parsers.OnlinePlayer("target player"), parsers.OptionalBool("remove flag", "rm"), parsers.OptionalBool("force flag", "f"))
            .HandleWith((args) =>
            {
                IServerPlayer targetPlayer = (IServerPlayer)args[1];
                var byEntity = args.Caller.Entity;
                string exitMessage;
                bool success;
                string? traitName = args[0]?.ToString();
                if (string.IsNullOrWhiteSpace(traitName))
                {
                    return TextCommandResult.Error("No Trait specified");
                }
                bool remove = false;
                bool force = false;
                if (!args.Parsers[2].IsMissing) { remove = (bool)args[2]; }
                if (!args.Parsers[3].IsMissing) { force = (bool)args[3]; }
                if (traits.Find(x => x.Code == traitName) == null)
                {
                    return TextCommandResult.Error("Trait does not exist");
                }
                if(targetPlayer == null)
                {
                    return TextCommandResult.Error("No player found for caller");
                }
                if(traitName == null)
                {
                    return TextCommandResult.Error("No Trait specified");
                }
                if (remove)
                {
                    success = processTraits(targetPlayer.PlayerUID, new string[0], new string[] { traitName }, force);
                    exitMessage = "Trait Removed";
                }
                else
                {
                    success = processTraits(targetPlayer.PlayerUID, new string[] { traitName }, new string[0], force);
                    exitMessage = "Trait Given";
                }
                if (!success)
                {
                    return TextCommandResult.Error("Unable to execute Command");
                }
                return TextCommandResult.Success(exitMessage);
            });
        }

        public void listTraitsCommand()
        {
            if (sapi == null || api == null) return;
            var parsers = sapi.ChatCommands.Parsers;
            sapi.ChatCommands.GetOrCreate("listTraits")
            .WithAlias("lt")
            .WithDescription("Returns a sorted list of the loaded trait codes")
            .RequiresPrivilege(this.api.World.Config.GetString("listCmdPrivelege"))
            .RequiresPlayer()
            .HandleWith((args) =>
            {
                List<string> traitList = new();
                foreach (ExtendedTrait trait in traits)
                {
                    traitList.Add(trait.Code);
                }
                traitList.Sort();
                string returnString = "";
                foreach (string traitName in traitList)
                {
                    returnString += $"{traitName}\n";
                }
                return TextCommandResult.Success(returnString);
            });
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            loadCharacterClasses();
            charDlg = api.Gui.LoadedGuis.Find(dlg => dlg is GuiDialogCharacterBase) as GuiDialogCharacterBase;
            if(charDlg == null)
            {
                api.Logger.Error("Failed to find Character Dialog. Traits Tab will not be added.");
                return;
            }
            charDlg.RenderTabHandlers.Add(composeTraitsTab);
            
            api.Event.BlockTexturesLoaded += cleanupTraitsTab;

            //Generate Handbook Pages
            api.ModLoader.GetModSystem<ModSystemSurvivalHandbook>().OnInitCustomPages += traitacquirerModSystem_OnInitCustomPages;
        }

        public void traitacquirerModSystem_OnInitCustomPages(List<GuiHandbookPage> pages)
        {
            if(capi == null)
            {
                return;
            }
            foreach (ExtendedTrait trait in traits) //Generate a page for each trait
            {
                pages.Add(new GuiHandbookExtendedTraitPage(capi, trait));
            }
            foreach (int type in Enum.GetValues(typeof(EnumTraitType))) //Generate a page for each type of trait
            {
                pages.Add(new GuiHandbookTraitTypesPage(capi, type, traits));
            }
        }

        private void cleanupTraitsTab()
        {
            if (api == null) return;
            if (charDlg == null) {
                api.Logger.Error("Character Dialog not found. Cannot clean up Traits Tab.");
                return; 
            }
            foreach (Action<GuiComposer> i in charDlg.RenderTabHandlers)
            {
                if (i.Target != null && i.Target.ToString() == "Vintagestory.GameContent.CharacterSystem")
                {
                    charDlg.RenderTabHandlers.Remove(i);
                    break;
                }
            }
        }

        private void composeTraitsTab(GuiComposer compo)
        {

            this.clippingBounds = ElementBounds.Fixed(0, 25, 385, 310);
            compo.BeginClip(clippingBounds);
            compo.AddRichtext(getClassTraitText(), CairoFont.WhiteDetailText().WithLineHeightMultiplier(1.15), ElementBounds.Fixed(0, 0, 385, 310), "text");
            compo.EndClip();
            this.scrollbarBounds = clippingBounds.CopyOffsetedSibling(clippingBounds.fixedWidth - 3, -6).WithFixedWidth(6).FixedGrow(0, 2);
            compo.AddVerticalScrollbar(OnNewScrollbarValue, this.scrollbarBounds, "scrollbar");
            this.richtextElem = compo.GetRichtext("text");

            compo.GetScrollbar("scrollbar").SetHeights(
                (float)100, (float)310
            );
        }
        private void OnNewScrollbarValue(float value)
        {
            if (richtextElem == null || clippingBounds == null || scrollbarBounds == null) return;
            richtextElem.Bounds.fixedY = 10 - value;
            richtextElem.Bounds.CalcWorldBounds();

        }

        string getClassTraitText()
        {
            if(capi == null || api == null)
            {
                return "Error loading API";
            }
            if(capi.World.Player == null)
            {
                return "Error loading player data";
            }
            string charClass = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
            if (charClass == null)
            {
                return "Error loading character class";
            }
            CharacterClass? chclass = characterClasses.FirstOrDefault(c => c.Code == charClass);
            if(chclass == null)
            {
                return "Error loading character class data";
            }

            StringBuilder fulldesc = new StringBuilder();
            StringBuilder attributes = new StringBuilder();

            fulldesc.AppendLine(Lang.Get("Class Traits: "));

            var chartraits = chclass.Traits.Select(code => TraitsByCode[code]).OrderBy(trait => (int)trait.Type);

            foreach (var trait in chartraits)
            {
                attributes.Clear();
                foreach (var val in trait.Attributes)
                {
                    if (attributes.Length > 0) attributes.Append(", ");
                    attributes.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", val.Key, val.Value)));
                }

                if (attributes.Length > 0)
                {
                    fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + trait.Code), attributes));
                }
                else
                {
                    string desc = Lang.Get("traitdesc-" + trait.Code);
                    if (desc != null)
                    {
                        fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + trait.Code), desc));
                    }
                    else
                    {
                        fulldesc.AppendLine(Lang.Get("trait-" + trait.Code));
                    }


                }
            }

            if (chclass.Traits.Length == 0)
            {
                fulldesc.AppendLine(Lang.Get("No positive or negative traits"));
            }

            fulldesc.AppendLine(Lang.Get("Extra Traits: "));

            string[] extraTraits = capi.World.Player.Entity.WatchedAttributes.GetStringArray("extraTraits");
            IOrderedEnumerable<string> extratraits = Enumerable.Empty<string>().OrderBy(x => 1); ;
            
            if (extraTraits != null)
            {
                extratraits = extraTraits.OrderBy(code => (int)TraitsByCode[code].Type);
                if(extratraits.Count() == 0)
                {
                    fulldesc.AppendLine(Lang.Get("no-extra-traits"));
                }
            }
            else
            {
                fulldesc.AppendLine(Lang.Get("no-extra-traits"));
            }

            foreach (var code in extratraits)
            {
                attributes.Clear();
                foreach (var val in TraitsByCode[code].Attributes)
                {
                    if (attributes.Length > 0) attributes.Append(", ");
                    attributes.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", val.Key, val.Value)));
                }

                if (attributes.Length > 0)
                {
                    fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + code), attributes));
                }
                else
                {
                    string desc = Lang.Get("traitdesc-" + code);
                    if (desc != null)
                    {
                        fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + code), desc));
                    }
                    else
                    {
                        fulldesc.AppendLine(Lang.Get("trait-" + code));
                    }


                }
            }

            return fulldesc.ToString();
        }

        public void AcquireTraitEventHandler(string eventName, ref EnumHandling handling, IAttribute data)
        {
            if (api == null) return;
            TreeAttribute? tree = data as TreeAttribute;
            if (tree == null)
            {
                handling = EnumHandling.PreventSubsequent;
                return;
            }
            string playerUid = tree.GetString("playeruid");
            IPlayer player = api.World.PlayerByUid(playerUid);
            string[] addtraits = tree.GetStringArray("addtraits");
            string[] removetraits = tree.GetStringArray("removetraits");
            int itemslotId = tree.GetInt("itemslotId", -1);
            ItemSlot itemslot = player.InventoryManager.GetHotbarInventory()[itemslotId];
            bool success = processTraits(playerUid, addtraits, removetraits);
            if (success)
            {
                itemslot.TakeOut(1);
                itemslot.MarkDirty();
            }
        }

        public bool processTraits(string playerUid, string[] addtraits, string[] removetraits, bool force = false)
        {
            if (api == null) return false;
            if (addtraits == null){
                api.Logger.Error("AddTraits is Null", Lang.Get("AddTraits is Null"));
                return false;
            }
            if (removetraits == null){
                api.Logger.Error("RemoveTraits is Null", Lang.Get("RemoveTraits is Null"));
                return false;
            }
            IServerPlayer? plr = api.World.PlayerByUid(playerUid) as IServerPlayer;
            if(plr == null)
            {
                return false;
            }
            List<string> newExtraTraits = new List<string>();
            string[] extraTraits = plr.Entity.WatchedAttributes.GetStringArray("extraTraits");
            List<string> incompatibleTraits = new List<string>();

            //Keep traits already added
            if (extraTraits != null)
            {
                newExtraTraits.AddRange(extraTraits);
            }

            //Remove traits from the updated list
            foreach (string traitName in removetraits)
            {
                ExtendedTrait? trait = traits.Find(x => x.Code == traitName);
                if (trait == null)
                {
                    plr.SendIngameError("Trait is Null", Lang.Get("Trait is Null"));
                    return false;
                }
                if (newExtraTraits.Contains(traitName))
                {
                    newExtraTraits.Remove(traitName);
                }
            }

            //Build the new list of traits you'll possess
            foreach (string traitName in addtraits)
            {
                ExtendedTrait? trait = traits.Find(x => x.Code == traitName);
                if (trait == null)
                {
                    plr.SendIngameError("Trait is Null", Lang.Get("Trait is Null"));
                    return false;
                }
                if (!newExtraTraits.Contains(traitName))
                {
                    newExtraTraits.Add(traitName);
                }
            }

            if (!force)
            {
                //Determine which traits are incompatible with the updated trait list
                foreach (string traitName in newExtraTraits)
                {
                    ExtendedTrait? trait = traits.Find(x => x.Code == traitName);
                    if(trait == null)
                    {
                        plr.SendIngameError("Trait is Null", Lang.Get("Trait is Null"));
                        return false;
                    }
                    if (trait.ExclusiveWith != null)
                    {
                        incompatibleTraits.AddRange(trait.ExclusiveWith);
                    }
                }

                //Determine whether there are any incompatibilities in the new list and fail the change
                foreach (string traitName in newExtraTraits)
                {
                    if (incompatibleTraits.Contains(traitName))
                    {
                        plr.SendIngameError("Trait is Incompatible", Lang.Get("Trait is Incompatible"));
                        return false;
                    }
                }
            }

            //Update the trait list and apply their effects
            plr.Entity.WatchedAttributes.SetStringArray("extraTraits", newExtraTraits.ToArray());
            plr.Entity.WatchedAttributes.MarkPathDirty("extraTraits");
            applyTraitAttributes(plr.Entity, addtraits, removetraits);
            plr.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/writing"), plr.Entity);
            return true;
        }

        private void applyTraitAttributes(EntityPlayer eplr, string[] addtraits, string[] removetraits)
        {
            if(characterClasses == null || TraitsByCode == null)
            {
                return;
            }
            string classcode = eplr.WatchedAttributes.GetString("characterClass");
            CharacterClass? charclass = characterClasses.FirstOrDefault(c => c.Code == classcode);
            if (charclass == null) throw new ArgumentException("Not a valid character class code!");

            //Remove trait attributes
            foreach (string traitcode in removetraits)
            {
                ExtendedTrait trait = TraitsByCode[traitcode];
                foreach (var attr in trait.Attributes)
                {
                    eplr.Stats[attr.Key].Remove($"trait_{traitcode}");
                }
            }

            //Add trait attributes
            foreach (string traitcode in addtraits)
            {
                ExtendedTrait trait = TraitsByCode[traitcode];
                foreach (var attr in trait.Attributes)
                {
                    eplr.Stats.Set(attr.Key, $"trait_{traitcode}", (float)attr.Value, true);
                }
            }

            //Mark Dirty
            eplr.GetBehavior<EntityBehaviorHealth>()?.MarkDirty();

            /*
            // Reset 
            foreach (var stat in eplr.Stats)
            {
                foreach (var statmod in stat.Value.ValuesByKey)
                {
                    if (statmod.Key.Length >= 5 ? statmod.Key[..5] == "trait" : false)
                    {
                        stat.Value.Remove(statmod.Key);
                    }
                }
            }

            // Then apply
            string[] extraTraits = eplr.WatchedAttributes.GetStringArray("extraTraits");
            var allTraits = extraTraits == null ? charclass.Traits : charclass.Traits.Concat(extraTraits);

            foreach (var traitcode in allTraits)
            {
                ExtendedTrait trait;
                if (TraitsByCode.TryGetValue(traitcode, out trait))
                {
                    foreach (var val in trait.Attributes)
                    {
                        string attrcode = val.Key;
                        double attrvalue = val.Value;

                        eplr.Stats.Set(attrcode, $"trait_{traitcode}", (float)attrvalue, true);
                    }
                }
            }
            
            eplr.GetBehavior<EntityBehaviorHealth>()?.MarkDirty();
            */
        }
        public void loadCharacterClasses()
        {
            if (api == null) return;
            var allTraits = new List<ExtendedTrait>();
            var allCharacterClasses = new List<CharacterClass>();

            // Search all loaded mod assets
            foreach (var pair in api.Assets.AllAssets)
            {
                var assetLoc = pair.Key;  // <-- this is AssetLocation
                var asset = pair.Value;   // <-- this is IAsset

                string path = assetLoc.Path.ToLowerInvariant();
                
                

                if (path.EndsWith("config/traits.json"))
                {
                    try
                    {   
                        //api.World.Logger.Warning($"Trait Acquirer Loading Path: {path};Asset: {asset}");
                        var traits = api.Assets.Get(assetLoc).ToObject<List<ExtendedTrait>>() ?? new List<ExtendedTrait>();
                        api.World.Logger.Warning($"Loaded {traits.Count} Traits from {asset}");
                        
                        allTraits.AddRange(traits);
                        // ======== Build runtime dictionaries ========
                        foreach (var trait in traits)
                        {
                            try
                            {
                                //api.World.Logger.Warning($"Loading {trait.Code}");
                                TraitsByCode[trait.Code] = trait;
                            }
                            catch (Exception e)
                            {
                                api.World.Logger.Warning($"Failed loading trait data from {trait}: {e}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        api.World.Logger.Warning($"Failed loading traits.json from {assetLoc}: {e}");
                    }
                }

                if (path.EndsWith("config/characterclasses.json"))
                {
                    try
                    {
                        //api.World.Logger.Warning($"Trait Acquirer Loading Path: {path};Asset: {asset}");
                        var classes = api.Assets.Get(assetLoc).ToObject<List<CharacterClass>>() ?? new List<CharacterClass>();
                        api.World.Logger.Warning($"Loaded {classes.Count} Classes from {asset}");
                        allCharacterClasses.AddRange(classes);

                        
                        foreach (var charclass in classes)
                        {
                            try
                            {
                                //api.World.Logger.Warning($"Loading {charclass.Code}");
                                characterClassesByCode[charclass.Code] = charclass;

                                foreach (var jstack in charclass.Gear)
                                {
                                    if (!jstack.Resolve(api.World, "character class gear", false))
                                    {
                                        api.World.Logger.Warning($"Unable to resolve character class gear {jstack.Type}:{jstack.Code}. Ignoring.");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                api.World.Logger.Warning($"Failed loading characterclass data from {charclass}: {e}");
                            }
                            
                        }
                    }
                    catch (Exception e)
                    {
                        api.World.Logger.Warning($"Failed loading characterclasses.json from {assetLoc}: {e}");
                    }
                }
            }

            // Save merged results
            this.traits = allTraits;
            this.characterClasses = allCharacterClasses;
        }
    }
}
