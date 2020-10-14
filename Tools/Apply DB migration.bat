@echo off
cd ../Server
dotnet ef database update --project Trivia --context ApplicationDbContext
pause