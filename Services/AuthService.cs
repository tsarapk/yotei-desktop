using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YoteiTasks.Models;

namespace YoteiTasks.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        void Logout();
        bool IsUserInRole(string roleName);
        bool HasPermission(Permission permission);
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
    }

    public class AuthService : IAuthService
    {
        private readonly ISaveService _saveService;
        private User? _currentUser;
        private List<Role> _roles = new();

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public AuthService(ISaveService saveService)
        {
            _saveService = saveService;
            
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            // TODO: Implement actual user lookup and password hashing
            
            if (username == "admin" && password == "admin")
            {
                _currentUser = new User
                {
                    Username = "admin",
                    RoleIds = new List<string> { "admin-role-id" },
                    IsActive = true
                };

                
                if (!_roles.Any(r => r.Id == "admin-role-id"))
                {
                    _roles.Add(new Role
                    {
                        Id = "admin-role-id",
                        Name = "Administrator",
                        Permissions = Enum.GetValues<Permission>().ToList()
                    });
                }

                return true;
            }
            return false;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public bool IsUserInRole(string roleName)
        {
            if (_currentUser == null) return false;
            
            var userRoles = _roles.Where(r => _currentUser.RoleIds.Contains(r.Id));
            return userRoles.Any(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasPermission(Permission permission)
        {
            if (_currentUser == null) return false;
            
            var userRoles = _roles.Where(r => _currentUser.RoleIds.Contains(r.Id));
            return userRoles.Any(r => r.Permissions.Contains(permission));
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
