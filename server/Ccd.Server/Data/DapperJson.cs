using System;
using System.Data;
using Dapper;
using Ccd.Server.Helpers;

namespace Ccd.Server.Data;

/// <summary>
/// Dapper TypeHandler used to deserialize JSONB columns into POCO list properties
/// (e.g. ReferralResponse.FileIds, BeneficaryResponse.MatchedFields) when reading
/// rows via PagedApiResponse / raw Dapper queries.
///
/// SetValue is a pass-through: app code never binds list parameters via Dapper, and
/// forcing NpgsqlDbType.Jsonb here breaks third-party Dapper consumers that share the
/// same global TypeHandler registry (notably Hangfire.PostgreSql, whose dequeue query
/// passes the queue list as a List&lt;string&gt; expecting a native text[] binding).
/// </summary>
public class JsonHandler<T> : SqlMapper.TypeHandler<T>
{
    public override T Parse(object value)
    {
        if (value == null || value == DBNull.Value)
        {
            return default(T);
        }

        return Json.Deserialize<T>((string)value);
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = (object)value ?? DBNull.Value;
    }
}
