Param(
	[Parameter(Mandatory=$true)][string]$gitHubUser,
    [Parameter(Mandatory=$true)][string]$gitHubAccessToken,
    [Parameter(Mandatory=$true)][string]$gitHubUserEmail,
    [Parameter(Mandatory=$true)][bool]$gitDeploy
)


Write-Host Building docfx

rm .\Doc\_site\ -Recurse -Force
rm linq2db.github.io -Recurse -Force
rm .\Doc\api\*.yml
rm .\Doc\api\.manifest

.\Redist\NuGet.exe install msdn.4.5.2 -ExcludeVersion -OutputDirectory ./tools -Prerelease

choco install docfx

docfx .\Doc\docfx.json

git clone https://github.com/linq2db/linq2db.github.io.git -b master linq2db.github.io -q

if ($LASTEXITCODE -ne 0)
{
    throw "DocFx build failed";
}

Copy-Item linq2db.github.io/.git ./doc/_site -recurse

cd .\doc\_site

git config core.autocrlf true
git config user.name $gitHubUserEmail
git config user.email $gitHubUserEmail

git add -A 2>&1
git commit -m "CI docfx update" -q

if ($gitDeploy)
{
    git push "https://$($gitHubUser):$($gitHubAccessToken)@github.com/linq2db/linq2db.github.io.git" master -q
}

cd ..\..
