Move-Item -Path "win_netfx_job1.json" -Destination "DataProviders.json" -Force

#install postgresql
  $env:PGUSER="postgres"
  $env:PGPASSWORD="Password12!"
  $dbName = "TestDatanet45"

  choco install postgresql9 --force --params '/Password:Password12!'
  set PATH=%PATH%;"C:\Program Files\PostgreSQL\9.6\bin;C:\Program Files\PostgreSQL\9.6\lib"
  $cmd = '"C:\Program Files\PostgreSQL\9.6\bin\createdb" $dbName'
  iex "& $cmd"
