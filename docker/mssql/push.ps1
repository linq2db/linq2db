function Push-Image
{
    param
    (
        [string]$Tag
    )

    docker push linq2db/linq2db:$Tag
}

function Push-Version
{
    param
    (
        [string]$Tag
    )

    Push-Image $Tag
}

Push-Version 'win-mssql-2005'
Push-Version 'win-mssql-2008'
Push-Version 'win-mssql-2012'
Push-Version 'win-mssql-2014'
Push-Version 'win-mssql-2016'
Push-Version 'win-mssql-2017'
Push-Version 'win-mssql-2019'
Push-Version 'win-mssql-2022'