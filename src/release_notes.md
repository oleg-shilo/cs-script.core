# Release v1.4.5.0-NET5-RC5

## CLI
- Added complex (multi-file) commands support (e.g. `css -self-test-run`)
- Added `css -self-test` command for testing the engine on the target system
- Added `css -self-exe` command for building css launcher for manual deployment
- Build changes to support Linux distro
- Added `-vscode` help info
- Added `css -server:restart` CLI parameter
- Added detection of CLI host and build server running with different root privileges on Linux