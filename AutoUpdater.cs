using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

namespace JesusQCsAutoUpdater
{
    public class AutoUpdater : Plugin<Config>
    {
        public override string Name { get; } = "JesusQC's AutoUpdater";
        public override string Author { get; } = "JesusQC";
        public override string Prefix { get; } = "jesusqc-autoupdater";
        public override Version RequiredExiledVersion { get; } = new Version(2, 3, 4);
        public override Version Version { get; } = new Version(1, 0, 5, 2);
        public override PluginPriority Priority => PluginPriority.Lowest;

        public bool shouldSendDebug;
        public int updatedplugins;
        public List<LPlugin> LPluginList = new List<LPlugin>();

        public override void OnEnabled()
        {
            shouldSendDebug = Config.IsDebugEnabled;
            updatedplugins = 0;

            MEC.Timing.CallDelayed(3.0f, () => 
            { 
                Log.Info
                (@"
                    ___
                   |[_]|  JesusQC's AutoUpdater
                   |+ ;|  Searching for updates...
                   `---'");
                CheckForUpdates(); 
                
                if(Config.ApiKey == "00000000000000000000000000000000")
                    Log.Error("Invalid api key");
            });
            
            base.OnEnabled();
        }

        private void CheckForUpdates()
        {
            LPluginList = GetPluginListByURL($"http://plugins.exiled.host/api/plugins?apikey={Config.ApiKey}").success;
            try
            {
                foreach (var plugin in Loader.Plugins)
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
                foreach (LPlugin pluginInfo in LPluginList.Where(p => p.name == plugin.Assembly.GetName().Name))
                {
                    if (plugin.Version < new Version(pluginInfo.latest_version))
                    {
                        Log.Warn(
                            $"{plugin.Name} [{plugin.Version}] is outdated. Latest version: [{pluginInfo.latest_version}]. Updating it...");
                        try
                        {
                            File.Copy(plugin.GetPath(), $"{plugin.GetPath()}.bak");
                            
                            File.Delete(plugin.GetPath());
                            using (var client = new WebClient())
                            {
                                client.DownloadFile($"https://plugins.exiled.host/plugin/{pluginInfo.id}/download/{pluginInfo.latest_file_id}", plugin.GetPath()); 
                            }
                            Log.Info($"{plugin.Name} was updated successfully!");
                        
                            File.Delete($"{plugin.GetPath()}.bak");
                        }
                        catch (Exception e)
                        {
                            File.Copy($"{plugin.GetPath()}.bak", plugin.GetPath());
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

        public class LPlugin
        {
            public int id { get; set; }
            public string name { get; set; }
            public int latest_file_id { get; set; }
            public string latest_version { get; set; }
        }

        public class PluginList
        {
            public List<LPlugin> success { get; set; }
        }
    }
}
