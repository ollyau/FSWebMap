FSWebMap
========

A utility that starts a web server to display a live map with position updates from Flight Simulator.

Usage
---

Launch the program after Flight Simulator has started, connect to the simulator, then start the web server.  The map should then be viewable at http://localhost:8081/.

Known Issues
---

If you close Flight Simulator, SimConnect will automatically disconnect, but the web server will remain running.

Roadmap
---

* Implement an airplane marker on the map that rotates with the heading of the aircraft in the sim
