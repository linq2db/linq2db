#!/bin/bash
#https://github.com/microsoft/azure-pipelines-image-generation/issues/738
docker_retries=0
brew cask install docker
sudo /Applications/Docker.app/Contents/MacOS/Docker --quit-after-install --unattended
/Applications/Docker.app/Contents/MacOS/Docker --unattended &
while ! docker info 2>/dev/null ; do
    sleep 5
    docker_retries=`expr $docker_retries + 1`
    if pgrep -xq -- "Docker"; then
        echo docker still running
    else
        echo docker not running, restart
        /Applications/Docker.app/Contents/MacOS/Docker --unattended &
    fi
    if [ $docker_retries -gt 20 ]; then
        >&2 echo "Failed to install/run docker."
        exit 1
    fi;

    echo "Waiting for docker service to be in the running state"
done
