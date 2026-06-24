using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ccd.Server.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Ccd.Server.Helpers;

public class PagedApiResponseMeta
{
    public PagedApiResponseMeta() { }

    public PagedApiResponseMeta(
        int page,
        int pageSize,
        int totalRows,
        string sortBy,
        string sortDirection
    )
    {
        Page = page;
        PageSize = pageSize;
        TotalRows = totalRows;
        TotalPages = Convert.ToInt32(Math.Ceiling((decimal)totalRows / pageSize));
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public string SortBy { get; set; }
    public string SortDirection { get; set; }
}

public class PagedApiResponse<T>
{
    private const int DEFAULT_PAGE_SIZE = 20;

    private static readonly Regex ValidIdentifier =
        new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    public List<T> Data { get; set; }
    public PagedApiResponseMeta Meta { get; set; }

    private static string SanitizeSortDirection(string direction) =>
        direction == "desc" ? "desc" : "asc";

    private static (string sql, DynamicParameters parameters) generateSearchSql<TT>(
        RequestParameters parameters
    )
    {
        var sqlSearch = "";
        var dynParams = new DynamicParameters();

        if (!string.IsNullOrEmpty(parameters?.Search))
        {
            var searchColumnList = (
                from pi in typeof(TT).GetProperties()
                where Attribute.IsDefined(pi, typeof(QuickSearchable))
                select pi.Name
            ).ToList();

            dynParams.Add("searchVal", parameters.Search);

            sqlSearch = " (";

            sqlSearch += "id::text ILIKE '%' || @searchVal || '%'";

            for (var i = 0; i < searchColumnList.Count; i++)
                sqlSearch +=
                    " OR " + searchColumnList[i].ToSnakeCase() + " ILIKE '%' || @searchVal || '%'";

            sqlSearch += ") ";
        }

        return (sqlSearch, dynParams);
    }

