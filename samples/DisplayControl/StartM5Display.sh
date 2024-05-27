#!/bin/bash

cd /home/pi/projects/iot/src/devices/Ili934x/samples/bin/Debug/net6.0/linux-arm64/publish/
chmod +x Ili934x.Samples
./Ili934x.Samples INET M5Tough.local
