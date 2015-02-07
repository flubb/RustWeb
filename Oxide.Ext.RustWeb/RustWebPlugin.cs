using dcodeIO.RustWeb;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Rust.Libraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Oxide.Rust.Plugins
{
    public class RustWebPlugin : CSPlugin
    {
        private static Logger logger = Interface.GetMod().RootLogger;
        private static VersionNumber MinOxideVersion = new VersionNumber(2, 0, 200);
        internal static string RootDir;
        internal static string DataDir;

        private RustWeb rustWeb = null;
        private string oxideDataDir;

        public RustWebPlugin() {
            Name = "rustweb";
            Title = "RustWeb glue for Oxide 2";
            Author = "dcode";
            Version = new VersionNumber(1, 1, 0);
            HasConfig = false;
        }

        protected override void LoadDefaultConfig() {
            throw new NotImplementedException();
        }

        [HookMethod("Init")]
        private void Init() {
            if (Oxide.Core.OxideMod.Version < MinOxideVersion) {
                logger.Write(LogType.Error, "This version of RustWeb requires at least Oxide " + MinOxideVersion + " but this server is running Oxide " + Oxide.Core.OxideMod.Version + ".");
                return;
            }
            logger.Write(LogType.Info, "Initializing RustWeb");

            oxideDataDir = Interface.GetMod().DataDirectory;
            RootDir = Path.GetFullPath(Path.Combine(oxideDataDir, Path.Combine("..", "www")));
            DataDir = Path.GetFullPath(Path.Combine(oxideDataDir, "rustweb"));

            Command cmdlib = Interface.GetMod().GetLibrary<Command>("Command");
            cmdlib.AddConsoleCommand("map.export"      , this, "cmdExport");
            cmdlib.AddConsoleCommand("map.monuments"   , this, "cmdMonuments");
            cmdlib.AddConsoleCommand("map.dumpobjects" , this, "cmdDumpObjects");
        }

        [HookMethod("OnUnload")]
        private void OnUnload() {
            logger.Write(LogType.Warning, "Reloading RustWeb has no effect. To update it, a server restart is inevitable.");
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized() {
        }

        [HookMethod("OnTick")]
        private void OnTick() {
            if (rustWeb == null) {
                if (RustWeb.Instance == null) {
                    logger.Write(LogType.Info, "Starting RustWeb " + RustWeb.Version.ToString(3) + " on "+(string.IsNullOrEmpty(server.ip) ? "*" : server.ip)+":"+server.port+", serving from '" + RootDir + "' ...");
                    rustWeb = new RustWeb(RootDir, DataDir);
                    rustWeb.AddEnvironmentVersion("oxide", Oxide.Core.OxideMod.Version.ToString());
                    rustWeb.OnLog += (sender, message) => {
                        logger.Write(LogType.Info, "[RustWeb] {0}", message);
                    };
                    rustWeb.OnError += (sender, e) => {
                        logger.WriteException("[RustWeb] ERROR: " + e.Message, e.Exception);
                    };
                    rustWeb.Start();
                } else {
                    logger.Write(LogType.Warning, "Reloading RustWeb has no effect. To update it, a server restart is inevitable.");
                    rustWeb = RustWeb.Instance;
                }
            }
            rustWeb.Tick();
        }

        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(Network.Message packet) {
            if (rustWeb != null)
                rustWeb.OnPlayerConnected(packet);
        }

        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(BasePlayer player) {
            if (rustWeb != null)
                rustWeb.OnPlayerSpawn(player);
        }

        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer player) {
            if (rustWeb != null)
                rustWeb.OnPlayerDisconnected(player);
        }

        [HookMethod("OnPlayerChat")]
        private void OnPlayerChat(chat.Arg arg) {
            if (rustWeb != null)
                rustWeb.OnPlayerChat(arg);
        }

        [HookMethod("OnEntityDeath")]
        private void OnEntityDeath(UnityEngine.MonoBehaviour entity, HitInfo hitinfo) {
            if (rustWeb != null)
                rustWeb.OnEntityDeath(entity, hitinfo);
        }

        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist) {
            taglist.Add("rustweb");
        }

        [HookMethod("cmdExport")]
        private void cmdExport(ConsoleSystem.Arg arg) {
            if (rustWeb == null) {
                arg.ReplyWith("Server isn't initialized yet");
                return;
            }
            if (rustWeb == null || arg.connection != null)
                return; // Allow this only from (real) console as the server will most likely hang
            RconUtil.MapExport(arg, oxideDataDir);
        }

        [HookMethod("cmdMonuments")]
        private void cmdMonuments(ConsoleSystem.Arg arg) {
            if (rustWeb == null) {
                arg.ReplyWith("Server isn't initialized yet");
                return;
            }
            if (arg.connection != null)
                return; // Allow this only from (real) console as the server will most likely hang
            RconUtil.MapMonuments(arg);
        }

        [HookMethod("cmdDumpObjects")]
        private void cmdDumpObjects(ConsoleSystem.Arg arg) {
            if (rustWeb == null) {
                arg.ReplyWith("Server isn't initialized yet");
                return;
            }
            if (arg.connection != null)
                return; // Allow this only from (real) console as the server will most likely hang
            RconUtil.MapDumpGameObjects(arg);
        }
    }
}
