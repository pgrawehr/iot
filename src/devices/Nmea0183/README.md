# NMEA 0183 Protocol

## Summary

NMEA stands for `National Marine Electronics Associations`.

NMEA 0183 is an industry standard first released in 1983 and has been updated several times since then. It is used as a standard protocol 
for communication between different sensors and devices on a boat, but has been adapted for other uses as well. Most GNSS receivers
support some variant of the NMEA protocol as output. Other sensors that support the protocol on boats are wind sensors, depth transducers
or electronic compasses. The data is interpreted and combined by displays, chart plotters or autopilots.

On the physical layer, NMEA uses a serial protocol on an RS-232 interface. Historically, the baud rate is limited to 4800 baud, however
most recent devices support configuring higher baud rates. Since RS-232 only supports point-to-point connections, message routers are
required to combine multiple data sources.

NMEA 0183 has been superseeded by NMEA 2000, which uses a CAN-Bus protocol and can therefore run a large number of sensors on a single cable.
Since NMEA 0183 is much simpler to parse and does not require specific electronic components, it is still in wide use. Bi-directional convertes
from NMEA 0183 to NMEA 2000 are available from different vendors.

In NMEA 0183 a device is either a talker or a listener. There are multiple types of sentences (or messages) which can be sent:
- talker sentence (`TalkerSentence` class) - most common message
- query sentence (`QuerySentence` class) - almost never used by existing devices
- propertiary sentence (not available here)

Each message has a talker identifier (see `TalkerIdentifier`), sentence identifier (see `SentenceId`), fields and optional checksum.

An NMEA system typically consists of several devices 
The following sentence ids are currently supported:

- BOD: Bearing origin to destination
- BWC: Bearing and distance to waypoint
- DBS: Depth below surface
- GGA: GNSS fix data
- GLL: Position fast update
- GSV: Satellites in view
- HDG: Heading and deviation
- HDM: Heading, magnetic
- HDT: Heading, true
- MDA: Meterological information
- MWD: Wind direction absolute
- MWV: Wind Speed and angle (both true and apparent)
- RMB: Recommended navigation to destination (for autopilot)
- RMC: Recommended minimum navigation sentence
- RPM: Engine revolutions
- RTE: Route
- VHW: Speed trough water
- VTG: Speed and course over ground
- WPT: Waypoints
- XDR: Transducer measurement (for a variety of sensors)
- XTE: Cross track error
- ZDA: Date and time

All supported messages can both be parsed as well as sent out. Therefore it's possible to recieve GNSS data from a NMEA 2000 network and
send temperature data from an attached DHT11 sensor back to the network.

A `MessageRouter` class is available that can be used to route messages between different interfaces (the Raspberry Pi 4 supports up to 
6 RS-232 interfaces, not including USB-to-Serial adapters).
Unsupported messages can still be routed around, e.g. AIS data (AIVDM messages)

## Samples

See [NEO-M8 sample](../samples/NEO-M8-README.md) for a simple parser
See the samples directory for a simple NMEA simulator (generates sentences for a trip along a path)

Advanced use cases should use the `MessageRouter` to combine different message sources and sinks.

## Guidelines for adding new sentence identifiers

- Base a new sentence identifier on [RMC sentence](Sentences/RecommendedMinimumNavigationInformation.cs)
- Modify `GetKnownSentences` in [TalkerSentence.cs](TalkerSentence.cs) or call `TalkerSentence.RegisterSentence` in the beginning of your `Main` method

## References 

- https://www.nmea.org/
- http://www.tronico.fi/OH6NT/docs/NMEA0183.pdf
