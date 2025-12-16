using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.Services;


public interface IUserActorService
{
   
    void SetActorCredentials(Actor actor, string username, string password);
    
    Actor? AuthenticateActor(string username, string password);
    
    User GetOrCreateUser(Actor actor);
    
    bool HasCredentials(Actor actor);
}

public class UserActorService : IUserActorService
{
    private readonly IEnumerable<Actor> _actors;
    
    public UserActorService(IEnumerable<Actor> actors)
    {
        _actors = actors;
    }
    
    public void SetActorCredentials(Actor actor, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));
            
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));
        
        
        var existingActor = _actors
            .FirstOrDefault(a => a.Username == username && a.Id != actor.Id);
            
        if (existingActor != null)
            throw new InvalidOperationException($"Username '{username}' is already taken");
        
        var passwordHash = HashPassword(password);
        actor.SetCredentials(username, passwordHash);
    }
    
    public Actor? AuthenticateActor(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;
        
        var actor = _actors.FirstOrDefault(a => a.Username == username);
        if (actor == null || string.IsNullOrEmpty(actor.PasswordHash))
            return null;
        
        var passwordHash = HashPassword(password);
        return passwordHash == actor.PasswordHash ? actor : null;
    }
    
    public User GetOrCreateUser(Actor actor)
    {
        return new User
        {
            Id = actor.Id.ToString(),
            Username = actor.Username ?? actor.Name,
            PasswordHash = actor.PasswordHash ?? string.Empty,
            RoleIds = new List<string>(),
            IsActive = true
        };
    }
    
    public bool HasCredentials(Actor actor)
    {
        return !string.IsNullOrEmpty(actor.Username) && !string.IsNullOrEmpty(actor.PasswordHash);
    }
    
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
