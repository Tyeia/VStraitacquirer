using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace traitacquirermoddedclasses
{
    internal class traitacquirerConfig
    {
        public traitacquirerConfig() { }
        public traitacquirerConfig(Dictionary<string, dynamic> config)
        {
            foreach (var key in config.Keys)
            {
                this.configurables[key] = config[key];
            }
        }

        public Dictionary<string, dynamic> configurables = new Dictionary<string, dynamic> {
            {"acquireCmdPrivilege", "gamemode"},
            {"giveCmdPrivilege", "root"},
            {"listCmdPrivilege", "chat"},
            {"classManuals", true},
            {"manualsAvgPrice", 10f},
            {"manualsVarPrice", 4f},
            {"manualsAvgStock", 1f},
            {"manualsVarStock", 0.25f},
            {"manualsAvgLoot", 1f},
            {"manualsVarLoot", 0.25f},
            {"manualsChanceLoot", 30f}
        };

        public static traitacquirerConfig GetDefault()
        {

            traitacquirerConfig config = new traitacquirerConfig();
            return config;
        }

        public static void loadConfig(ICoreAPI api)
        {
            traitacquirerConfig traitacquirerConfig = null;
            try
            {
                traitacquirerConfig = new traitacquirerConfig(api.LoadModConfig<Dictionary<string, dynamic>>("traitacquirer.json"));
                if (traitacquirerConfig != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    traitacquirerConfig = traitacquirerConfig.GetDefault();
                }
            }
            catch
            {
                traitacquirerConfig = traitacquirerConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig<Dictionary<string, dynamic>>(traitacquirerConfig.configurables, "traitacquirer.json");
            }
            setConfig(api, traitacquirerConfig);
        }
        public static void setConfig(ICoreAPI api, traitacquirerConfig traitacquirerConfig)
        {
            foreach (var config in traitacquirerConfig.configurables)
            {
                switch (config.Value)
                {
                    case int v:
                        api.World.Config.SetInt(config.Key, v);
                        break;
                    case double v:
                        api.World.Config.SetDouble(config.Key, v);
                        break;
                    case float v:
                        api.World.Config.SetFloat(config.Key, v);
                        break;
                    case string v:
                        api.World.Config.SetString(config.Key, v);
                        break;
                    case bool v:
                        api.World.Config.SetBool(config.Key, v);
                        break;
                    default:
                        throw new NotImplementedException("Type of config value is not handled");
                }
            }
        }
    }
}
