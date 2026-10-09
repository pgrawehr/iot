

To get LIRC running on the Raspberry Pi with the latest Raspbian release (Buster), a bit of tweaking is necessary, due to recent incompatible changes in the kernel. It's best to follow these https://www.raspberrypi.org/forums/viewtopic.php?t=235256 step-by-step instructions. After following those instructions, you will get two device nodes: /dev/lirc0 is the sender, /dev/lirc1 the receiver. Depending on which is active, you'll be able to either test the receiver using `irw` or the sender using `irsend`. If you need both services, test them separtelly now by changing the line `device=` in /etc/lirc/lirc_options.conf` from `/dev/lirc0` to /dev/lirc1` or vice-versa and restart the service (using `sudo systemctl restart lircd`). Note: To see that the sender is working, it may be helpful to temporarily replace the IR-LED with an ordinary LED. 

After all this trouble, I've got this lirc_options.conf file: 

```
# These are the default options to lircd, if installed as
# /etc/lirc/lirc_options.conf. See the lircd(8) and lircmd(8)
# manpages for info on the different options.
#
# Some tools including mode2 and irw uses values such as
# driver, device, plugindir and loglevel as fallback values
# in not defined elsewhere.

[lircd]
nodaemon        = False
driver          = default
device          = /dev/lirc0
output          = /var/run/lirc/lircd0
pidfile         = /var/run/lirc/lircd0.pid
plugindir       = /usr/lib/arm-linux-gnueabihf/lirc/plugins
permission      = 666
allow-simulate  = No
repeat-max      = 600
#effective-user =
listen         = 2221 # Important: We will need this later
#connect        = host[:port]
#loglevel       = 6
#release        = true
#release_suffix = _EVUP
#logfile        = ...
#driver-options = ...

[lircmd]
uinput          = False
nodaemon        = False

# [modinit]
# code = /usr/sbin/modprobe lirc_serial
# code1 = /usr/bin/setfacl -m g:lirc:rw /dev/uinput
# code2 = ...


# [lircd-uinput]
# add-release-events = False
# release-timeout    = 200
# release-suffix     = _EVUP

```

I've also got an /etc/lirc/lirc.conf file for my remote, which I created using irrecord, but you might be more fortunate
and find one that matches your particular remote. 

Now I copied the above file to /etc/lirc/lirc_options1.conf and replace these lines:
```

device          = /dev/lirc1
#Note that we leave this on default, will simplify usage of irw.
output          = /var/run/lirc/lircd
pidfile         = /var/run/lirc/lircd1.pid
listen         = 2222 # Important: We will need this later
```

Now, once the service (for /dev/lirc0) runs, we can test it again: `irsend -a localhost:2221 SEND_ONCE <Remote> KEY_GREEN` should blink the LED (gives a bunch of errors, but works). Then we now start the receiver as well: `sudo lircd --nodaemon -O /etc/lirc/lirc_options1.conf`. This should start the service in foreground mode and show any problems (i.e. if you get an error saying there is already another lirc process running, check that the two have different pidfile entries). Now start `irw`, and check that pressing keys on the remote gives the expected results. 

After that, both input and output work at the same time. The only thing that we now still want to do is set up the second service to start automatically at bootup as well. Let's do a copy of the service description: `sudo cp /lib/systemd/system/lircd.service /lib/systemd/system/lircd1.service' and change it to: 


```
[Unit]
Documentation=man:lircd(8)
Documentation=http://lirc.org/html/configure.html
Description=Flexible IR remote input application support on /dev/lirc1
Wants=lircd-setup.service
After=network.target lircd-setup.service

[Service]
Type=notify
ExecStart=/usr/sbin/lircd --nodaemon -O /etc/lirc/lirc_options1.conf
; User=lirc
; Group=lirc

; Hardening opts, see systemd.exec(5). Doesn't add much unless
; not running as root.
;
; # Required for dropping privileges in --effective-user.
; MemoryDenyWriteExecute=true
; NoNewPrivileges=true
; PrivateTmp=true
; ProtectHome=true
; ProtectSystem=full

[Install]
WantedBy=multi-user.target

```

Now prepare it for autostart and start it with `sudo systemctl enable lirc1` `sudo systemctl start lirc1`. Both sending and receiving
data over infrared should now be working. 

To make something useful from the input, there's yet another service we can use. Just start it interactively for a quick test:

sudo lircmd -s /var/run/lirc/lircd --options-file=/etc/lirc/lirc_options1.conf --uinput --nodaemon --loglevel=10 

