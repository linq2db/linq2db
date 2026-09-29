<#
mssql-container.ps1 - bring up the SQL Server container(s) a Windows test leg needs, and keep
retrying until they answer or the budget runs out. Called by every sqlserver.*.cmd, which then
runs its own version-specific `docker exec ... sqlcmd -Q "..."` setup statements.

Replaces the hand-rolled `docker run` + `set max=100` / `sleep 1` / `docker exec ... SELECT 1`
loop those scripts each carried. That loop could not distinguish the three ways the setup fails,
and recovered from none of them:

  - the agent's docker daemon is not reachable at all. On build 23263 the Win d_SqlServer2012 leg
    got "failed to connect to the docker API ... check if the path is correct and if the daemon is
    running" from every single call, so `docker run` never created anything and the wait loop then
    spent 106s asking a container that did not exist for a row. Every other Windows docker leg in
    that build was green, so it is a per-agent flake, not a broken image.
  - the container was created but died during startup. firebird.sh already checks for this
    (docker inspect .State.Running) and fails fast with the logs; the Windows scripts waited out
    the full budget instead.
  - the container was never created, or came up wedged. Nothing re-created it, so a single lost
    `docker run` was terminal for the leg.

So: probe the daemon first (and give a stopped docker service one push), then treat "container up
and answering" as the thing being retried rather than assuming one `docker run` is enough.
`docker rm -f` before each attempt is what makes a retry idempotent - without it a second attempt
dies on "name is already in use" whenever the first one did create the container.

All of a leg's containers are started before any of them is waited on, so the merged 2017+2019
and 2022+2025 legs keep initialising both servers concurrently the way they do today.

Invoked from a .cmd as:

  pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql2012|1412|linq2db/linq2db:win-mssql-2012"
  if %errorlevel% NEQ 0 exit /b 1

Parameters:
  -Container          one or more `name|hostPort|image|sqlcmdExtraArgs` specs. The last field is
                      optional and is appended to every readiness probe (the 2025 image ships
                      mssql-tools18, whose sqlcmd needs -C to accept the self-signed cert).
                      Several specs may be passed as separate array elements or comma-separated
                      inside one string - `pwsh -File` hands array syntax through as a literal
                      string, so the comma form is what a .cmd can actually rely on.
  -SaPassword         sa password. Must match the connection strings in DataProviders.json.
  -MaxAttempts        create-and-wait attempts before giving up.
  -DaemonTimeoutSec   how long to wait for the docker daemon before the first attempt.
  -ReadyTimeoutSec    how long to wait for one container to accept a query, per attempt.

The three budgets are picked, not measured: the step they replace had a fixed ~110s ceiling, and
the worst case here is ~120s + 3 x 100s = ~7 min. That only elapses on a leg that was going to
fail anyway, and a Windows test leg runs for tens of minutes, so trading the extra minutes for a
recovered leg is worth it. A healthy container answers in a few seconds and pays none of it.
#>

param(
    [Parameter(Mandatory = $true)][string[]] $Container,
    [string] $SaPassword       = 'Password12!',
    [int]    $MaxAttempts      = 3,
    [int]    $DaemonTimeoutSec = 120,
    [int]    $ReadyTimeoutSec  = 100
)

# Parse the specs up front so a typo fails before anything is created.
$specs = foreach ($entry in ($Container -split ',')) {
    $text = $entry.Trim()
    if (-not $text) { continue }

    $parts = $text -split '\|'
    if ($parts.Count -lt 3) {
        Write-Host "##vso[task.logissue type=error]mssql-container: '${text}' is not a name|hostPort|image[|sqlcmdExtraArgs] spec"
        exit 1
    }

    [pscustomobject]@{
        Name      = $parts[0]
        Port      = $parts[1]
        Image     = $parts[2]
        ExtraArgs = @(if ($parts.Count -gt 3 -and $parts[3]) { $parts[3] -split '\s+' })
        Ready     = $false
    }
}

$specs = @($specs)
if ($specs.Count -eq 0) {
    Write-Host "##vso[task.logissue type=error]mssql-container: no container specs given"
    exit 1
}

