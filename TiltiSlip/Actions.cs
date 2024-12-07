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

        //TODO
        // - Program each action
        // - Add recieve order for each action

        internal static void recieveOrder(string msg, string user = null)
        {
            try
            {
                if (user != null)
                {
                    msg = $"\"{msg}\" -{user}";
                }

                OrderVo local = OrderHelpers.CreateLocal(OrderIssuer.Self, OrderType.CustomMessage, msg);
                Svc.Get<Subpixel.Events.Events>().Dispatch<OrderGivenEvent>(new OrderGivenEvent(local));
            } catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        internal static void sendOrder(string msg, string user = null)
        {
            try
            {

                if (user != null)
                {
                    msg = $"\"{msg}\" -{user}";
                }

                RequestCatalog.CaptainIssueOrderAll(OrderType.CustomMessage, msg);
            } catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        internal static void focusRandomCrew(string source)
        {
            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    Plugin.debugLogError("mpSvc is null! focusRandomCrew");
                    return;
                }

                ICollection<Crewmate> crew = mpSvc.Crew.AllCrew();

                // Get a random Crewmate from the collection
                System.Random random = new System.Random();
                int index = random.Next(crew.Count);

                IEnumerator<Crewmate> enumerator = crew.GetEnumerator();
                
                for (int i = 0; i < index; i++)
                {
                    enumerator.MoveNext();
                }

                Crewmate target = enumerator.Current;

                recieveOrder($"Let's take a look at what {target.Client.Player.DisplayName} is up to...", source);
                Mainstay<CameraOperator>.Main.Movement.CamFollowCrewmate(target);

            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        internal static void focusSelf(string source)
        {
            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    Plugin.debugLogError("mpSvc is null! focusSelf");
                    return;
                }

                List<Crewmate> crewList = mpSvc.Clients.LocalClient.Crew;

                if (crewList == null || crewList.Count == 0)
                {
                    Plugin.debugLogError("crewList is empty! focusSelf");
                    return;
                }

                Crewmate target = crewList[0];

                recieveOrder("You should take a look at yourself!", source);
                Mainstay<CameraOperator>.Main.Movement.CamFollowCrewmate(target);
            } catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        internal static void dropGems(string source)
        {
            try
            {
                GemInventoryHud hud = GameObject.Find("FIXME").GetComponent<GemInventoryHud>();

                if (hud == null)
                {
                    Plugin.debugLogError("Gem HUD is null!");
                    return;
                }

                hud.DropItems();
                recieveOrder("Drop it like it's hot!", source);
            } catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        internal static void renameShip(string name, string source)
        {
            try
            {
                LocalPlayerPrefs.SetShipName(name);
                recieveOrder($"Can the ship be named {name} instead?", source);
            } catch (Exception e)
            {
                Plugin.Log.LogError(e);
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
            try
            {
                MpSvc mpSvc = Svc.Get<MpSvc>();

                if (mpSvc == null)
                {
                    Plugin.debugLogError("mpSvc is null! goToRandomStation");
                    return;
                }


                int typeIndex = UnityEngine.Random.RandomRangeInt(0, types.Length);
                StationType type = types[typeIndex];

                List<Station> stations = Mainstay<StationManager>.Main.GetStationsOfType(type);

                int StationIndex = UnityEngine.Random.RandomRangeInt(0, stations.Count);

                Station station = stations[StationIndex];

                recieveOrder("Look over there!", source);
                Svc.Get<Subpixel.Events.Events>().Dispatch<StationClick>(new StationClick(station));
            } catch(Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
    }
}
