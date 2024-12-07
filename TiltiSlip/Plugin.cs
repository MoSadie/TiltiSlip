using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;
using System.Net.Http;
using BepInEx.Logging;
using UnityEngine;
using Newtonsoft.Json;
using Subpixel.Events;
using System;
using System.Net;

namespace TiltiSlip
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Slipstream_Win.exe")]
    public class Plugin : BaseUnityPlugin, MoCore.MoPlugin
    {
        private static ConfigEntry<int> port;

        private static ConfigEntry<bool> debugLogs;

        private static HttpListener listener;

        internal static ManualLogSource Log;

        public static readonly string COMPATIBLE_GAME_VERSION = "4.1595"; // Grab from log file for each game update.
        public static readonly string GAME_VERSION_URL = "https://raw.githubusercontent.com/MoSadie/tiltislip/refs/heads/main/versions.json";

        private void Awake()
        {
            Plugin.Log = base.Logger;

            if (!MoCore.MoCore.RegisterPlugin(this))
            {
                Log.LogError("Failed to register plugin with MoCore. Please check the logs for more information.");
                return;
            }

            port = Config.Bind("Websocket", "Port", 4000);

            debugLogs = Config.Bind("Debug", "Debug Logs", false);

            if (!HttpListener.IsSupported)
            {
                Log.LogError("HttpListener is not supported on this platform.");
                listener = null;
                return;
            }

            // Start the http server
            listener = new HttpListener();

            listener.Prefixes.Add($"http://127.0.0.1:{port.Value}/tiltislip/");
            listener.Prefixes.Add($"http://localhost:{port.Value}/tiltislip/");

            listener.Start();

            listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);

            //Harmony.CreateAndPatchAll(typeof(Plugin));

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            
                      

            Application.quitting += ApplicationQuitting;

        }

        internal static void debugLogInfo(string message)
        {
            if (debugLogs.Value)
            {
                Log.LogInfo(message);
            }
        }

        internal static void debugLogWarn(string message)
        {
            if (debugLogs.Value)
            {
                Log.LogWarning(message);
            }
        }

        internal static void debugLogError(string message)
        {
            if (debugLogs.Value)
            {
                Log.LogError(message);
            }
        }

        internal static void debugLogDebug(string message)
        {
            if (debugLogs.Value)
            {
                Log.LogDebug(message);
            }
        }

        private void HandleRequest(IAsyncResult result)
        {
            debugLogInfo("Handling request");
            try
            {
                HttpListener listener = (HttpListener)result.AsyncState;

                HttpListenerContext context = listener.EndGetContext(result);

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                HttpStatusCode status;
                string responseString;

                string pathUrl = request.RawUrl.Split('?', 2)[0];


                // Parse stuff here
                // - Replace this
                status = HttpStatusCode.OK;
                responseString = string.Empty;
                // -

                response.StatusCode = (int)status;

                response.Headers.Add("Access-Control-Allow-Origin", "*");

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();


                // Start listening for the next request
                listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while handling the request.");
                Log.LogError(e.Message);
                Log.LogError(e.StackTrace);
            }
        }

        private void ApplicationQuitting()
        {
            Logger.LogInfo("Stopping server");
            // Stop server
            listener.Close();
        }

        public string GetCompatibleGameVersion()
        {
            return COMPATIBLE_GAME_VERSION;
        }

        public string GetVersionCheckUrl()
        {
            return GAME_VERSION_URL;
        }

        public BaseUnityPlugin GetPluginObject()
        {
            return this;
        }

        private GemInventoryHud getGemInventoryHud()
        {
            return GameObject.Find("GemInventoryHud").GetComponent<GemInventoryHud>();
        }
    }
}
