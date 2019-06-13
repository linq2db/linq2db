Move-Item -Path "win_netfx_job1.json" -Destination "DataProviders.json" -Force

#install postgresql
choco install postgresql10 --force --params '/Password:Password12!'
set PATH=%PATH%;"C:\Program Files\PostgreSQL\10\bin;C:\Program Files\PostgreSQL\10\lib"