    private static (string sql, DynamicParameters parameters) generateFilterSql<TT>(
        RequestParameters parameters
    )
    {
        var sqlFilter = "";
        var dynParams = new DynamicParameters();

        if (parameters?.FilterList.Count > 0)
        {
            var filterColumnList = (
                from pi in typeof(TT).GetProperties()
                select pi.Name.ToSnakeCase()
            ).ToList();

            if (filterColumnList.Count > 0)
            {
                sqlFilter = " (";
                var sqlCondition = "AND";
                var i = 0;
                var keys = parameters.FilterList.Keys.ToList();

                foreach (var key in keys)
                {
                    var columnRaw = key.Replace("[gt]", "")
                        .Replace("[lt]", "")
                        .Replace("[not]", "")
                        .Replace("[like]", "")
                        .Replace("[in]", "")
                        .Replace("[or]", "")
                        .Replace("[contains]", "");

                    var isCustomField = columnRaw.Contains('.');

                    string column;

                    if (isCustomField)
                    {
                        var basePart = columnRaw.Split('.')[0];
                        var jsonKey = columnRaw.Split('.')[1];

                        if (!ValidIdentifier.IsMatch(basePart) || !ValidIdentifier.IsMatch(jsonKey))
                            throw new BadRequestException($"Invalid custom field filter: {key}");

                        var baseColumn = basePart.ToSnakeCase();
                        if (!filterColumnList.Contains(baseColumn))
                            throw new BadRequestException($"Invalid filter column: {basePart}");

                        column = baseColumn + "->>'" + jsonKey + "'";
                    }
                    else
                    {
                        column = columnRaw.ToSnakeCase();

                        if (!filterColumnList.Contains(column))
                            throw new BadRequestException($"Invalid filter: {column}");
                    }

                    var filterType = isCustomField
                        ? typeof(string)
                        : typeof(TT)
                            .GetProperties()
                            .First(e => e.Name.ToSnakeCase() == column)
                            ?.PropertyType;

                    var cast = "::varchar";
                    var isNumeric = false;

                    if (filterType == typeof(decimal) || filterType == typeof(int))
                    {
                        cast = "";
                        isNumeric = true;
                    }

                    var value = parameters.FilterList[key];

                    if (key.Contains("[or]"))
                        sqlCondition = "OR";
                    else
                        sqlCondition = "AND";

                    if (i > 0)
                        sqlFilter += $" {sqlCondition} ";

                    if (value == "$null")
                    {
                        sqlFilter += column + " is null";
                    }
                    else if (value == "$notnull")
                    {
                        sqlFilter += column + " is not null";
                    }
                    else if (filterType == typeof(List<string>))
                    {
                        var paramName = $"filterVal{i}";
                        dynParams.Add(paramName, "[\"" + value + "\"]");
                        sqlFilter += column + $" @> @{paramName}::jsonb";
                    }
                    else if (key.Contains("[in]"))
                    {
                        var inValues = value.Split("|");
                        var paramNames = new List<string>();
                        for (var j = 0; j < inValues.Length; j++)
                        {
                            var paramName = $"filterIn{i}_{j}";
                            if (isNumeric)
                                dynParams.Add(paramName, decimal.Parse(inValues[j]));
                            else
                                dynParams.Add(paramName, inValues[j]);
                            paramNames.Add($"@{paramName}");
                        }

                        sqlFilter += column + $"{cast} in ({string.Join(",", paramNames)})";
                    }
                    else if (key.Contains("[like]"))
                    {
                        if (isNumeric)
                            throw new BadRequestException(
                                $"Invalid [like] filter for numeric column: {column}"
                            );

                        var paramName = $"filterVal{i}";
                        dynParams.Add(paramName, $"%{value}%");
                        sqlFilter += column + $"{cast} ilike @{paramName}";
                    }
                    else if (key.Contains("[contains]"))
                    {
                        var paramName = $"filterVal{i}";
                        dynParams.Add(paramName, value);
                        sqlFilter += column + $" @> @{paramName}";
                    }
                    else
                    {
                        var sqlOperator = "=";
                        if (key.Contains("[lt]"))
                            sqlOperator = "<";
                        if (key.Contains("[gt]"))
                            sqlOperator = ">=";
                        if (key.Contains("[not]"))
                            sqlOperator = "<>";

                        var paramName = $"filterVal{i}";
                        if (isNumeric)
                            dynParams.Add(paramName, decimal.Parse(value));
                        else
                            dynParams.Add(paramName, value);

                        if (isCustomField)
                            sqlFilter += column + $" {sqlOperator} @{paramName}";
                        else
                            sqlFilter += column + $"{cast} {sqlOperator} @{paramName}";
                    }

                    i++;
                }

                sqlFilter += ") ";
            }
        }

        return (sqlFilter, dynParams);
    }

    private static bool matchesQuickSearchableFields<TT>(TT item, string search)
    {
        if (string.IsNullOrEmpty(search))
            return true;

        var searchableProperties = typeof(TT)
            .GetProperties()
            .Where(
                pi =>
                    Attribute.IsDefined(pi, typeof(QuickSearchable))
                    || string.Equals(pi.Name, "Id", StringComparison.OrdinalIgnoreCase)
            );

        return searchableProperties.Any(
            property =>
                property
                    .GetValue(item)
                    ?.ToString()
                    ?.Contains(search, StringComparison.OrdinalIgnoreCase) == true
        );
    }

