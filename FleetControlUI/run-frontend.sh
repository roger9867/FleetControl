#!/bin/bash
# Starts the Angular development server in production on the VM.
# Runs relative to its own location, so the calling path does not matter.
# allowedHosts is not accepted as a CLI flag (Vite-based dev server),
# but is set in angular.json under serve.options.
cd "$(dirname "$0")"
exec npx ng serve --host 0.0.0.0 --live-reload=false --hmr=false
