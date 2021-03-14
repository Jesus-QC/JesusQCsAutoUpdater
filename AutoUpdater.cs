using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace JesusQCsAutoUpdater
{
    public class AutoUpdater : Plugin<Config>
    {
        public override string Name { get; } = "JesusQC's AutoUpdater";
        public override string Author { get; } = "JesusQC";
        public override string Prefix { get; } = "jesusqc-autoupdater";
        public override Version RequiredExiledVersion { get; } = new Version(2, 3, 4);
        public override Version Version { get; } = new Version(1, 0, 4);
        public override PluginPriority Priority => PluginPriority.Lowest;

        public bool shouldSendDebug = true;
        public int updatedplugins = 0;
        public List<OnePlugin> FinalPluginList = new List<OnePlugin>();

        public override void OnEnabled()
        {
            shouldSendDebug = Config.IsDebugEnabled;
            updatedplugins = 0;

            PluginList ApiPluginList = GetPluginListByURL("https://plugins.exiled.host/api/plugins?apikey=" + Config.ApiKey);
            foreach (ApiPluginInfo pluginInfo in ApiPluginList.success)
            {
                foreach (File file in pluginInfo.files)
                {
                    OnePlugin newPlugin = new OnePlugin
                    {
                        name = file.file_name,
                        latest_version = pluginInfo.latest_version,
                        id = pluginInfo.id
                    };
                    Log.Info(file.file_name + "." + file.file_extension + " - added + " + pluginInfo.latest_version);
                    FinalPluginList.Add(newPlugin);
                }
            }

            MEC.Timing.CallDelayed(3, () => 
            { 
                Log.Info
                (@"
                    ___
                   |[_]|  JesusQC's AutoUpdater
                   |+ ;|  Searching for updates...
                   `---'");
                CheckForUpdates(); 
            });

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
        }

        public void CheckForUpdates()
        {
            try
            {
                foreach (IPlugin<IConfig> plugin in Loader.Plugins)
                {
                    if (plugin.Name.Contains("Exiled.") || Config.pluginBlacklist.Contains(plugin.Prefix))
                    {
                        Log.Debug(plugin.Name + " is autoupdated, skipping it", shouldSendDebug);
                        updatedplugins++;
                    }
                    else
                    {
                        CheckVersion(plugin);
                        updatedplugins++;
                    }
                    AllPluginsUpdated();
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        public void CheckVersion(IPlugin<IConfig> plugin)
        {
            Log.Debug("Checking the version of " + plugin.Name, shouldSendDebug);

            foreach (OnePlugin pluginInfo in FinalPluginList.Where(p => p.name == plugin.Assembly.GetName().Name))
            {
                if (plugin.Version < new Version(pluginInfo.latest_version))
                {
                    Log.Warn(plugin.Name + " [" + plugin.Version + "] is outdated. Latest version: [" + pluginInfo.latest_version + "]. Updating it...");
                    using (var client = new WebClient())
                    {
                        System.IO.File.Delete(plugin.GetPath());
                        client.DownloadFile(GetLastestVersionByID(pluginInfo.id).success.latest_download_link, plugin.GetPath());
                        Log.Info(plugin.Name + " was updated successfully!");
                    }
                }
                else if (plugin.Version >= new Version(pluginInfo.latest_version))
                {
                    Log.Debug(plugin.Name + " " + plugin.Version + " is updated", shouldSendDebug);
                }
                return;
            }
        }

        public void AllPluginsUpdated()
        {
            if (updatedplugins == Loader.Plugins.Count)
            {
                Log.Warn("All plugins are updated!");
            }
        }

        public PluginList GetPluginListByURL(string url)
        {
            using (var client = new WebClient())
            {
                string response = client.DownloadString(url);
                PluginList deserializedClass = Utf8Json.JsonSerializer.Deserialize<PluginList>(response);
                return deserializedClass;
            }
        }

        public FinalPluginInfo GetLastestVersionByID(int id)
        {
            using (var client = new WebClient())
            {
                string response = client.DownloadString(new Uri("https://plugins.exiled.host/api/plugins/" + id));
                FinalPluginInfo deserializedClass = Utf8Json.JsonSerializer.Deserialize<FinalPluginInfo>(response);
                return deserializedClass;
            }
        }

        public class OnePlugin
        {
            public string name { get; set; }
            public string latest_version { get; set; }
            public int id { get; set; }
        }

        public class Success
        {
            public string latest_version { get; set; }
            public string latest_exiled_version { get; set; }
            public string latest_download_link { get; set; }
        }

        public class FinalPluginInfo
        {
            public Success success { get; set; }
        }

        public class User
        {
            public object id { get; set; }
            public string nickname { get; set; }
        }

        public class File
        {
            public int file_id { get; set; }
            public int plugin_id { get; set; }
            public int type { get; set; }
            public string file_name { get; set; }
            public string file_extension { get; set; }
            public int file_size { get; set; }
            public string upload_time { get; set; }
            public string exiled_version { get; set; }
            public string version { get; set; }
            public int downloads_count { get; set; }
            public string changelog { get; set; }
        }

        public class Categoryobj
        {
            public int id { get; set; }
            public string category_name { get; set; }
            public string category_color { get; set; }
        }

        public class ApiPluginInfo
        {
            public int id { get; set; }
            public string name { get; set; }
            public string image_url { get; set; }
            public string small_description { get; set; }
            public string description { get; set; }
            public string wiki_url { get; set; }
            public string issues_url { get; set; }
            public string source_url { get; set; }
            public int latest_file_id { get; set; }
            public string latest_exiled_version { get; set; }
            public string latest_version { get; set; }
            public string last_update { get; set; }
            public string creation_date { get; set; }
            public int downloads_count { get; set; }
            public User user { get; set; }
            public List<File> files { get; set; }
            public Categoryobj categoryobj { get; set; }
        }

        public class PluginList
        {
            public List<ApiPluginInfo> success { get; set; }
        }
    }
}
