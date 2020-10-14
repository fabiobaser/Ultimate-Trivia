cd ../Server

@echo off
echo "Provide a name for the migration file"
set /p name="name: ": 
dotnet ef migrations add %name% --project Trivia --context ApplicationDbContext
pause


