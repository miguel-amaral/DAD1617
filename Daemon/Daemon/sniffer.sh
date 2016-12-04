#!/bin/bash

counter = 0;
while true
do
	COUNTER=$((COUNTER+1))
	( tcpdump -i br0 -n 2>/dev/null | python sniffer.py > CSF_PACKET$Number ) & sleep 30 ; kill $!  
done