using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Xml;

namespace LEAForKSP
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class LEAForKSP : MonoBehaviour
    {
        bool connected = false;
        string pass = "123456";
        int port = 45612;
        AutoResetEvent ARE = new AutoResetEvent(false);
        CultureInfo usCulture = new CultureInfo("en-US");
        FlightCtrlState lastState;
        Vessel currentVessel;
        bool initialized = false;
        char[] semicolon = new char[] { ';' };
        ApplicationLauncherButton appLauncherBtn;
        Texture2D buttonTexture;
        Texture2D buttonTextureNotConnected;
        bool GUIEnabled = false;
        bool hovering = false;

        string GUILayerPort = "45612";
        string GUILayerPass = "123456";

        string path;

        bool invPitch = false;
        bool invRoll = false;
        bool invYaw = false;

        bool invX = false;
        bool invY = false;
        bool invZ = false;

        bool invWheelSteer = false;
        bool invWheelThrottle = false;

        void init()
        {
            DontDestroyOnLoad(this);

            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "config.xml";

            try
            {
                XmlDocument XD = new XmlDocument();
                if (File.Exists(path))
                {
                    using (StreamReader SR = new StreamReader(path, Encoding.UTF8))
                    {
                        XD.Load(SR);
                    }
                    XmlNode xn = XD.SelectSingleNode("config");
                    if (xn != null)
                    {
                        XmlAttribute xa = xn.Attributes["password"];
                        if (xa != null)
                        {
                            GUILayerPass = pass = xa.Value;
                        }
                        xa = xn.Attributes["port"];
                        if (xa != null)
                        {
                            GUILayerPort = xa.Value;
                            int.TryParse(GUILayerPort, NumberStyles.Integer, usCulture, out port);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                print("[LEAForKSP] Error while reading configuration. Error: " + ex.Message);
                GUILayerPass = pass = "123456";
                GUILayerPort = "45612";
                port = 45612;
            }

            buttonTexture = GameDatabase.Instance.GetTexture("LEAForKSP/Textures/LEA", false);
            buttonTextureNotConnected = GameDatabase.Instance.GetTexture("LEAForKSP/Textures/LEANotConnected", false);

            lastState = new FlightCtrlState();
            lastState.mainThrottle = 0f;

            lastState.pitch = 0f;
            lastState.roll = 0f;
            lastState.yaw = 0f;

            lastState.X = 0f;
            lastState.Y = 0f;
            lastState.Z = 0f;

            lastState.wheelSteer = 0f;
            lastState.wheelThrottle = 0f;

            TCPLayerLite.localDeviceType = TCPLayerLite.deviceType.GAME;
            TCPLayerLite.DataReceived += commands.newDataToDecode;
            TCPLayerLite.FailToConnect += TCPLayerLite_FailToConnect;
            TCPLayerLite.DirectConnectionEstablished += TCPLayerLite_ConnectionEstablished;
            TCPLayerLite.LastConnectionLost += TCPLayerLite_LastConnectionLost;
            TCPLayerLite.NoConnectedDevice += TCPLayerLite_NoConnectedDevice;
//             TCPLayerLite.setSecurityOptions(TCPLayerLite.securityMode.PASS_SHA1, Encoding.UTF8.GetBytes(pass), false);
//             TCPLayerLite.launchConnection(new IPEndPoint(IPAddress.Loopback, port));
            commands.NewCommands += commands_NewCommands;

            List<commands.EMData> EMDList = new List<commands.EMData>();
            EMDList.Add(new commands.EMData("LEAForKSP_Gear", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Light", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_SAS", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Brakes", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Abort", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Stage", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_RCS", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom01", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom02", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom03", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom04", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom05", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom06", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom07", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom08", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom09", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_Custom10", "False", commands.EMType.BUTTON));

            EMDList.Add(new commands.EMData("LEAForKSP_mainThrottle", "0;0", commands.EMType.AXIS));

            EMDList.Add(new commands.EMData("LEAForKSP_pitch", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_roll", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_yaw", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_pitch", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_roll", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_yaw", "False", commands.EMType.BUTTON));

            EMDList.Add(new commands.EMData("LEAForKSP_X", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_Y", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_Z", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_X", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_Y", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_Z", "False", commands.EMType.BUTTON));

            EMDList.Add(new commands.EMData("LEAForKSP_wheelSteer", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_wheelThrottle", "0.5;0.5", commands.EMType.AXIS));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_wheelSteer", "False", commands.EMType.BUTTON));
            EMDList.Add(new commands.EMData("LEAForKSP_inv_wheelThrottle", "False", commands.EMType.BUTTON));

            commands.registerCommands(EMDList);

            GameEvents.onVesselDestroy.Add(vesselDestroyed);

            appLauncherBtn = ApplicationLauncher.Instance.AddApplication(onAppButtonIsOn, onAppButtonIsOff, onAppButtonHover, onAppButtonHoverOut, onAppButtonActive, onAppButtonInactive, buttonTexture);
            ApplicationLauncher.Instance.AddOnHideCallback(removeGUICallback);
        }

        public void Awake()
        {
            if (!initialized)
            {
                init();
            }

            reconnect();
        }

        void reconnect()
        {
            TCPLayerLite.shutdownAll();
            connected = false;

            TCPLayerLite.setDefaultSecurityOptions(TCPLayerLite.securityMode.PASS_SHA1, Encoding.UTF8.GetBytes("Anonymous"), Encoding.UTF8.GetBytes(pass), false);
            TCPLayerLite.launchConnection(new IPEndPoint(IPAddress.Loopback, port));

            if (!ARE.WaitOne(10000))
            {
                print("[LEAForKSP] Fail to get answer from server.");
                TCPLayerLite.shutdownAll();
                appLauncherBtn.SetTexture(buttonTextureNotConnected);
                return;
            }

            if (!TCPLayerLite.waitForDevicesToBeReady(TimeSpan.FromMilliseconds(1000)))
            {
                print("[LEAForKSP] Timeout.");
                TCPLayerLite.shutdownAll();
                appLauncherBtn.SetTexture(buttonTextureNotConnected);
                return;
            }

            if (!connected)
            {
                print("[LEAForKSP] Connection closed. Bad port or bad password.");
                TCPLayerLite.shutdownAll();
                appLauncherBtn.SetTexture(buttonTextureNotConnected);
                return;
            }

            appLauncherBtn.SetTexture(buttonTexture);

            commands.sendConfigureServer(commands.serverMode.PUSH);

            commands.sendResynchData();
        }

        public void OnDestroy()
        {
            if (currentVessel != null)
            {
                print("[LEAForKSP] Unregister active vessel.");
                currentVessel.OnFlyByWire -= FlightInputCallback;
                currentVessel = null;
            }

            TCPLayerLite.shutdownAll();

            ApplicationLauncher.Instance.RemoveApplication(appLauncherBtn);
        }

        public void FixedUpdate()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                if (currentVessel != null && currentVessel != FlightGlobals.ActiveVessel)
                {
                    //print("[LEAForKSP] Unregister active vessel.");
                    currentVessel.OnFlyByWire -= FlightInputCallback;
                    currentVessel = null;
                }
                if (currentVessel == null)
                {
                    currentVessel = FlightGlobals.ActiveVessel;
                    if (currentVessel != null)
                    {
                        //print("[LEAForKSP] Register active vessel.");
// 
//                         List<FlightInputCallback> allFIC = FlightGlobals.ActiveVessel.OnFlyByWire.GetInvocationList().OfType<FlightInputCallback>().ToList<FlightInputCallback>();
//                         foreach (FlightInputCallback FIC in allFIC)
//                         {
//                             FlightGlobals.ActiveVessel.OnFlyByWire -= FIC;
//                         }

                        FlightGlobals.ActiveVessel.OnFlyByWire += FlightInputCallback;
// 
//                         foreach (FlightInputCallback FIC in allFIC)
//                         {
//                             FlightGlobals.ActiveVessel.OnFlyByWire += FIC;
//                         }

                        List<commands.EMData> commandList = new List<commands.EMData>();

                        commands.EMData EMD = new commands.EMData("LEAForKSP_Gear", currentVessel.ActionGroups[KSPActionGroup.Gear].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Gear", currentVessel.ActionGroups[KSPActionGroup.Gear].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Light", currentVessel.ActionGroups[KSPActionGroup.Light].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_SAS", currentVessel.ActionGroups[KSPActionGroup.SAS].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Brakes", currentVessel.ActionGroups[KSPActionGroup.Brakes].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_RCS", currentVessel.ActionGroups[KSPActionGroup.RCS].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom01", currentVessel.ActionGroups[KSPActionGroup.Custom01].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom02", currentVessel.ActionGroups[KSPActionGroup.Custom02].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom03", currentVessel.ActionGroups[KSPActionGroup.Custom03].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom04", currentVessel.ActionGroups[KSPActionGroup.Custom04].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom05", currentVessel.ActionGroups[KSPActionGroup.Custom05].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom06", currentVessel.ActionGroups[KSPActionGroup.Custom06].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom07", currentVessel.ActionGroups[KSPActionGroup.Custom07].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom08", currentVessel.ActionGroups[KSPActionGroup.Custom08].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom09", currentVessel.ActionGroups[KSPActionGroup.Custom09].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        EMD = new commands.EMData("LEAForKSP_Custom10", currentVessel.ActionGroups[KSPActionGroup.Custom10].ToString(usCulture), commands.EMType.BUTTON);
                        commandList.Add(EMD);

                        lock (lastState)
                        {
                            lastState.NeutralizeAll();
                        }

                        string value = lastState.mainThrottle.ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_mainThrottle", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.pitch + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_pitch", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.roll + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_roll", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.yaw + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_yaw", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.X + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_X", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.Y + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_Y", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.Z + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_Z", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.wheelSteer + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_wheelSteer", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        value = ((lastState.wheelThrottle + 1f) / 2f).ToString(usCulture);
                        EMD = new commands.EMData("LEAForKSP_wheelThrottle", value + ';' + value, commands.EMType.AXIS);
                        commandList.Add(EMD);

                        commands.sendPushData(commandList);
                    }
                }
            }
            else if (currentVessel != null)
            {
                //print("[LEAForKSP] Unregister active vessel.");
                currentVessel.OnFlyByWire -= FlightInputCallback;
                currentVessel = null;
            }
        }

        void removeGUICallback()
        {
            GUIEnabled = false;
            hovering = false;
        }

        void FlightInputCallback(FlightCtrlState st)
        {
            lock (lastState)
            {
                if (!Mathf.Approximately(lastState.pitch, 0f))
                {
                    st.pitch = Mathf.Clamp(lastState.pitch, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.roll, 0f))
                {
                    st.roll = Mathf.Clamp(lastState.roll, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.yaw, 0f))
                {
                    st.yaw = Mathf.Clamp(lastState.yaw, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.X, 0f))
                {
                    st.X = Mathf.Clamp(lastState.X, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.Y, 0f))
                {
                    st.Y = Mathf.Clamp(lastState.Y, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.Z, 0f))
                {
                    st.Z = Mathf.Clamp(lastState.Z, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.mainThrottle, 0f))
                {
                    st.mainThrottle = Mathf.Clamp(lastState.mainThrottle, 0f, 1f);
                }
                if (!Mathf.Approximately(lastState.wheelSteer, 0f))
                {
                    st.wheelSteer = Mathf.Clamp(lastState.wheelSteer, -1f, 1f);
                }
                if (!Mathf.Approximately(lastState.wheelThrottle, 0f))
                {
                    st.wheelThrottle = Mathf.Clamp(lastState.wheelThrottle, -1f, 1f);
                }
            }
        }

        void vesselDestroyed(Vessel vessel)
        {
            if (currentVessel != null)
            {
                currentVessel.OnFlyByWire -= FlightInputCallback;
                currentVessel = null;
            }
        }

        void onAppButtonIsOn()
        {
            GUIEnabled = true;
        }

        void onAppButtonIsOff()
        {
            GUIEnabled = false;
        }

        void OnGUI()
        {
            if (GUIEnabled)
            {
                float width = 250f;
                float height = 150f;
                float margin = 10f;
                float rowWidth1 = 100f;
                float rowWidth2 = 150;
                float blockHeight = 20f;
                float left = (float)Screen.width / 2f - width / 2f;
                float top = (float)Screen.height / 2f - height / 2f;
                GUI.Box(new Rect(left, top, width, height), "LEA Config Menu");
                float row = 1;
                GUI.Label(new Rect(left + margin, top + row * blockHeight + row * margin, rowWidth1 - margin * 2f, blockHeight), "Password: ");
                GUILayerPass = GUI.TextField(new Rect(left + margin + rowWidth1, top + row * blockHeight + row * margin, rowWidth2 - margin * 2f, blockHeight), GUILayerPass);
                row = row + 1f;
                GUI.Label(new Rect(left + margin, top + row * blockHeight + row * margin, rowWidth1 - margin * 2f, blockHeight), "Port: ");
                GUILayerPort = GUI.TextField(new Rect(left + margin + rowWidth1, top + row * blockHeight + row * margin, rowWidth2 - margin * 2f, blockHeight), GUILayerPort);
                row = row + 1f;
                if (GUI.Button(new Rect(left + margin, top + row * blockHeight + row * margin, width - margin * 2f, blockHeight), "Set Parameters"))
                {
                    pass = GUILayerPass;
                    if (!int.TryParse(GUILayerPort, NumberStyles.Integer, usCulture, out port))
                    {
                        port = 45612;
                        GUILayerPort = "45612";
                    }
                    if (port < 1024 || port > 65535)
                    {
                        port = 45612;
                        GUILayerPort = "45612";
                    }

                    XmlDocument XD = new XmlDocument();
                    XD.AppendChild(XD.CreateXmlDeclaration("1.0", "utf-8", null));

                    XmlNode xn = XD.CreateElement("config");
                    XD.AppendChild(xn);

                    XmlAttribute xa = XD.CreateAttribute("password");
                    xa.Value = pass;
                    xn.Attributes.Append(xa);

                    xa = XD.CreateAttribute("port");
                    xa.Value = port.ToString("D", usCulture);
                    xn.Attributes.Append(xa);

                    try
                    {
                        using (StreamWriter SW = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            XD.Save(SW);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        print("[LEAForKSP] Error while writing configuration. Error: " + ex.Message);
                    }

                    reconnect();
                }
                row = row + 1f;
                if (GUI.Button(new Rect(left + margin, top + row * blockHeight + row * margin, width - margin * 2f, blockHeight), "Close"))
                {
                    GUIEnabled = false;
                    ApplicationLauncher.Instance.RemoveApplication(appLauncherBtn);
                    appLauncherBtn = ApplicationLauncher.Instance.AddApplication(onAppButtonIsOn, onAppButtonIsOff, onAppButtonHover, onAppButtonHoverOut, onAppButtonActive, onAppButtonInactive, buttonTexture);
                }
            }
            if (hovering)
            {
                if (!connected)
                {
                    float height = 20f;
                    float width = 100f;
                    Vector2 mousePos = Mouse.screenPos;
                    Rect labelPos;
                    if (mousePos.x + width > Screen.width)
                    {
                        if (mousePos.y < 25f)
                        {
                            labelPos = new Rect(Screen.width - width, 0f, width, height);
                        }
                        else
                        {
                            labelPos = new Rect(Screen.width - width, mousePos.y - 25f, width, height);
                        }
                    }
                    else
                    {
                        if (mousePos.y < 25f)
                        {
                            labelPos = new Rect(mousePos.x - width, 0f, width, height);
                        }
                        else
                        {
                            labelPos = new Rect(mousePos.x - width, mousePos.y - 25f, width, height);
                        }
                    }

                    GUI.Box(labelPos, "NOT connected!");
                }
            }
        }

        void onAppButtonHover()
        {
            hovering = true;
        }

        void onAppButtonHoverOut()
        {
            hovering = false;
        }

        void onAppButtonActive()
        {

        }

        void onAppButtonInactive()
        {

        }

        //////////////////////////////////////////////////////////////////////////
        //TCP layer events
        //////////////////////////////////////////////////////////////////////////

        void TCPLayerLite_FailToConnect(List<TCPLayerLite.device> devList)
        {
            print("[LEAForKSP] Fail to connect to server.");
            connected = false;
            ARE.Set();
        }

        void TCPLayerLite_ConnectionEstablished(List<TCPLayerLite.device> devList)
        {
            print("[LEAForKSP] Connection established.");
            connected = true;
            ARE.Set();
        }

        void TCPLayerLite_LastConnectionLost(List<TCPLayerLite.device> devList)
        {
            print("[LEAForKSP] Connection lost.");
            connected = false;
            ARE.Set();
        }

        void TCPLayerLite_NoConnectedDevice()
        {
            print("[LEAForKSP] Cannot send data: no connection.");
        }

        //////////////////////////////////////////////////////////////////////////
        //Commands
        //////////////////////////////////////////////////////////////////////////

        void commands_NewCommands(List<commands.EMData> EMDList)
        {
            if (currentVessel != null && !currentVessel.IsControllable)
            {
                return;
            }
            lock (lastState)
            {
                foreach (commands.EMData EMD in EMDList)
                {
                    if (EMD.EMTag == "LEAForKSP_Gear")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Light")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Light, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Light, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_SAS")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Brakes")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Abort")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Stage")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                Staging.ActivateNextStage();
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_RCS")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom01")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom02")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom03")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom04")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom05")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom06")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom07")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom08")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom09")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Custom10")
                    {
                        if (currentVessel != null)
                        {
                            if (EMD.EMValue == "True")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, true);
                            }
                            else if (EMD.EMValue == "False")
                            {
                                currentVessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, false);
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_pitch")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invPitch = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invPitch = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_roll")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invRoll = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invRoll = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_yaw")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invYaw = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invYaw = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_X")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invX = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invX = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_Y")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invY = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invY = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_Z")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invZ = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invZ = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_wheelSteer")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invWheelSteer = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invWheelSteer = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_inv_wheelThrottle")
                    {
                        if (EMD.EMValue == "True")
                        {
                            invWheelThrottle = true;
                        }
                        else if (EMD.EMValue == "False")
                        {
                            invWheelThrottle = false;
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_mainThrottle")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                lastState.mainThrottle = value;
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_pitch")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invPitch)
                                {
                                    lastState.pitch = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.pitch = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_roll")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invRoll)
                                {
                                    lastState.roll = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.roll = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_yaw")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invYaw)
                                {
                                    lastState.yaw = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.yaw = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_X")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invX)
                                {
                                    lastState.X = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.X = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Y")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invY)
                                {
                                    lastState.Y = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.Y = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_Z")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invZ)
                                {
                                    lastState.Z = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.Z = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_wheelSteer")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invWheelSteer)
                                {
                                    lastState.wheelSteer = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.wheelSteer = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                    else if (EMD.EMTag == "LEAForKSP_wheelThrottle")
                    {
                        string[] splitVal = EMD.EMValue.Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                        if (splitVal.Length > 1)
                        {
                            float value;
                            if (float.TryParse(splitVal[0], NumberStyles.Float, usCulture, out value))
                            {
                                if (invWheelThrottle)
                                {
                                    lastState.wheelThrottle = ((value * 2f) - 1f) * -1f;
                                }
                                else
                                {
                                    lastState.wheelThrottle = (value * 2f) - 1f;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
