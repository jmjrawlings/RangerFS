#!/usr/bin/env bash

set -eu
set -o pipefail

dotnet restore build.proj

if [ ! -f build.fsx ]; then
    fake run init.fsx
fi

fake build $@
