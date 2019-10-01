#!/bin/bash

# osx agent doesn't have docker pre-installed, so we need to do it manually
# unfortunatelly, installer is not very good and sometimes just doesn't work
#https://github.com/microsoft/azure-pipelines-image-generation/issues/738
retries=0
# recent 37199 version of docker fails to start, so we need to specify most recent working one
brew cask install docker
# install working 2.0.0.3-ce-mac81,31259
#brew cask install https://raw.githubusercontent.com/Homebrew/homebrew-cask/8ce4e89d10716666743b28c5a46cd54af59a9cc2/Casks/docker.rb
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
        echo 'searching for logs start'
        ls '/Users/vsts/Library/Containers/com.docker.docker/Data/vms/0/data'
        tree '/Users/vsts/Library/Containers/com.docker.docker/Data/vms/data'
        cat '/Users/vsts/Library/Containers/com.docker.docker/Data/vms/data'
        echo 'searching for logs end'
    fi
    if [ $retries -gt 30 ]; then
        >&2 echo 'Failed to run docker'
        exit 1
    fi;

    echo 'Waiting for docker service to be in the running state'
done
