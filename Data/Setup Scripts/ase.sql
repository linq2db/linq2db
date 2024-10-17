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
