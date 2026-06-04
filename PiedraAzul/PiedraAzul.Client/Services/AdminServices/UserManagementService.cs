using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.Admin;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.AdminServices;

public class UserManagementService(GraphQLHttpClient graphQL)
{
    // ─── Query ───────────────────────────────────────────────────

    public async Task<Result<UserListResultGQL>> GetUsersAsync(UserFilterModel filter)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            var query = """
                query GetUsers($filter: UserFilterInput!) {
                    users(filter: $filter) {
                        users {
                            id name email phone identificationNumber documentType gender birthDate
                            avatarUrl roles doctorType isActive emailConfirmed createdAt
                        }
                        totalCount pageNumber pageSize totalPages
                    }
                }
                """;

            var variables = new
            {
                filter = new
                {
                    searchText   = filter.SearchText,
                    roleFilter   = filter.RoleFilter,
                    statusFilter = filter.StatusFilter,
                    pageNumber   = filter.PageNumber,
                    pageSize     = filter.PageSize
                }
            };

            var result = await graphQL.ExecuteAsync<UserListResultGQL>(query, variables, "users");
            return result ?? new UserListResultGQL();
        });
    }

    // ─── Mutations ───────────────────────────────────────────────

    public async Task<Result<AdminUserGQL>> CreateUserAsync(UserCreateModel input)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            var mutation = """
                mutation CreateUser($input: CreateUserInput!) {
                    createUser(input: $input) {
                        id name email phone identificationNumber documentType gender birthDate
                        avatarUrl roles doctorType isActive emailConfirmed createdAt tempPassword
                    }
                }
                """;

            var variables = new
            {
                input = new
                {
                    fullName             = input.FullName,
                    email                = input.Email,
                    phone                = input.Phone,
                    identificationNumber = input.IdentificationNumber,
                    documentType         = input.DocumentType,   // CC/TI/CE/PA
                    gender               = input.Gender,          // MALE/FEMALE/OTHER/NON_SPECIFIED
                    birthDate            = input.BirthDate,
                    role                 = input.Role,
                    doctorType           = input.DoctorType,
                    isActive             = input.IsActive
                }
            };

            var result = await graphQL.ExecuteAsync<AdminUserGQL>(mutation, variables, "createUser");
            return result ?? throw new GraphQLClientException("No se pudo crear el usuario");
        });
    }

    public async Task<Result<AdminUserGQL>> UpdateUserAsync(UserEditModel input)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            var mutation = """
                mutation UpdateUser($input: UpdateUserInput!) {
                    updateUser(input: $input) {
                        id name email phone identificationNumber documentType gender birthDate
                        avatarUrl roles doctorType isActive emailConfirmed createdAt
                    }
                }
                """;

            var variables = new
            {
                input = new
                {
                    userId               = input.UserId,
                    fullName             = input.FullName,
                    email                = input.Email,
                    phone                = input.Phone,
                    identificationNumber = input.IdentificationNumber,
                    documentType         = input.DocumentType,
                    gender               = input.Gender,
                    birthDate            = input.BirthDate,
                    role                 = input.Role,
                    doctorType           = input.DoctorType
                }
            };

            var result = await graphQL.ExecuteAsync<AdminUserGQL>(mutation, variables, "updateUser");
            return result ?? throw new GraphQLClientException("No se pudo actualizar el usuario");
        });
    }

    public async Task<Result<bool>> DeleteUserAsync(string userId)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            var mutation = """
                mutation DeleteUser($userId: String!) {
                    deleteUser(userId: $userId)
                }
                """;

            var result = await graphQL.ExecuteAsync<bool>(mutation, new { userId }, "deleteUser");
            return result;
        });
    }

    public async Task<Result<AdminUserGQL>> ToggleUserStatusAsync(string userId)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            var mutation = """
                mutation ToggleUserStatus($userId: String!) {
                    toggleUserStatus(userId: $userId) {
                        id name email phone avatarUrl roles
                        doctorType isActive emailConfirmed createdAt
                    }
                }
                """;

            var result = await graphQL.ExecuteAsync<AdminUserGQL>(mutation, new { userId }, "toggleUserStatus");
            return result ?? throw new GraphQLClientException("No se pudo cambiar el estado del usuario");
        });
    }
}
