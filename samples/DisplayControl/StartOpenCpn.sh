#!/bin/bash
# Check whether opencpn is running. Start it if not
ps -ef | grep opencpn | grep -v grep
# if not found - equals to 1, start it
if [ $? -eq 1 ]
then
/usr/bin/flatpak run --arch=aarch64 --command=opencpn.sh org.opencpn.OpenCPN
else
echo "OpenCpn is already running"
fi


