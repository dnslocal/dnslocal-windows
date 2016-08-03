# dnslocal-windows
A Windows implementation of dnslocal.

![](http://i.imgur.com/B9Bsvsy.png)

## Features

* HSTS system proxy supported.
* Major browsers supported.
* Secure https update access.
* Automatically DNS rule update.
* Service auto start on reboot.
* Both IPv4 and IPv6 are supported.
* New version auto discovery.

## Supported domain

If you want to know all supported domains, Please refer to https://github.com/dnslocal/dnslocal-list

## Download

You can always get the latest release here: https://github.com/dnslocal/dnslocal-windows/blob/master/RELEASE.md

## Requirement

* Windows 7 or later, Windows XP is not supported
* .NET Framework 4.5+ (not compatible with Windows XP)

## Usage

Run DNSlocal.exe as administrator, please click update button to update configuration first, and click start button to run DNSlocal service. After do this, the service auto startup on reboot and update DNS configuration every 30 minutes.

## Thanks

This software is proudly using the open source classifieds software [DNSAgent](https://github.com/stackia/DNSAgent).

# License

The project is released under [MIT License](https://github.com/dnslocal/dnslocal-list/blob/master/LICENSE).
