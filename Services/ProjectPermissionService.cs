using System;
using System.Collections.Generic;
using System.Linq;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.Services;




public class ProjectPermissionService
{
    private readonly Yotei _yotei;
    private readonly ISuperUserService? _superUserService;

    public ProjectPermissionService(Yotei yotei, ISuperUserService? superUserService = null)
    {
        _yotei = yotei;
        _superUserService = superUserService;
    }

    
    
    
    public ProjectPermissions GetPermissions(Project? project, Actor? currentActor)
    {
        if (currentActor == null)
        {
            return new ProjectPermissions(); 
        }

        
        if (_superUserService?.IsSuperUser(currentActor) == true)
        {
            return new ProjectPermissions
            {
                CanViewProject = true,
                CanEditProject = true,
                CanDeleteProject = true,
                CanCreateTasks = true,
                CanEditTasks = true,
                CanDeleteTasks = true,
                CanCompleteTasks = true,
                CanManageUsers = true,
                CanManageRoles = true
            };
        }

        if (project == null)
        {
            return new ProjectPermissions(); 
        }

        
        var actorRoleId = project.GetActorRole(currentActor.Id.ToString());
        if (string.IsNullOrEmpty(actorRoleId))
        {
            return new ProjectPermissions(); 
        }

        
        var role = _yotei.Roles.GetAll().FirstOrDefault(r => r.Id.ToString() == actorRoleId);
        if (role == null)
        {
            return new ProjectPermissions(); 
        }

        
        var hasFullAccess = role.privs.Contains(RolePriv.FullAccess);
        
        return new ProjectPermissions
        {
            CanViewProject = hasFullAccess || role.privs.Contains(RolePriv.CanReadProject),
            CanEditProject = hasFullAccess || role.privs.Contains(RolePriv.CanWriteProject),
            CanDeleteProject = hasFullAccess,
            CanCreateTasks = hasFullAccess || role.privs.Contains(RolePriv.CanAddNewTasks),
            CanEditTasks = hasFullAccess || role.privs.Contains(RolePriv.CanWriteProject),
            CanDeleteTasks = hasFullAccess,
            CanCompleteTasks = hasFullAccess || role.privs.Contains(RolePriv.CanWriteProject),
            CanManageUsers = hasFullAccess,
            CanManageRoles = hasFullAccess
        };
    }

    
    
    
    public bool CanPerformAction(Project? project, Actor? currentActor, ProjectAction action)
    {
        var permissions = GetPermissions(project, currentActor);
        
        return action switch
        {
            ProjectAction.ViewProject => permissions.CanViewProject,
            ProjectAction.EditProject => permissions.CanEditProject,
            ProjectAction.DeleteProject => permissions.CanDeleteProject,
            ProjectAction.CreateTask => permissions.CanCreateTasks,
            ProjectAction.EditTask => permissions.CanEditTasks,
            ProjectAction.DeleteTask => permissions.CanDeleteTasks,
            ProjectAction.CompleteTask => permissions.CanCompleteTasks,
            ProjectAction.ManageUsers => permissions.CanManageUsers,
            ProjectAction.ManageRoles => permissions.CanManageRoles,
            _ => false
        };
    }
}




public class ProjectPermissions
{
    public bool CanViewProject { get; set; }
    public bool CanEditProject { get; set; }
    public bool CanDeleteProject { get; set; }
    public bool CanCreateTasks { get; set; }
    public bool CanEditTasks { get; set; }
    public bool CanDeleteTasks { get; set; }
    public bool CanCompleteTasks { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageRoles { get; set; }

    
    
    
    public bool HasAnyPermission => 
        CanViewProject || CanEditProject || CanDeleteProject ||
        CanCreateTasks || CanEditTasks || CanDeleteTasks || CanCompleteTasks ||
        CanManageUsers || CanManageRoles;
    
    public bool IsReadOnly => CanViewProject && !CanEditProject && !CanEditTasks && !CanCreateTasks;
}




public enum ProjectAction
{
    ViewProject,
    EditProject,
    DeleteProject,
    CreateTask,
    EditTask,
    DeleteTask,
    CompleteTask,
    ManageUsers,
    ManageRoles
}
