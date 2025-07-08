@echo off
echo ItemFilterLibrary Local API Server
echo ====================================
echo.
echo IMPORTANT: This is a SEPARATE API server, not part of the plugin!
echo The plugin compiles independently in ExileCore.
echo.
echo Starting the API server on http://localhost:5000
echo.
echo Make sure you have .NET 8.0 SDK installed!
echo.
echo Press Ctrl+C to stop the server
echo.

cd ItemFilterLibraryAPI
dotnet run

pause 