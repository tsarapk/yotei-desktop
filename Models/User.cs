using System;
using System.Collections.Generic;

namespace YoteiTasks.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<string> RoleIds { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class Role
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<Permission> Permissions { get; set; } = new();
    }

    public enum Permission
    {
        
        ViewProject,
        EditProject,
        DeleteProject,
        
        
        ViewTasks,
        CreateTasks,
        EditTasks,
        DeleteTasks,
        
        
        ManageUsers,
        ManageRoles,
        
        
        EditHighPriorityTasks,
        EditMediumPriorityTasks,
        EditLowPriorityTasks
    }
}
