#!/bin/bash
# Publish masDependencyMap for Linux x64 as a self-contained single-file executable

echo -e "\033[36mPublishing masDependencyMap for Linux x64...\033[0m"

# Clean previous builds
rm -rf publish/linux-x64

# Publish as self-contained (NOT single-file due to MSBuildLocator limitations)
# Single-file publishing doesn't work with Microsoft.Build.Locator
dotnet publish src/MasDependencyMap.CLI/MasDependencyMap.CLI.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --output publish/linux-x64 \
    /p:PublishReadyToRun=true \
    /p:DebugType=None \
    /p:DebugSymbols=false

if [ $? -eq 0 ]; then
    echo -e "\n\033[32mPublish successful!\033[0m"
    echo -e "\033[32mExecutable location: publish/linux-x64/MasDependencyMap.CLI\033[0m"
    echo -e "\n\033[36mFile size:\033[0m"
    ls -lh publish/linux-x64/MasDependencyMap.CLI | awk '{print $5 " " $9}'

    # Make executable
    chmod +x publish/linux-x64/MasDependencyMap.CLI

    echo -e "\n\033[33mUsage:\033[0m"
    echo "  cd publish/linux-x64"
    echo "  ./MasDependencyMap.CLI analyze --solution path/to/solution.sln"
    echo ""
    echo "Or add publish/linux-x64 to your PATH to use from anywhere:"
    echo "  MasDependencyMap.CLI analyze --solution path/to/solution.sln"
else
    echo -e "\n\033[31mPublish failed!\033[0m"
    exit 1
fi
