using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.Admin;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.AdminServices;

public class AuditLogService(GraphQLHttpClient graphQL)
{
    public async Task<Result<AuditLogListGQL>> GetAuditLogsAsync(AuditFilterModel filter)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string query = """
                query GetAuditLogs($filter: AuditFilterInput!) {
                    auditLogs(filter: $filter) {
                        items {
                            id timestamp entityType entityId action source
                            actorUserId actorName actorRoles ipAddress
                            subjectIdentification subjectName subjectPhone data
                        }
                        totalCount pageNumber pageSize totalPages
                    }
                }
                """;

            var variables = new
            {
                filter = new
                {
                    searchText = filter.SearchText,
                    entityType = filter.EntityType,
                    action     = filter.Action,
                    source     = filter.Source,
                    dateFrom   = filter.DateFrom,
                    dateTo     = filter.DateTo,
                    pageNumber = filter.PageNumber,
                    pageSize   = filter.PageSize
                }
            };

            var result = await graphQL.ExecuteAsync<AuditLogListGQL>(query, variables, "auditLogs");
            return result ?? new AuditLogListGQL();
        });
    }

    public async Task<Result<AuditFilterOptionsGQL>> GetFilterOptionsAsync()
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string query = """
                query GetAuditFilterOptions {
                    auditFilterOptions { entityTypes actions }
                }
                """;

            var result = await graphQL.ExecuteAsync<AuditFilterOptionsGQL>(query, null, "auditFilterOptions");
            return result ?? new AuditFilterOptionsGQL();
        });
    }
}
