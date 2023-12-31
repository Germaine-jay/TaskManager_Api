﻿using TaskManager.Models.Dtos;
using TaskManager.Models.Dtos.Request;
using TaskManager.Models.Entities;
using TaskManager.Services.Implementations;
using TaskManager.Services.Infrastructure;

namespace TaskManager.Services.Interfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<ApplicationUserDto>> GetUsers();
        Task<ApplicationUserDto> GetUser(string userId);
        Task<SuccessResponse> LockUser(LockUserRequest request);
        Task<SuccessResponse> DeleteUser(string userId);
        Task<SuccessResponse> UserProjectsWithTasks(string userId);

        Task<IEnumerable<Project>> AllUsersProjectsWithTasks();
    }
}
