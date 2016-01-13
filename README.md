DbUtilSqlCmd
============

SQLCMD mode SQL script execution library. Allows your .Net application to run .sql files against SQL Server. Handles the batch delimiters (GO) and SQLCMD mode commands like `:setvar` or `:connect`.

# C# Static Method Example #

```c#
using com.rusanu.DBUtil;

SqlCmd.ExecuteFile(SqlConnection, @"The\Path\To\Your\Sp.sql");

```


# C# Instance Method Example #

```c#
using com.rusanu.DBUtil;

var inst = new SqlCmd(SqlConnection)
inst.ExecuteFile(@"The\Path\To\Your\Sp.sql");

```

# NuGet
Available on NuGet.org as [com.rusanu.DbUtilSqlCmd](https://www.nuget.org/packages/com.rusanu.DbUtilSqlCmd)