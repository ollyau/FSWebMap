using System.Net;
using System.Windows.Input;

namespace WebMap {
    class MainViewModel : ViewModelBase {
        public bool IsConnected {
            get { return sc.IsConnected; }
        }

        public bool WebServerOff {
            get { return server == null; }
        }

        private SimConnectInstance sc = null;
        private WebServer server = null;

        private string ruleName = "FS Web Map";
        private int port = 8081;
        private string siteHtml = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <title>FS Web Map</title>\r\n    <link rel=\"stylesheet\" href=\"http://cdn.leafletjs.com/leaflet-0.7.3/leaflet.css\" />\r\n    <link href=\"http://maxcdn.bootstrapcdn.com/font-awesome/4.2.0/css/font-awesome.min.css\" rel=\"stylesheet\">\r\n    <style type=\"text/css\">\r\n        body {\r\n            margin: 0;\r\n            padding: 0;\r\n        }\r\n\r\n        html, body, #map {\r\n            height: 100%;\r\n        }\r\n    </style>\r\n    <script src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js\"></script>\r\n    <script src=\"http://cdn.leafletjs.com/leaflet-0.7.3/leaflet.js\"></script>\r\n    <script src=\"http://maps.googleapis.com/maps/api/js?v=3\"></script>\r\n    <script src=\"http://cdn.rawgit.com/shramov/leaflet-plugins/4aef4dfd3c0a54ef98edb35f81313d2726220ac8/layer/tile/Google.js\"></script>\r\n    <script src=\"http://cdn-geoweb.s3.amazonaws.com/esri-leaflet/1.0.0-rc.4/esri-leaflet.js\"></script>\r\n    <script src=\"http://cdn.rawgit.com/CliffCloud/Leaflet.EasyButton/0b147ac801b78fb768027ac47b460744f196f7e4/easy-button.js\"></script>\r\n    <script>\r\n        var map;\r\n        var userMarker;\r\n        var followAircraft = true;\r\n        $(document).ready(function () {\r\n            map = L.map('map').setView([37.79, -122.32], 11);\r\n            userMarker = L.circleMarker(map.getCenter(), { color: '#074788', fillColor: '#00a8ff', fillOpacity: 0.5 }).addTo(map);\r\n            var baseMaps = {\r\n                'Google Road': new L.Google('ROADMAP'),\r\n                'Google Aerial': new L.Google('SATELLITE'),\r\n                'Google Hybrid': new L.Google('HYBRID'),\r\n                'Google Terrain': new L.Google('TERRAIN'),\r\n                'Esri Topographic': new L.esri.BasemapLayer('Topographic'),\r\n                'Esri National Geographic': new L.esri.BasemapLayer('NationalGeographic'),\r\n                'Esri Imagery': new L.esri.BasemapLayer('Imagery'),\r\n                'FAA Sectional': L.tileLayer.wms('http://wms.chartbundle.com/wms', { layers: 'sec' }),\r\n                'FAA TAC': L.tileLayer.wms('http://wms.chartbundle.com/wms', { layers: 'tac' })\r\n            };\r\n            var overlays = {\r\n                'NEXRAD': L.tileLayer.wms('http://mesonet.agron.iastate.edu/cgi-bin/wms/nexrad/n0q.cgi', { layers: 'nexrad-n0q-900913', format: 'image/png', transparent: true })\r\n            };\r\n            overlays['NEXRAD'].setOpacity(0.9);\r\n            new L.Control.Layers(baseMaps, overlays).addTo(map);\r\n            L.easyButton('fa-plane', function () { followAircraft = !followAircraft; }, 'Center map on aircraft', map);\r\n            map.addLayer(baseMaps['FAA Sectional']);\r\n            setInterval(function () {\r\n                $.ajax({\r\n                    cache: false,\r\n                    url: '/get?aircraft',\r\n                    dataType: 'json',\r\n                    success: function (data) {\r\n                        userMarker.setLatLng([data.coordinates[1], data.coordinates[0]]);\r\n                        if (followAircraft) {\r\n                            map.panTo([data.coordinates[1], data.coordinates[0]]);\r\n                        }\r\n                    }\r\n                });\r\n            }, 1000);\r\n        });\r\n    </script>\r\n</head>\r\n<body>\r\n    <div id=\"map\" />\r\n</body>\r\n</html>";

        public MainViewModel() {
            sc = new SimConnectInstance(this);
            sc.PropertyChanged += (sender, args) => base.OnPropertyChanged(args.PropertyName);

            //using (System.IO.StreamWriter outfile = new System.IO.StreamWriter("map.html")) {
            //    outfile.Write(siteHtml);
            //}

            //using (System.IO.StreamReader sr = new System.IO.StreamReader(@"..\..\..\Site\map.html")) {
            //    siteHtml = sr.ReadToEnd();
            //}
        }

        internal void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (IsConnected) {
                sc.Disconnect();
            }
            TryDisableWebServer();
        }

        #region Commands

        private ICommand _connectCommand;
        public ICommand ConnectCommand {
            get {
                if (_connectCommand == null) {
                    _connectCommand = new RelayCommand(_ => {
                        if (!IsConnected) {
                            sc.Connect();
                        }
                        else {
                            sc.Disconnect();
                            if (server != null) {
                                DisableWebServer();
                            }
                        }
                    });
                }
                return _connectCommand;
            }
        }

        private ICommand _webServerCommand;
        public ICommand WebServerCommand {
            get {
                if (_webServerCommand == null) {
                    _webServerCommand = new RelayCommand(_ => {
                        if (server == null) {
                            EnableWebServer();
                        }
                        else {
                            DisableWebServer();
                        }
                    });
                }
                return _webServerCommand;
            }
        }

        private ICommand _donateCommand;
        public ICommand DonateCommand {
            get {
                if (_donateCommand == null) {
                    _donateCommand = new RelayCommand(param => {
                        System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_xclick&business=orion%2epublic%40live%2ecom&lc=US&item_name=FS%20Web%20Map%20Tip&button_subtype=services&no_note=0&currency_code=USD");
                    });
                }
                return _donateCommand;
            }
        }

        #endregion

        #region Website

        public void TryDisableWebServer() {
            if (server != null) {
                DisableWebServer();
            }
        }

        private string DisplayPage(HttpListenerRequest request) {
            System.Diagnostics.Debug.WriteLine("{0}: {1}", request.UserHostAddress, request.RawUrl);
            if (request.RawUrl.StartsWith("/get?aircraft")) {
                return string.Format("{{ \"type\": \"Point\", \"coordinates\": [{0}, {1}] }}", sc.userPos.Longitude, sc.userPos.Latitude);
            }
            else if (request.RawUrl.Equals("/")) {
                return siteHtml;
            }
            else {
                return string.Empty;
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

        #endregion
    }
}
