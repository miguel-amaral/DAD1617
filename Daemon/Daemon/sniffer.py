import fileinput
PROTOCOLS_IGNORE = ('ARP', 'ARP,', 'ICMP')
def main():
	for line in fileinput.input():
		words = line.split(" ")
		skip = False
		for protocol in PROTOCOLS_IGNORE:
			if protocol in line:
				skip = True

		if skip or words[2] == "Unknown":
			continue

		source = words[2].split(".")
		source_port = source[-1]
		del(source[-1])
		source = ".".join(source)

		dest = words[2].split(".")
		dest_port = dest[-1]
		del(dest[-1])
		dest = ".".join(dest)
		
		packet_len = words[-1].replace("\n", "")
		packet_len = packet_len.replace("(","")
		packet_len = packet_len.replace(")","")
		print(source + " " + source_port + " " + dest + " " + dest_port + " " + packet_len + " " + words[1])

	# my code here

if __name__ == "__main__":
	main()
