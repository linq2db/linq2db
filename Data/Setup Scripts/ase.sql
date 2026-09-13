use master
go
sp_dboption tempdb, 'ddl in tran', 'true'
go
disk resize name='master', size='100m'
go
create database TestData on default
go
create database TestDataCore on default
go
sp_dboption TestData, 'trunc log on chkpt', 'true'
go
sp_dboption TestDataCore, 'trunc log on chkpt', 'true'
go
sp_dboption TestData, 'abort tran on log full', 'true'
go
sp_dboption TestDataCore, 'abort tran on log full', 'true'
go