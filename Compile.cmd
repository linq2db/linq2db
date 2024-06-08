@echo off

cd /d "%~dp0"

dotnet clean Source\LinqToDB\LinqToDB.csproj -c Debug -v m
dotnet clean Source\LinqToDB\LinqToDB.csproj -c Release -v m
dotnet build Source\LinqToDB\LinqToDB.csproj -c Debug -v m
dotnet build Source\LinqToDB\LinqToDB.csproj -c Release -v m
