@echo off
cd ../Server
dotnet ef database update --project Trivia --context IdentityDbContext
pause