using BeatlesBlog.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebMap {
    // Struct for holding simulation variables
    [DataStruct()]
    public struct LatLon {
        [DataItem("PLANE LATITUDE", "degrees")]
        public double Latitude;

        [DataItem("PLANE LONGITUDE", "degrees")]
        public double Longitude;
    }

    // Enum for enumerating the different SimConnect requests
    enum Requests {
        DisplayText,
        UserPosition,
    }

    // This class handles all SimConnect operations
    class SimConnectInstance : ViewModelBase {

        // instance of the singleton class
        private static readonly Lazy<SimConnectInstance> sci = new Lazy<SimConnectInstance>(() => new SimConnectInstance());

        // static property to get the instance
        public static SimConnectInstance Instance { get { return sci.Value; } }

        // Variables
        SimConnect sc = null;
        const string appName = "Web Map";

        // To read in the http server
        public double userLat;
        public double userLon;

        // Members to be re-triggered in view model for data binding

        private string _textOutput;
        public string TextOutput {
            get { return _textOutput; }
            set { SetField(ref _textOutput, value); IsLogsChangedPropertyInViewModel = true; }
        }

        private bool _isConnected;
        public bool IsConnected {
            get { return _isConnected; }
            private set { _isConnected = value; OnPropertyChanged("SimConnectConnected"); }
        }

        private bool _isLogsChangedPropertyInViewModel;
        public bool IsLogsChangedPropertyInViewModel {
            get { return _isLogsChangedPropertyInViewModel; }
            set { SetField(ref _isLogsChangedPropertyInViewModel, value); }
        }

        private bool _loggingEnabled;
        public bool LoggingEnabled {
            get { return _loggingEnabled; }
            private set { SetField(ref _loggingEnabled, value); }
        }

        /// <summary>
        /// Class constructor.  Instantiates the class and hooks SimConnect events.
        /// </summary>
        private SimConnectInstance() {
            // Instantiate the class
            sc = new SimConnect(null);

            // Set logging enabled or disabled
#if DEBUG
            LoggingEnabled = true;
#else
            LoggingEnabled = false;
#endif

            // hook needed events
            sc.OnRecvOpen += new SimConnect.RecvOpenEventHandler(sc_OnRecvOpen);
            sc.OnRecvException += new SimConnect.RecvExceptionEventHandler(sc_OnRecvException);
            sc.OnRecvQuit += new SimConnect.RecvQuitEventHandler(sc_OnRecvQuit);

            sc.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(sc_OnRecvSimobjectData);

            // Give output
            AddOutput(appName + " by Orion Lyau\r\nVersion: " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion + "\r\n");
        }

        ~SimConnectInstance() {
            sc = null;
        }

        private void AddOutput(string text) {
            if (LoggingEnabled) {
                TextOutput += text + "\r\n";
            }
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        /// <summary>
        /// Initialization method.  Connects to the simulator and hooks necessary events.
        /// </summary>
        public void Connect() {
            try {
                sc.Open(appName);
            }
            catch (SimConnect.SimConnectException) {
                AddOutput("Local connection failed.");
            }
        }

        /// <summary>
        /// Closes the SimConnect connection.
        /// </summary>
        public void Disconnect() {
            AddOutput("Disconnecting.");

            sc.Close();
            IsConnected = false;
        }

        /// <summary>
        /// Callback for the SimConnect open event.  Writes information, maps key events, subscribes to AI add/remove events, and requests data on initially loaded AI.
        /// </summary>
        void sc_OnRecvOpen(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV_OPEN data) {
            // Write log info
            AddOutput("Connected to " + data.szApplicationName +
                "\r\n    Simulator Version:\t" + data.dwApplicationVersionMajor + "." + data.dwApplicationVersionMinor + "." + data.dwApplicationBuildMajor + "." + data.dwApplicationBuildMinor +
                "\r\n    SimConnect Version:\t" + data.dwSimConnectVersionMajor + "." + data.dwSimConnectVersionMinor + "." + data.dwSimConnectBuildMajor + "." + data.dwSimConnectBuildMinor +
                "\r\n");

            // Set variable
            IsConnected = true;

            // request lat/lon on user simobject every second
            sc.RequestDataOnUserSimObject(Requests.UserPosition, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, typeof(LatLon));

            // alert user that it connected
            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 5.0f, Requests.DisplayText, appName + " is connected to " + data.szApplicationName);
        }

        /// <summary>
        /// Callback for SimConnect exceptions.
        /// </summary>
        void sc_OnRecvException(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV_EXCEPTION data) {
            AddOutput("OnRecvException: " + data.dwException.ToString() + " (" + Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException) + ")" + "  " + data.dwSendID.ToString() + "  " + data.dwIndex.ToString());
            sc.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, 10.0f, Requests.DisplayText, appName + " SimConnect Exception: " + data.dwException.ToString() + " (" + Enum.GetName(typeof(SIMCONNECT_EXCEPTION), data.dwException) + ")");
        }

        /// <summary>
        /// Callback for quit events.  This is when the simulator exits.
        /// </summary>
        void sc_OnRecvQuit(BeatlesBlog.SimConnect.SimConnect sender, BeatlesBlog.SimConnect.SIMCONNECT_RECV data) {
            AddOutput("OnRecvQuit\tSimulator has closed.");
            Disconnect();
        }


        /**
         * SimConnect Callbacks
         */

        /// <summary>
        /// This is where we receive SimObject data.  It gets filtered by requests.
        /// </summary>
        void sc_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            switch ((Requests)data.dwRequestID) {
                case Requests.UserPosition:
                    // this is where the user position gets sent periodically
                    LatLon userPos = (LatLon)data.dwData;
                    userLat = userPos.Latitude;
                    userLon = userPos.Longitude;
                    AddOutput(string.Format("Latitude:\t\t{0}\r\nLongitude:\t{1}\r\n", userPos.Latitude, userPos.Longitude));
                    break;
                default:
                    AddOutput("OnRecvSimobjectData | default: " + data.dwObjectID);
                    break;
            }
        }

    }
}
