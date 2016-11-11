#!/bin/bash

tcpdump -i br0 -n 2>/dev/null | python sniffer.py
