#!/bin/sh

instance='r20esdiscordbot-run'
container='r20esdiscordbot'

git pull origin master

docker stop $instance
docker rm $instance
docker build -t $container $container

docker run -dit --restart unless-stopped --name $instance $container