    public static async Task<PagedApiResponse<T>> GetFromSql(
        DbContext context,
        string sql,
        object sqlParams,
        RequestParameters parameters,
        Func<T, Task> resolveDependencies = null,
        Func<T, string, Task> resolveDependenciesWithLanguage = null,
        string language = null,
        Func<T, string> resolveDependenciesSearch = null,
        bool searchAfterResolve = false
    )
    {
        var page = parameters?.Page ?? 1;
        var pageSize = parameters?.PageSize ?? DEFAULT_PAGE_SIZE;

        var offset = (page - 1) * pageSize;
        var limit = pageSize;

        var sqlOrder = "";
        var sortDirection = SanitizeSortDirection(parameters?.SortDirection);

        if (!string.IsNullOrEmpty(parameters?.SortBy))
        {
            var sortByInput = parameters.SortBy;
            var needsQuoting = sortByInput.StartsWith('"') && sortByInput.EndsWith('"');
            var sortByClean = needsQuoting ? sortByInput.Trim('"') : sortByInput;

            var sortProperty = typeof(T)
                .GetProperties()
                .FirstOrDefault(e => e.Name.ToSnakeCase() == sortByClean.ToSnakeCase());

            if (sortProperty == null)
                throw new BadRequestException($"Invalid sort column: {parameters.SortBy}");

            var sortColumn = sortProperty.Name.ToSnakeCase();
            var sortColumnSql = needsQuoting ? $"\"{sortColumn}\"" : sortColumn;

            if (sortProperty.IsDefined(typeof(SortAsNumberAttribute), false))
                sqlOrder =
                    $@" ORDER BY right('00000000000000000000' || {sortColumnSql}, 20) {sortDirection}";
            else
                sqlOrder = $@" ORDER BY {sortColumnSql} {sortDirection}";
        }
        else
        {
            sqlOrder = @" ORDER BY id";
        }

        var sqlPaging = $@" OFFSET {offset.ToString()} LIMIT {limit.ToString()}";

        var sqlWhere = "";
        var (sqlSearch, searchParams) = searchAfterResolve
            ? ("", new DynamicParameters())
            : generateSearchSql<T>(parameters);
        var (sqlFilter, filterParams) = generateFilterSql<T>(parameters);

        if (!string.IsNullOrEmpty(sqlSearch) || !string.IsNullOrEmpty(sqlFilter))
        {
            sqlWhere = " WHERE ";
            if (!string.IsNullOrEmpty(sqlSearch))
                sqlWhere += sqlSearch;
            if (!string.IsNullOrEmpty(sqlSearch) && !string.IsNullOrEmpty(sqlFilter))
                sqlWhere += " AND ";
            if (!string.IsNullOrEmpty(sqlFilter))
                sqlWhere += sqlFilter;
        }

        var shouldSearchAfterResolve =
            searchAfterResolve && !string.IsNullOrEmpty(parameters?.Search);
        var sqlWithConditions =
            $@"SELECT * FROM ( {sql} ) as result "
            + sqlWhere
            + sqlOrder
            + (shouldSearchAfterResolve ? "" : sqlPaging);
        var sqlCount = $@"SELECT COUNT(*) as row_count FROM ( {sql} ) as result {sqlWhere}";

        var allParams = new DynamicParameters(sqlParams);
        allParams.AddDynamicParams(searchParams);
        allParams.AddDynamicParams(filterParams);

        var items = await context.Database
            .GetDbConnection()
            .QueryAsync<T>(sqlWithConditions, allParams);
        var totalRows = shouldSearchAfterResolve
            ? 0
            : await context.Database.GetDbConnection().QuerySingleAsync<int>(sqlCount, allParams);

        var data = items.ToList();

        if (resolveDependenciesWithLanguage != null)
            foreach (var item in data)
                await resolveDependenciesWithLanguage(item, language);
        else if (resolveDependencies != null)
            foreach (var item in data)
                await resolveDependencies(item);
        else if (resolveDependenciesSearch != null)
            foreach (var item in data)
                resolveDependenciesSearch(item);

        if (shouldSearchAfterResolve)
        {
            data = data.Where(item => matchesQuickSearchableFields(item, parameters.Search))
                .ToList();
            totalRows = data.Count;
            data = data.Skip(offset).Take(limit).ToList();
        }

        var response = new PagedApiResponse<T>
        {
            Data = data,
            Meta = new PagedApiResponseMeta(
                page,
                pageSize,
                totalRows,
                parameters?.SortBy,
                sortDirection
            )
        };

        return response;
    }
}
