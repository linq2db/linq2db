create user MANAGED identified by managed;
grant unlimited  tablespace to MANAGED;
grant all privileges to MANAGED identified by managed;
GRANT SELECT ON sys.dba_users TO MANAGED;

create user NATIVE identified by native;
grant unlimited  tablespace to NATIVE;
grant all privileges to NATIVE identified by native;
GRANT SELECT ON sys.dba_users TO NATIVE;

EXIT;
