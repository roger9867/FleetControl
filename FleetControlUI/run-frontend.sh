#!/bin/bash
# Startet den Angular-Entwicklungsserver produktiv auf der VM.
# Laeuft relativ zu seinem eigenen Speicherort, damit der Aufrufpfad egal ist.
# allowedHosts wird nicht per CLI-Flag akzeptiert (Vite-basierter Dev-Server),
# sondern steht in angular.json unter serve.options.
cd "$(dirname "$0")"
exec npx ng serve --host 0.0.0.0 --live-reload=false --hmr=false
