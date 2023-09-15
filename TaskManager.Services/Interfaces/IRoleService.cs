﻿using TaskManager.Services.Infrastructure;
using TaskManager.Services.Implementations;

namespace TaskManager.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleResponse>> GetAllRoles();
        Task<IEnumerable<string>> GetUserRoles(string userName);
        Task<SuccessResponse> RemoveUserFromRole(AddUserToRoleRequest request);
        Task<SuccessResponse> EditRole(string id, string Name);
        Task<SuccessResponse> DeleteRole(string name);
        Task<AddUserToRoleResponse> AddUserToRole(AddUserToRoleRequest request);
        Task<SuccessResponse> CreateRoleAync(RoleDto request);
    }
}
