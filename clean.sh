#!/bin/bash
find . -type d \( -name "obj" -o -name "bin" \) -prune -exec rm -rf {} + 2>/dev/null
dotnet restore