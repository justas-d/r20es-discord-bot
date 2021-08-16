#!/bin/bash

dotnet publish -c Release -o out
git add .
git commit
git push origin master