# True when the daemon answers. `docker version` is the probe rather than `docker info` because it
# reports the *server* version, which only exists once the daemon is reachable - the client half
# answers regardless.
function Test-DockerDaemon {
    $null = docker version --format '{{.Server.Version}}' 2>&1
    return $LASTEXITCODE -eq 0
}

function Wait-DockerDaemon {
    if (Test-DockerDaemon) {
        return $true
    }

    Write-Host "Docker daemon is not responding, waiting up to ${DaemonTimeoutSec}s"

    # The hosted image runs dockerd as a Windows service. If that service is not running, waiting
    # cannot help on its own, so push it once - but keep polling either way, because a daemon that
    # is merely slow to come up looks identical from here.
    $service = Get-Service -Name docker -ErrorAction SilentlyContinue
    if ($service -and $service.Status -ne 'Running') {
        Write-Host "Docker service status is '$($service.Status)', trying to start it"
        try {
            Start-Service -Name docker -ErrorAction Stop
            Write-Host "Docker service started"
        }
        catch {
            Write-Host "Failed to start the docker service: $($_.Exception.Message)"
        }
    }

    $deadline = (Get-Date).AddSeconds($DaemonTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        if (Test-DockerDaemon) {
            Write-Host "Docker daemon is responding"
            return $true
        }
    }

    return $false
}

# False as soon as the container is known to be gone or stopped, so a crashed startup is not
# waited out for the whole readiness budget.
function Test-ContainerRunning([string]$name) {
    $state = docker inspect -f '{{.State.Running}}' $name 2>&1
    return $LASTEXITCODE -eq 0 -and "$state".Trim() -eq 'true'
}

function Test-SqlReady($spec) {
    # Not $args - that is an automatic variable, and assigning to it inside a function shadows the
    # caller's argument list.
    $dockerArgs = @('exec', $spec.Name, 'sqlcmd', '-S', 'localhost', '-U', 'sa', '-P', $SaPassword, '-Q', 'SELECT 1') + $spec.ExtraArgs
    $null = docker @dockerArgs 2>&1
    return $LASTEXITCODE -eq 0
}

if (-not (Wait-DockerDaemon)) {
    Write-Host "##vso[task.logissue type=error]mssql-container: the docker daemon did not become available within ${DaemonTimeoutSec}s"
    exit 1
}

for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    $pending = @($specs | Where-Object { -not $_.Ready })

    Write-Host "=== Attempt ${attempt} of ${MaxAttempts}: starting $($pending.Name -join ', ') ==="

    foreach ($spec in $pending) {
        # Ignore the outcome: on the first attempt there is normally nothing to remove, and on a
        # later one this is the whole point - it clears whatever the failed attempt left behind so
        # `docker run` does not fail on a name collision.
        docker rm -f $spec.Name 2>&1 | Out-Null

        docker run -d `
            -e "ACCEPT_EULA=Y" `
            -e "MSSQL_SA_PASSWORD=$SaPassword" `
            -p "$($spec.Port):1433" `
            -h $spec.Name `
            --name $spec.Name `
            $spec.Image
    }

    docker ps -a

    foreach ($spec in $pending) {
        Write-Host "Waiting up to ${ReadyTimeoutSec}s for $($spec.Name) to accept connections"

        $deadline = (Get-Date).AddSeconds($ReadyTimeoutSec)
        while ((Get-Date) -lt $deadline) {
            if (Test-SqlReady $spec) {
                $spec.Ready = $true
                Write-Host "$($spec.Name) is ready"
                break
            }

            if (-not (Test-ContainerRunning $spec.Name)) {
                Write-Host "$($spec.Name) is not running (crashed or exited during startup)"
                break
            }

            Start-Sleep -Seconds 1
        }

        if (-not $spec.Ready) {
            Write-Host "$($spec.Name) did not become ready"
            docker logs $spec.Name
        }
    }

    if (@($specs | Where-Object { -not $_.Ready }).Count -eq 0) {
        exit 0
    }
}

$failed = @($specs | Where-Object { -not $_.Ready }).Name -join ', '
Write-Host "##vso[task.logissue type=error]mssql-container: ${failed} did not become ready after ${MaxAttempts} attempts"
exit 1
