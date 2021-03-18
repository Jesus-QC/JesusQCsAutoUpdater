using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.IO;
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
        public override Version Version { get; } = new Version(1, 0, 5, 1);
        public override PluginPriority Priority => PluginPriority.Lowest;

        public bool shouldSendDebug = true;
        public int updatedplugins = 0;
        public List<OnePlugin> FinalPluginList = new List<OnePlugin>();

        public override void OnEnabled()
        {
            shouldSendDebug = Config.IsDebugEnabled;
            updatedplugins = 0;

            PluginList ApiPluginList = GetPluginListByURL($"http://plugins.exiled.host/api/plugins?apikey={Config.ApiKey}");
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
                    FinalPluginList.Add(newPlugin);
                }
            }

            MEC.Timing.CallDelayed(3.0f, () => 
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
                        Log.Debug($"{plugin.Name} is autoupdated / blacklisted, skipping it", shouldSendDebug);
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

        private void CheckVersion(IPlugin<IConfig> plugin)
        {
            Log.Debug($"Checking the version of {plugin.Name}...", shouldSendDebug);
            try
            {
                foreach (OnePlugin pluginInfo in FinalPluginList.Where(p => p.name == plugin.Assembly.GetName().Name))
                {
                    if (plugin.Version < new Version(pluginInfo.latest_version))
                    {
                        Log.Warn(
                            $"{plugin.Name} [{plugin.Version}] is outdated. Latest version: [{pluginInfo.latest_version}]. Updating it...");
                        try
                        {
                            System.IO.File.Copy(plugin.GetPath(), $"{plugin.GetPath()}-backup"); // Creating a backup
                            System.IO.File.Delete(plugin.GetPath());
                            using (var client = new WebClient())
                            {
                                client.DownloadFile(GetLastestVersionURLByID(pluginInfo.id), plugin.GetPath()); 
                            }
                            Log.Info($"{plugin.Name} was updated successfully!");
                        
                            System.IO.File.Delete($"{plugin.GetPath()}-backup"); // Removing the backup
                        }
                        catch (Exception e)
                        {
                            System.IO.File.Copy($"{plugin.GetPath()}-backup", plugin.GetPath()); // Restoring the backup
                            Log.Error($"There was an error updating the plugin {plugin.Name} {e}");
                        }
                    
                    }
                    else
                    {
                        Log.Debug($"{plugin.Name} {plugin.Version} is updated", shouldSendDebug);
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Debug($"There was an error updating {plugin.Name} {plugin.Version} | {e}", shouldSendDebug);
                throw;
            }
            Log.Debug($"The plugin {plugin.Name}, isn't in the list.", shouldSendDebug);
        }

        private void AllPluginsUpdated()
        {
            if (updatedplugins == Loader.Plugins.Count)
            {
                Log.Warn("All plugins are updated!");
            }
        }

        public PluginList GetPluginListByURL(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();
            var responseReader = new StreamReader(webStream);
            var response = responseReader.ReadToEnd();
            PluginList deserializedClass = Utf8Json.JsonSerializer.Deserialize<PluginList>(response);
            responseReader.Close();
            return deserializedClass;
        }

        public string GetLastestVersionURLByID(int id)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://plugins.exiled.host/api/plugins/{id}");
            request.Method = "GET";
            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();
            var responseReader = new StreamReader(webStream);
            var response = responseReader.ReadToEnd();
            FinalPluginInfo deserializedClass = Utf8Json.JsonSerializer.Deserialize<FinalPluginInfo>(response);
            responseReader.Close();
            return deserializedClass.success.latest_download_link;
        }

        // This is the json object that contains the information of the updated plugin.
        public class FinalPluginInfo
        {
            public Success success { get; set; }
        }

        public class Success
        {
            public string latest_download_link { get; set; }
        }

        // This is the json object that contains the information needed of each plugin that is in the api.
        public class PluginList
        {
            public List<ApiPluginInfo> success { get; set; }
        }
        public class ApiPluginInfo
        {
            public int id { get; set; }
            public string latest_version { get; set; }
            public List<File> files { get; set; }
        }
        public class File
        {
            public string file_name { get; set; }
        }
        
        // This is an object to save the data of an individual plugin with its latest version
        public class OnePlugin
        {
            public string name { get; set; }
            public string latest_version { get; set; }
            public int id { get; set; }
        }
    }
}
