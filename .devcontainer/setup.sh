#!/bin/bash
# PPDS devcontainer post-create setup
# Configures Claude Code auth + marketplace, restores .NET packages
set -e

CLAUDE_DIR="$HOME/.claude"
PLUGINS_DIR="$CLAUDE_DIR/plugins"
STAGED_CONFIG="/tmp/host-claude-config.json"
STAGED_SETTINGS="/tmp/host-claude-settings.json"

# --- Step 1: Extract oauthAccount from staged host config ---
echo "=== Configuring Claude Code authentication ==="
if [ -f "$STAGED_CONFIG" ]; then
    node -e "
        const fs = require('fs');
        const host = JSON.parse(fs.readFileSync('$STAGED_CONFIG', 'utf8'));
        if (host.oauthAccount) {
            const clean = {
                oauthAccount: host.oauthAccount,
                hasCompletedOnboarding: true
            };
            fs.writeFileSync('$HOME/.claude.json', JSON.stringify(clean, null, 2));
            console.log('Auth config created for: ' + host.oauthAccount.emailAddress);
        } else {
            console.warn('WARNING: No oauthAccount in host config. Manual login required.');
        }
    "
else
    echo "WARNING: Host config not found at $STAGED_CONFIG. Manual login required."
fi

# --- Step 2: Copy settings.json if available ---
echo "=== Configuring Claude Code settings ==="
if [ -f "$STAGED_SETTINGS" ]; then
    mkdir -p "$CLAUDE_DIR"
    cp "$STAGED_SETTINGS" "$CLAUDE_DIR/settings.json"
    echo "Settings copied from host."
else
    echo "No host settings found. Using defaults."
fi

# --- Step 3: Initialize marketplace with Linux paths ---
echo "=== Setting up Claude Code marketplace ==="
mkdir -p "$PLUGINS_DIR/marketplaces"

if [ ! -d "$PLUGINS_DIR/marketplaces/claude-plugins-official" ]; then
    echo "Cloning Anthropic official marketplace..."
    git clone --depth 1 https://github.com/anthropics/claude-plugins-official.git \
        "$PLUGINS_DIR/marketplaces/claude-plugins-official"
    echo "Marketplace cloned."
else
    echo "Marketplace already present, skipping clone."
fi

node -e "
    const fs = require('fs');
    const marketplaces = {
        'claude-plugins-official': {
            source: { source: 'github', repo: 'anthropics/claude-plugins-official' },
            installLocation: '$PLUGINS_DIR/marketplaces/claude-plugins-official',
            lastUpdated: new Date().toISOString()
        }
    };
    fs.mkdirSync('$PLUGINS_DIR', { recursive: true });
    fs.writeFileSync('$PLUGINS_DIR/known_marketplaces.json', JSON.stringify(marketplaces, null, 2));
    console.log('Marketplace registry written with Linux paths.');
"

# --- Step 4: Restore .NET packages ---
echo "=== Restoring .NET packages ==="
dotnet restore PPDS.sln

echo "=== Setup complete ==="
