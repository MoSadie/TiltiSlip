﻿using BepInEx;
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
using System.Security.Cryptography;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TiltiSlip
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Slipstream_Win.exe")]
    public class Plugin : BaseUnityPlugin, MoCore.MoPlugin
    {
        private static ConfigEntry<int> port;

        private static ConfigEntry<bool> debugLogs;

        private static ConfigEntry<string> webhookKey;

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

            port = Config.Bind("Webhook", "Port", 4000);
            webhookKey = Config.Bind("Webhook", "Webhook Key", "default");

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

                // Validation check

                // Check for required headers
                bool hasSignature = request.Headers.AllKeys.Contains("X-Tiltify-Signature");
                bool hasTimestamp = request.Headers.AllKeys.Contains("X-Tiltify-Timestamp");

                // print all headers
                foreach (string key in request.Headers.AllKeys)
                {
                    debugLogInfo($"{key}: {request.Headers.Get(key)}");
                }

                if (!hasSignature || !hasTimestamp)
                {
                    status = HttpStatusCode.OK; // This is so the webhook does not keep trying to send the message, which could lead to it being disabled.
                    responseString = "Missing required headers";
                    debugLogWarn($"Missing required headers: Sign: {hasSignature} Timestamp: {hasTimestamp}");
                }
                else
                {
                    // Get X-Tiltify-Signate and X-Tiltify-Timestamp headers
                    string signature = request.Headers.Get("X-Tiltify-Signature");
                    string timestamp = request.Headers.Get("X-Tiltify-Timestamp");

                    // Get the body
                    string body = new System.IO.StreamReader(request.InputStream).ReadToEnd();

                    // Validate the message
                    if (isValidMessage(body, webhookKey.Value, timestamp, signature))
                    {
                        status = HttpStatusCode.OK;
                        responseString = "Valid message";

                        handleTiltifyMessage(body);
                    }
                    else
                    {
                        status = HttpStatusCode.OK; // This is so the webhook does not keep trying to send the message, which could lead to it being disabled.
                        responseString = "Invalid message";
                    }
                }

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

        private void handleTiltifyMessage(string body)
        {
            // Parse the json body
            JObject json = JsonConvert.DeserializeObject<JObject>(body);

            string eventType = json["meta"]["event_type"].Value<string>();

            if (eventType == "public:direct:donation_updated")
            {
                string donorName = json["data"]["donor_name"].Value<string>();
                string amount = json["data"]["amount"]["value"].Value<string>();
                string currency = json["data"]["amount"]["currency"].Value<string>();
                string comment = json["data"]["donor_comment"].Value<string>();

                string message = $"{donorName} donated {amount} {currency} with the comment \"{comment}\"";

                debugLogInfo(message);

                //switch based on the last character of the amount

                char lastChar = amount[amount.Length - 1];

                Actions.recieveOrder($"{donorName} just donated {amount} {currency}", "Tiltify");

                switch (lastChar)
                {
                    default:
                        
                        break;
                        /*
                    case '1':
                        Actions.recieveOrder(comment, donorName);
                        break;
                        */

                    case '2':
                        Actions.sendOrder(comment, donorName);
                        break;

                        /*
                    case '3':
                        Actions.focusRandomCrew(donorName);
                        break;

                    case '4':
                        Actions.focusSelf(donorName);
                        break;
                        */

                    case '5':
                        Actions.goToRandomStation(donorName);
                        break;

                    case '6':
                        Actions.renameShip(comment, donorName);
                        break;

                    case '7':
                        Actions.dropGems(donorName);
                        break;
                }
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

        public static bool isValidMessage(string body, string secret, string timestamp, string signature)
        {
            try
            {
                string payload = timestamp + "." + body;

                HMACSHA256 hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));

                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));

                string computedSignature = Convert.ToBase64String(hash);

                return computedSignature.Equals(signature);
            }
            catch (Exception e)
            {
                Log.LogError("An error occurred while validating the message.");
                Log.LogError(e.Message);
                Log.LogError(e.StackTrace);
                return false;
            }
        }

        private static bool testMessageValidation()
        {
            string signature = "4OSwlhTt0EcrlSQFlqgE18FOtT+EKX4qTJdJeC8oV/o=";
            string timestamp = "2023-04-18T16:49:00.617031Z";
            string body = "{\"data\":{\"amount\":{\"currency\":\"USD\",\"value\":\"82.95\"},\"campaign_id\":\"a4fd5207-bd9f-4712-920a-85f8d92cf4e6\",\"completed_at\":\"2023-04-18T16:48:26.510702Z\",\"created_at\":\"2023-04-18T03:36:36.510717Z\",\"donor_comment\":\"Rerum quo necessitatibus voluptas provident ad molestiae ipsam.\",\"donor_name\":\"Jirachi\",\"fundraising_event_id\":null,\"id\":\"dfa25dcc-2026-4320-a5b7-5da076efeb05\",\"legacy_id\":0,\"poll_id\":null,\"poll_option_id\":null,\"reward_id\":null,\"sustained\":false,\"target_id\":null,\"team_event_id\":null},\"meta\":{\"attempted_at\":\"2023-04-18T16:49:00.617031Z\",\"event_type\":\"public:direct:donation_updated\",\"generated_at\":\"2023-04-18T16:48:59.510758Z\",\"id\":\"d8768e26-1092-4f4c-a829-a2698cd19664\",\"subscription_source_id\":\"00000000-0000-0000-0000-000000000000\",\"subscription_source_type\":\"test\"}}";
            string secret = "13c3b68914487acd1c68d85857ee1cfc308f15510f2d8e71273ee0f8a42d9d00";
            return isValidMessage(body, secret, timestamp, signature);
        }
    }
}
