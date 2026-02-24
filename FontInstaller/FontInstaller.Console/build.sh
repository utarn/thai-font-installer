#!/bin/bash

# Universal build script for FontInstaller
# Detects platform and runs appropriate build script
# Supports Windows (via PowerShell), macOS (ARM64 and x64)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_NAME="fontinstaller"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

show_usage() {
    echo "Usage: $0 [platform]"
    echo "Available platforms:"
    echo "  osx-arm64    - Build for macOS ARM64 (Apple Silicon)"
    echo "  osx-x64      - Build for macOS x64 (Intel)"
    echo "  win-x64      - Build for Windows x64 (requires PowerShell)"
    echo "  all          - Build for all platforms (macOS only from macOS)"
    echo "  help         - Show this help message"
    echo ""
    echo "If no platform is specified, the script will detect the current OS and build accordingly."
}

detect_platform() {
    local os_name=$(uname -s)
    case $os_name in
        Darwin*)
            # Check architecture
            local arch=$(uname -m)
            if [[ "$arch" == "arm64" ]]; then
                echo "osx-arm64"
            else
                echo "osx-x64"
            fi
            ;;
        MINGW*|MSYS*|CYGWIN*|Windows_NT)
            echo "win-x64"
            ;;
        *)
            echo "unknown"
            ;;
    esac
}

build_osx_arm64() {
    echo -e "${BLUE}Building for macOS ARM64...${NC}"
    "$SCRIPT_DIR/build-osx-arm64.sh"
}

build_osx_x64() {
    echo -e "${BLUE}Building for macOS x64...${NC}"
    "$SCRIPT_DIR/build-osx-x64.sh"
}

build_win_x64() {
    echo -e "${BLUE}Building for Windows x64...${NC}"
    if command -v pwsh >/dev/null 2>&1; then
        pwsh -File "$SCRIPT_DIR/build-win-x64.ps1"
    elif command -v powershell >/dev/null 2>&1; then
        powershell -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build-win-x64.ps1"
    else
        echo -e "${RED}Error: PowerShell is required to build for Windows but was not found.${NC}"
        exit 1
    fi
}

main() {
    local target_platform=${1:-"auto"}
    
    case $target_platform in
        "help"|"-h"|"--help")
            show_usage
            exit 0
            ;;
        "auto")
            target_platform=$(detect_platform)
            if [[ "$target_platform" == "unknown" ]]; then
                echo -e "${RED}Error: Unable to detect platform automatically.${NC}"
                echo "Please specify a platform explicitly."
                show_usage
                exit 1
            fi
            ;;
        "all")
            echo -e "${BLUE}Building for all platforms...${NC}"
            
            # Build macOS ARM64 if on macOS
            if [[ "$(uname -s)" == "Darwin"* ]]; then
                build_osx_arm64
                build_osx_x64
            else
                echo -e "${YELLOW}Skipping macOS builds (not on macOS)${NC}"
            fi
            exit 0
            ;;
        "osx-arm64"|"osx-x64"|"win-x64")
            # Valid platform, continue
            ;;
        *)
            echo -e "${RED}Error: Invalid platform '$target_platform'${NC}"
            show_usage
            exit 1
            ;;
    esac
    
    case $target_platform in
        "osx-arm64")
            build_osx_arm64
            ;;
        "osx-x64")
            build_osx_x64
            ;;
        "win-x64")
            build_win_x64
            ;;
        *)
            echo -e "${RED}Error: Unsupported platform: $target_platform${NC}"
            exit 1
            ;;
    esac
}

main "$@"