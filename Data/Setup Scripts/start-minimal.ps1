$commands = ".\\pgsql16.cmd",
            #".\\mariadb.cmd",
            ".\\mysql80.cmd",
            #".\\informix.cmd",
            ".\\db2.cmd",
            ".\\clickhouse.cmd",
            #".\\sqlserver2017.cmd",
            ".\\sqlserver2019.cmd",
            ".\\saphana2.cmd",
            ".\\oracle23.cmd"

$jobs = foreach ($command in $commands) {
    Start-Job -ScriptBlock { & $args[0] } -ArgumentList $command
}

Wait-Job $jobs | Out-Null
Receive-Job $jobs
