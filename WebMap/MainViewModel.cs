using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebMap {
    class MainViewModel : ViewModelBase {

        // Public properties for data binding
        public bool SimConnectConnected { get { return sc.IsConnected; } }
        public bool WebServerOff { get { return server == null; } }
        public bool LoggingEnabled { get { return sc.LoggingEnabled; } }
        public string TextOutput { get { return sc.TextOutput; } }
        public bool IsLogsChangedPropertyInViewModel { get { return sc.IsLogsChangedPropertyInViewModel; } }

        // Instances of the model (SimConnect class)
        private SimConnectInstance sc = null;
        private WebServer server = null;
        //private TestMain httpServer = null;

        private string ruleName = "FS Web Map";
        private int port = 8081;

        // Class constructor
        public MainViewModel() {
            // Instantiate the SimConnect class
            sc = SimConnectInstance.Instance;

            // Trigger property changed events coming from the SimConnect class
            sc.PropertyChanged += (sender, args) => base.OnPropertyChanged(args.PropertyName);

            //using (System.IO.StreamReader sr = new System.IO.StreamReader(@"C:\Users\Orion\Desktop\sim_map.html")) {
            //    String line = sr.ReadToEnd();
            //    Console.WriteLine(line);
            //}
        }

        // Event handler for clicking the SimConnect button
        internal void Button_Connect_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (!SimConnectConnected) {
                sc.Connect();
            }
            else {
                sc.Disconnect();
                if (server != null) {
                    DisableWebServer();
                }
            }
        }

        // Event handler for closing the SimConnect session if the window closes
        internal void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (SimConnectConnected) {
                sc.Disconnect();
            }
            if (server != null) {
                DisableWebServer();
            }
        }

        // Event handler to start the web server
        internal void Button_EnableWebServer_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (server == null) {
                EnableWebServer();
            }
            else {
                DisableWebServer();
            }
        }

        public static string DisplayPage(System.Net.HttpListenerRequest request) {
            Console.WriteLine("{0}: {1}", request.UserHostAddress, request.RawUrl);
            switch (request.RawUrl) {
                case "/get?userPos":
                    SimConnectInstance sc = SimConnectInstance.Instance;
                    return "{\n\"lat\": \"" + sc.userLat + "\",\n\"lon\": \"" + sc.userLon + "\"\n}";
                default:
                    return "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <title>Web Map</title>\r\n    <link rel=\"stylesheet\" href=\"http://cdn.leafletjs.com/leaflet-0.7.2/leaflet.css\" />\r\n    <style type=\"text/css\">\r\n        body {\r\n            margin: 0;\r\n            padding: 0;\r\n        }\r\n\r\n        html, body, #map {\r\n            height: 100%;\r\n        }\r\n    </style>\r\n    <script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js\"></script>\r\n    <script src=\"http://cdn.leafletjs.com/leaflet-0.7.2/leaflet.js\"></script>\r\n    <script src=\"https://maps.googleapis.com/maps/api/js?v=3.exp&sensor=false\"></script>\r\n    <script src=\"https://rawgit.com/shramov/leaflet-plugins/master/layer/tile/Google.js\"></script>\r\n    <script src=\"http://maps.stamen.com/js/tile.stamen.js?v1.2.4\"></script>\r\n    <!--<script src=\"https://rawgit.com/Esri/esri-leaflet/master/src/esri-leaflet.js\"></script>-->\r\n    <script src=\"http://esri.github.io/esri-leaflet/lib/esri-leaflet/esri-leaflet.js\"></script>\r\n\r\n    <script>\r\n        // on document load, set up the map\r\n        var map;\r\n        $(document).ready(function () {\r\n            // set up map\r\n            map = L.map('map').setView([37.79, -122.32], 11);\r\n\r\n            // Create layers\r\n            var googAerial = new L.Google('SATELLITE');\r\n            var googRoad = new L.Google('ROADMAP');\r\n            var googHybrid = new L.Google('HYBRID');\r\n            var googTerrain = new L.Google('TERRAIN');\r\n            var stamenTerrain = new L.StamenTileLayer('terrain');\r\n            var esri_Topo = new L.esri.BasemapLayer('Topographic');\r\n            var esri_NatGeo = new L.esri.BasemapLayer(\"NationalGeographic\");\r\n            var esri_Imagery = new L.esri.BasemapLayer(\"Imagery\");\r\n\r\n            var sectional = L.tileLayer.wms(\"http://wms.chartbundle.com/wms\", { layers: 'sec', format: 'image/png', transparent: true } ); var terminal = L.tileLayer.wms(\"http://wms.chartbundle.com/wms\", { layers: 'tac', format: 'image/png', transparent: true } ); // Add layers to map\r\n            new L.Control.Layers({\r\n                'Google Road': googRoad,\r\n                'Google Aerial': googAerial,\r\n                'Google Hybrid': googHybrid,\r\n                'Google Terrain': googTerrain,\r\n                'Stamen Terrain': stamenTerrain,\r\n                \"Esri Topographic\": esri_Topo,\r\n                'Esri National Geographic': esri_NatGeo,\r\n                \"Esri Imagery\": esri_Imagery\r\n, \"FAA Sectional\": sectional\r\n, \"FAA TAC\": terminal\r\n            }).addTo(map);\r\n\r\n            // Set default layer\r\n            map.addLayer(sectional);\r\n\r\n            // check for lat/lon periodically\r\n            setInterval(function () {\r\n                $.getJSON(\"http://\" + location.host + \"/get?userPos\", function (data, status) {\r\n                    if ((data != undefined) && (status == \"success\")) {\r\n                        map.panTo([data.lat, data.lon]); console.log(data.lat + ', ' + data.lon + status); \r\n                    }\r\n                });\r\n            }, 1000); //1 second\r\n        });\r\n    </script>\r\n</head>\r\n<body>\r\n    <div id=\"map\" />\r\n</body>\r\n</html>";
            }
        }

        private void EnableWebServer() {
            AddFirewallException(ruleName, port);
            server = new WebServer(DisplayPage, "http://+:" + port + "/");
            server.Run();
            OnPropertyChanged("WebServerOff");
        }

        private void DisableWebServer() {
            server.Dispose();
            server = null;
            OnPropertyChanged("WebServerOff");
            RemoveFirewallException(ruleName, port);
        }

        private void AddFirewallException(string ruleName, int port) {
            using (System.Diagnostics.Process p = new System.Diagnostics.Process()) {
                p.StartInfo.FileName = "netsh";
                p.StartInfo.Arguments = "advfirewall firewall add rule name=\"" + ruleName + "\" dir=in action=allow protocol=TCP localport=" + port;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
            }
        }

        private void RemoveFirewallException(string ruleName, int port) {
            using (System.Diagnostics.Process p = new System.Diagnostics.Process()) {
                p.StartInfo.FileName = "netsh";
                p.StartInfo.Arguments = "advfirewall firewall delete rule name=\"" + ruleName + "\" protocol=TCP localport=" + port;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
            }
        }
    }
}
