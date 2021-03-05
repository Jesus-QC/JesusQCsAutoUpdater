using Exiled.API.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace JesusQCsAutoUpdater
{
    public sealed class Config : IConfig
    {
        [Description("Is the plugin enabled?")]
        public bool IsEnabled { get; set; } = true;

        [Description("Is debug enabled?")]
        public bool IsDebugEnabled { get; set; } = false;

        [Description("Plugin blacklist, this plugins wont be updated automatically (Use the name of the plugin in the config [Prefix]).")]
        public List<string> pluginBlacklist { get; set; } = new List<string>
        {
            "scp_stats", "vs",
        };
    }
}
