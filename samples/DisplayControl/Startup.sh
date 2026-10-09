#!/bin/bash

pkill -e evdev-rce
/home/pi/projects/evdev-right-click-emulation/out/evdev-rce &


cd /home/pi/projects/iot/samples/DisplayControl/bin/Debug/net6.0/linux-arm64/publish
chmod +x DisplayControl
./DisplayControl
read -n1 -r -p "Press any key to continue..." key
