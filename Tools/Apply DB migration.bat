@echo off
cd ../Server
dotnet ef database update --project UltimateTrivia --context ApplicationDbContext
pause