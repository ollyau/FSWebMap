using BeatlesBlog.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebMap {
    [DataStruct()]
    public struct LatLon {
        [DataItem("PLANE LATITUDE", "degrees")]
        public double Latitude;

        [DataItem("PLANE LONGITUDE", "degrees")]
        public double Longitude;
    }

    enum Requests {
        DisplayText,
        UserPosition,
    }

    class SimConnectHelpers {
        public static bool IsLocalRunning {
            get { return LookupDefaultPortNumber("SimConnect_Port_IPv4") != 0 || LookupDefaultPortNumber("SimConnect_Port_IPv6") != 0; }
        }

        public static int LookupDefaultPortNumber(string strValueName) {
            string[] simulators = {
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator",
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft ESP",
                                      @"HKEY_CURRENT_USER\Software\LockheedMartin\Prepar3D",
                                      @"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v2",
                                      @"HKEY_CURRENT_USER\Software\Microsoft\Microsoft Games\Flight Simulator - Steam Edition"
                                  };
            foreach (string sim in simulators) {
                string value = (string)Microsoft.Win32.Registry.GetValue(sim, strValueName, null);
                if (!string.IsNullOrEmpty(value)) {
                    int port = int.Parse(value);
                    if (port != 0) { return port; }
                }
            }
            return 0;
        }
    }

    public class OpenEventArgs : EventArgs {
        public string SimulatorName { get; private set; }
        public OpenEventArgs(string SimulatorName) {
            this.SimulatorName = SimulatorName;
        }
    }

    class SimConnectInstance {
        private MainViewModel sender;
        private SimConnect sc = null;
        private const string appName = "Web Map";

        public EventHandler<OpenEventArgs> OpenEvent;
        public EventHandler DisconnectEvent;
        public LatLon userPos { get; private set; }

        protected virtual void OnRaiseOpenEvent(OpenEventArgs e) {
            EventHandler<OpenEventArgs> handler = OpenEvent;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnRaiseDisconnectEvent(EventArgs e) {
            EventHandler handler = DisconnectEvent;
            if (handler != null) {
                handler(this, e);
            }
        }

        public SimConnectInstance(MainViewModel sender) {
            this.sender = sender;
            sc = new SimConnect(null);
            sc.OnRecvOpen += sc_OnRecvOpen;
            sc.OnRecvException += sc_OnRecvException;
            sc.OnRecvQuit += sc_OnRecvQuit;
            sc.OnRecvSimobjectData += sc_OnRecvSimobjectData;
        }

        public void Connect() {
            if (SimConnectHelpers.IsLocalRunning) {
                try {
                    sc.Open(appName);
                }
                catch (SimConnect.SimConnectException) {
                    bool ipv6support = System.Net.Sockets.Socket.OSSupportsIPv6;
                    int scPort = ipv6support ? SimConnectHelpers.LookupDefaultPortNumber("SimConnect_Port_IPv6") : SimConnectHelpers.LookupDefaultPortNumber("SimConnect_Port_IPv4");
                    if (scPort == 0) { throw new SimConnect.SimConnectException("Invalid port."); }
                    sc.Open(appName, null, scPort, ipv6support);
                }
            }
        }

        public void Disconnect() {
            sc.Close();
            sender.TryDisableWebServer();
            OnRaiseDisconnectEvent(EventArgs.Empty);
        }

        private void sc_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            OnRaiseOpenEvent(new OpenEventArgs(data.szApplicationName));
            sc.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, typeof(LatLon));
        }

        private void sc_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 10.0f, Requests.DisplayText, string.Format("{0} SimConnect Exception: {1} ({2})", appName, data.dwException.ToString(), Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException)));
        }

        private void sc_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) {
            Disconnect();
        }

        private void sc_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            if ((Requests)data.dwRequestID == Requests.UserPosition) {
                userPos = (LatLon)data.dwData;
            }
        }
    }
}
