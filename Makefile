
none:

@PHONY: all test build pub rsync

all: publish rsync

test: quick-publish rsync
	ssh pi@rpi "sudo bledom/Hardware"

quick-publish:
	dotnet publish \
		-c Release \
		-r linux-arm64 \
	    hardware


publish:
	dotnet publish \
		-c Release \
		-r linux-arm64 \
		-p:PublishTrimmed=true \
	    hardware

rsync:
	rsync -avzh --delete hardware/bin/Release/net5.0/linux-arm64/ pi@rpi:~/bledom/