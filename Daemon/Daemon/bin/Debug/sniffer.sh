#!/bin/bash

tcpdump -i wlan0 -n 2>/dev/null | python sniffer.py
