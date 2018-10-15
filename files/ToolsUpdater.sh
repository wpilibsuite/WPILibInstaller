#!/usr/bin/env bash

"$(dirname "$(dirname "$(realpath "$0")")")"/jdk/bin/java -jar "$(dirname "$(realpath "$0")")"/ToolsUpdater.jar "$@"
