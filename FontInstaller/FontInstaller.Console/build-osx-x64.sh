#!/bin/bash

# Build script for FontInstaller - macOS AMD64 (Intel)
# Builds self-contained executable for macOS x64
# Includes code signing with USB token certificate auto-detection
# Note: Notarization is handled separately if needed

set -euo pipefail

PLATFORM="osx-x64"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/bin/publish/$PLATFORM"
DIST_DIR="$SCRIPT_DIR/dist"
APP_NAME="fontinstaller"
BUNDLE_ID="com.font.installer"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}==========================================${NC}"
echo -e "${BLUE}Building FontInstaller for $PLATFORM${NC}"
echo -e "${BLUE}==========================================${NC}"

# Load .env file if it exists
if [[ -f "$SCRIPT_DIR/.env" ]]; then
    echo -e "${YELLOW}Loading environment variables from .env${NC}"
    export $(grep -v '^#' "$SCRIPT_DIR/.env" | xargs)
elif [[ -f "$SCRIPT_DIR/../.env" ]]; then
    echo -e "${YELLOW}Loading environment variables from ../.env${NC}"
    export $(grep -v '^#' "$SCRIPT_DIR/../.env" | xargs)
fi

# Check for code signing identity
SIGNING_IDENTITY="${APPLE_SIGNING_IDENTITY:-Developer ID Application}"
if ! security find-identity -v -p codesigning | grep -q "$SIGNING_IDENTITY"; then
    echo -e "${YELLOW}Warning: No code signing identity found matching '$SIGNING_IDENTITY'.${NC}"
    echo -e "${YELLOW}Code signing will be skipped.${NC}"
    SKIP_SIGNING=1
else
    SKIP_SIGNING=0
    # Get the full identity name
    SIGNING_IDENTITY=$(security find-identity -v -p codesigning | grep "$SIGNING_IDENTITY" | head -1 | sed 's/.*"\(.*\)".*/\1/')
    echo -e "${GREEN}Found signing identity: $SIGNING_IDENTITY${NC}"
fi

# Clean previous build for this platform
echo -e "\n${YELLOW}Cleaning previous build for $PLATFORM...${NC}"
rm -rf "$OUTPUT_DIR"

# Build as single file executable
echo -e "\n${YELLOW}Building as single file executable...${NC}"
dotnet publish "$SCRIPT_DIR/FontInstaller.Console.csproj" \
    -c Release \
    -r "$PLATFORM" \
    -o "$OUTPUT_DIR" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true

if [[ $? -ne 0 ]]; then
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

EXECUTABLE="$OUTPUT_DIR/$APP_NAME"

if [[ ! -f "$EXECUTABLE" ]]; then
    echo -e "${RED}Error: Executable not found at $EXECUTABLE${NC}"
    exit 1
fi

# Make executable
chmod +x "$EXECUTABLE"

# Code signing
if [[ $SKIP_SIGNING -eq 0 ]]; then
    echo -e "\n${YELLOW}Code signing the executable...${NC}"
    
    # Use entitlements file to allow JIT compilation required by .NET CoreCLR
    ENTITLEMENTS_FILE="$SCRIPT_DIR/entitlements.plist"
    if [[ -f "$ENTITLEMENTS_FILE" ]]; then
        echo -e "${YELLOW}Using entitlements: $ENTITLEMENTS_FILE${NC}"
        codesign --force --options runtime --sign "$SIGNING_IDENTITY" \
            --timestamp \
            --identifier "$BUNDLE_ID" \
            --entitlements "$ENTITLEMENTS_FILE" \
            "$EXECUTABLE"
    else
        echo -e "${YELLOW}Warning: Entitlements file not found, signing without entitlements${NC}"
        codesign --force --sign "$SIGNING_IDENTITY" \
            --timestamp \
            --identifier "$BUNDLE_ID" \
            "$EXECUTABLE"
    fi
    
    # Verify signature
    echo -e "${YELLOW}Verifying code signature...${NC}"
    codesign --verify --verbose "$EXECUTABLE"
    echo -e "${GREEN}Code signature verified successfully.${NC}"
fi

# Copy to dist folder with platform suffix
echo -e "\n${YELLOW}Copying to dist folder...${NC}"
mkdir -p "$DIST_DIR"
DIST_FILE="$DIST_DIR/${APP_NAME}.${PLATFORM}"
cp "$EXECUTABLE" "$DIST_FILE"
chmod +x "$DIST_FILE"

echo ""
echo -e "${GREEN}==========================================${NC}"
echo -e "${GREEN}Build complete!${NC}"
echo -e "${GREEN}==========================================${NC}"
echo ""
echo -e "Build output: $EXECUTABLE"
echo -e "Dist output:  $DIST_FILE"
echo ""

echo -e "Note: This is a self-contained macOS (Intel) binary."
echo -e "      It does NOT require .NET runtime to be installed."
echo ""

# Show file size
if [[ -f "$DIST_FILE" ]]; then
    SIZE=$(du -h "$DIST_FILE" | cut -f1)
    echo -e "File size: $SIZE"
fi

if [[ $SKIP_SIGNING -eq 0 ]]; then
    echo -e "${GREEN}✓ Code signed${NC}"
else
    echo -e "${YELLOW}- Not code signed (no certificate found)${NC}"
fi