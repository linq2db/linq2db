#!/bin/bash

# osx agent doesn't have docker pre-installed, so we need to do it manually
# unfortunatelly, installer is not very good and sometimes just doesn't work
#https://github.com/microsoft/azure-pipelines-image-generation/issues/738
retries=0
# recent 37199 version of docker fails to start, so we need to specify most recent working one
#brew cask install docker
brew cask install https://raw.githubusercontent.com/Homebrew/homebrew-cask/b8c67034bd78f9585b1316564f223b97055bc0dc/Casks/docker.rb
sudo /Applications/Docker.app/Contents/MacOS/Docker --quit-after-install --unattended
/Applications/Docker.app/Contents/MacOS/Docker --unattended &
while ! docker info 2>/dev/null ; do
    sleep 5
    retries=`expr $retries + 1`
    if pgrep -xq -- "Docker"; then
        echo 'docker still running'
    else
        echo 'docker not running, restart'
        /Applications/Docker.app/Contents/MacOS/Docker --unattended &
    fi
    if [ $retries -gt 20 ]; then
        >&2 echo 'Failed to run docker.'
        cat '/var/root/Library/Group Containers/group.com.docker/DockerAppStderr.txt'
        exit 1
    fi;

    echo 'Waiting for docker service to be in the running state'
done
