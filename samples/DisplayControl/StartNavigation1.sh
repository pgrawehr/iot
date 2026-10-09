#!/bin/bash
sleep 2
cd /home/pi/projects/iot/samples/DisplayControl/bin/Debug/net8.0/publish/linux-arm64/



chmod +x DisplayControl
./DisplayControl

read -n1 -r -p "Press any key to close window" key
