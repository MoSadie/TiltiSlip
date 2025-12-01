using BepInEx;
using Subpixel;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace TiltiSlip
{
    public class Actions
    {
        internal static void recieveOrder(string msg, string user = null, bool overrideActionsEnabled = false)
        {
            if (!(TiltiSlip.ActionsEnabled || overrideActionsEnabled))
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring recieveOrder");
                return;
            }

            try
            {
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    if (user != null)
                    {
                        msg = $"\"{msg}\" -{user}";
                    }

                    OrderVo local = OrderHelpers.CreateLocal(OrderIssuer.Nobody, OrderType.General, msg);
                    Svc.Get<Subpixel.Events.Events>().Dispatch<OrderGivenEvent>(new OrderGivenEvent(local));
                });
            } catch (Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
            
            TiltiSlip.debugLogInfo($"recieveOrder by {user}: {msg}");

            //The "We're live in 10 mins fix", send it to everyone lol
            //sendOrder(msg, user);
        }

        internal static void sendOrder(string msg, string user = null)
        {
            if (!TiltiSlip.ActionsEnabled)
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring sendOrder");
                return;
            }

            try
            {
                if (!GetIsCaptainOrFirstMate())
                {
                    TiltiSlip.debugLogError("Not captain or first mate, cannot send order");
                    return;
                }

                if (user != null)
                {
                    msg = $"\"{msg}\" -{user}";
                }

                RequestCatalog.CaptainIssueOrderAll(OrderType.CustomMessage, msg);
            } catch (Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
        }

        private static bool GetIsCaptainOrFirstMate()
        {
            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    TiltiSlip.Log.LogError("An error occurred handling self crew. null MpSvc.");
                    return false;
                }

                MpClientController clients = Svc.Get<MpSvc>().Clients;

                if (clients == null || clients.LocalClient == null)
                {
                    TiltiSlip.Log.LogError("An error occurred handling self crew. null clients or local client.");
                    return false;
                }
                else
                {
                    return clients.LocalClient.Roles.Has(Roles.Captain) || clients.LocalClient.Roles.Has(Roles.FirstMate);
                }
            }
            catch (Exception e)
            {
                TiltiSlip.Log.LogError($"An error occurred while checking if the crewmate is the captain or first mate: {e.Message}");
                return false;
            }
        }

        internal static void focusRandomCrew(string source)
        {
            if (!TiltiSlip.ActionsEnabled)
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring focusRandomCrew");
                return;
            }

            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    TiltiSlip.debugLogError("mpSvc is null! focusRandomCrew");
                    return;
                }

                List<Crewmate> crew = mpSvc.Crew.CrewmatesOnBoard;

                if (crew == null || crew.Count == 0)
                {
                    TiltiSlip.debugLogError("crew is empty! focusRandomCrew");
                    return;
                }

                TiltiSlip.debugLogInfo($"There are {crew.Count} crewmates");

                // Get a random Crewmate from the collection
                System.Random random = new System.Random();
                int index = random.Next(crew.Count);

                TiltiSlip.debugLogInfo($"Random index is {index}");

                

                TiltiSlip.debugLogInfo("Getting current crewmate");

                Crewmate target = crew[index];

                if (target == null)
                {
                    TiltiSlip.debugLogError("target is null! focusRandomCrew");
                    return;
                }

                TiltiSlip.debugLogInfo($"Target is {target.Client.Player.DisplayName}");

                recieveOrder($"Let's take a look at what {target.Client.Player.DisplayName} is up to...", source);
                Mainstay<CameraOperator>.Main.Movement.CamFollowCrewmate(target);

            }
            catch (Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
        }

        internal static void focusSelf(string source)
        {
            if (!TiltiSlip.ActionsEnabled)
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring focusSelf");
                return;
            }

            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    TiltiSlip.debugLogError("mpSvc is null! focusSelf");
                    return;
                }

                List<Crewmate> crewList = mpSvc.Clients.LocalClient.Crew;

                if (crewList == null || crewList.Count == 0)
                {
                    TiltiSlip.debugLogError("crewList is empty! focusSelf");
                    return;
                }

                Crewmate target = crewList[0];

                recieveOrder("You should take a look at yourself!", source);
                Mainstay<CameraOperator>.Main.Movement.CamFollowCrewmate(target);
            } catch (Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
        }

        internal static void dropGems(string source)
        {
            if (!TiltiSlip.ActionsEnabled)
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring dropGems");
                return;
            }

            try
            {
                GameObject hud = GameObject.Find("GemInventoryHud");

                if (hud == null)
                {
                    TiltiSlip.debugLogError("Gem HUD is null!");
                    return;
                }

                GemInventoryHud gemHud = hud.GetComponent<GemInventoryHud>();

                if (gemHud == null)
                {
                    TiltiSlip.debugLogError("GemInventoryHud is null!");
                    return;
                }

                gemHud.DropItems();
                recieveOrder("Drop it like it's hot!", source);
            } catch (Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
        }

        internal static void renameShip(string name, string source)
        {
            if (!TiltiSlip.ActionsEnabled)
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring renameShip");
                return;
            }

            try
            {
                if (EditableText.IsTextUsable(name) == false)
                {
                    TiltiSlip.debugLogError($"Ship name {name} is not usable!");
                    recieveOrder("I would have renamed the ship, but the name was invalid!", source);
                    return;
                }

                LocalPlayerPrefs.SetShipName(name);
                recieveOrder($"Can the ship be named {name} instead?", source);

                GameObject panel = GameObject.Find("Canvas/PixelPerfectCanvas/DialogManager/DialogArea/CaptainConsole(Clone)/Captain Console Root/Captain Console Row/Column A/Bottom Row/ShipStatusPanel");
                //This cursed string is from RUE, full path to the GameObject when at the helm.

                if (panel == null)
                {
                    // Check the staging area for if we are not at helm
                    panel = GameObject.Find("Canvas/PixelPerfectCanvas/DialogManager/StagingArea/CaptainConsole(Clone)/Captain Console Root/Captain Console Row/Column A/Bottom Row/ShipStatusPanel");
                    //This cursed string is from RUE, full path to the GameObject when not at the helm.
                    if (panel == null)
                    {
                        TiltiSlip.debugLogError("ShipStatusPanel is null!");
                        return;
                    }
                }

                ShipStatsPanel statsPanel = panel.GetComponent<ShipStatsPanel>();

                if (statsPanel == null)
                {
                    TiltiSlip.debugLogError("ShipStatsPanel is null!");
                    return;
                }

                statsPanel.ShipNameText.SetUpInitialText(name);
            } catch (Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
        }

        private readonly static StationType[] types =
        [
            StationType.Weapon,
            StationType.Shield,
            StationType.Medbay,
            StationType.Helm,
            StationType.Engineering,
            StationType.Deposit,
            StationType.Transporter
        ];

        internal static void goToRandomStation(string source)
        {
            if (!TiltiSlip.ActionsEnabled)
            {
                TiltiSlip.Log.LogInfo("Actions are disabled, ignoring goToRandomStation");
                return;
            }

            try
            {
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    MpSvc mpSvc = Svc.Get<MpSvc>();

                    if (mpSvc == null)
                    {
                        TiltiSlip.debugLogError("mpSvc is null! goToRandomStation");
                        return;
                    }


                    int typeIndex = UnityEngine.Random.RandomRangeInt(0, types.Length);
                    StationType type = types[typeIndex];

                    List<Station> stations = Mainstay<StationManager>.Main.GetStationsOfType(type);

                    if (stations == null || stations.Count == 0)
                    {
                        TiltiSlip.debugLogError($"stations is empty for type {type}! goToRandomStation");
                        return;
                    }

                    int StationIndex = UnityEngine.Random.RandomRangeInt(0, stations.Count);

                    Station station = stations[StationIndex];

                    recieveOrder("Look over there!", source);
                    Svc.Get<Subpixel.Events.Events>().Dispatch<StationClick>(new StationClick(station));
                });
            } catch(Exception e)
            {
                TiltiSlip.Log.LogError(e);
            }
        }
    }
}
