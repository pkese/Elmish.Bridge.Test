
none:

@PHONY: all test build pub rsync

all: publish rsync

test: quick-publish rsync
	ssh pi@rpi "sudo sh -c 'killall -9 Server ; bledom/Server'"

quick-publish:
	dotnet publish \
		-c Release \
		-r linux-arm64 \
	    server


publish:
	dotnet publish \
		-c Release \
		-r linux-arm64 \
		-p:PublishTrimmed=true \
	    server

rsync:
	rsync -avzh --delete server/bin/Release/net5.0/linux-arm64/ pi@rpi:~/bledom/