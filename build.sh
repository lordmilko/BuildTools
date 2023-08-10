#!/bin/bash

BASEDIR="$(dirname "$BASH_SOURCE")"
pwsh -executionpolicy bypass -noexit -noninteractive -command "ipmo psreadline; . '$BASEDIR/build/Bootstrap.ps1'"