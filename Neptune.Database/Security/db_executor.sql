-- NPT-1112: Least-privilege role granting EXECUTE on all stored procedures /
-- functions. The passwordless workload identity (neptune-<env>-identity) is
-- added to this role by the pipeline db-aad-user step, alongside
-- db_datareader / db_datawriter / db_ddladmin. This replaces the db_owner
-- membership the retired NeptuneWeb SQL login held, without granting ownership,
-- permission-management, or impersonation rights an app identity shouldn't have.
CREATE ROLE [db_executor];
GO

GRANT EXECUTE TO [db_executor];
