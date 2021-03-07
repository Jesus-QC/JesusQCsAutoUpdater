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
        public override Version Version { get; } = new Version(1, 0, 3);
        public override PluginPriority Priority => PluginPriority.Lowest;

        public Dictionary<string, Dictionary<string, string>> pluginList;
        public bool shouldSendDebug = true;
        public int updatedplugins = 0;

        public override void OnEnabled()
        {
            shouldSendDebug = Config.IsDebugEnabled;
            updatedplugins = 0;

            GetPluginListURL();

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

        public void GetPluginListURL()
        {
            const string url = "https://jesus-qc.es/autoupdaterapi.json";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();
            var responseReader = new StreamReader(webStream);
            var response = responseReader.ReadToEnd();
            pluginList = Utf8Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(response);
            responseReader.Close();
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
                        if (pluginList.ContainsKey(plugin.Name))
                        {
                            CheckVersion(plugin);
                        }
                        else
                        {
                            Log.Debug(plugin.Name + " is not registered in the plugin list, skipping it.", shouldSendDebug);
                            updatedplugins++;
                        }
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
            try
            {
                Dictionary<string, string> versionList = pluginList[plugin.Name];
                Version listedVersion = new Version(versionList.Keys.FirstOrDefault());
                if (plugin.Version < listedVersion)
                {
                    Log.Warn(plugin.Name + " [" + plugin.Version + "] is outdated. Latest version: [" + listedVersion + "]. Updating it...");

                    try
                    {
                        File.Delete(plugin.GetPath());
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(versionList.Values.FirstOrDefault(), plugin.GetPath());
                        }
                        Log.Info(plugin.Name + " was updated successfully!");
                        updatedplugins++;
                    }
                    catch(Exception e)
                    {
                        Log.Error(plugin.Name + " couldn't be updated " + e);
                    }
                }
                else if (plugin.Version == listedVersion)
                {
                    Log.Debug(plugin.Name + " " + plugin.Version + " is updated", shouldSendDebug);
                    updatedplugins++;
                }
                else
                {
                    Log.Debug(plugin.Name + " " + plugin.Version + " is updated", shouldSendDebug);
                    updatedplugins++;
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        public void AllPluginsUpdated()
        {
            if (updatedplugins == Exiled.Loader.Loader.Plugins.Count)
            {
                Log.Warn("All plugins are updated!");
            }
        }
    }
}
