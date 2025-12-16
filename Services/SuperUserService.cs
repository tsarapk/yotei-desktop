using System;
using System.Collections.Generic;
using System.Linq;
using YoteiLib.Core;

namespace YoteiTasks.Services;




public interface ISuperUserService
{
    
    
    
    Actor GetOrCreateSuperUser();
    
    
    
    
    bool IsSuperUser(Actor? actor);
    
    
    
    
    string? GetSuperUserId();
    
    
    
    
    void SetSuperUser(Actor actor);
}

public class SuperUserService : ISuperUserService
{
    private readonly Yotei _yotei;
    private readonly IUserActorService _userActorService;
    private string? _superUserId;
    private const string SUPERUSER_NAME = "SuperUser";
    private const string SUPERUSER_USERNAME = "admin";
    private const string SUPERUSER_DEFAULT_PASSWORD = "admin";

    public SuperUserService(Yotei yotei, IUserActorService userActorService)
    {
        _yotei = yotei;
        _userActorService = userActorService;
    }

    public Actor GetOrCreateSuperUser()
    {
        
        if (!string.IsNullOrEmpty(_superUserId))
        {
            var existingSuperUser = _yotei.Actors.GetAll()
                .FirstOrDefault(a => a.Id.ToString() == _superUserId);
            
            if (existingSuperUser != null)
                return existingSuperUser;
        }

        
        var superUser = _yotei.Actors.GetAll()
            .FirstOrDefault(a => a.Username == SUPERUSER_USERNAME);

        if (superUser != null)
        {
            _superUserId = superUser.Id.ToString();
            return superUser;
        }

        
        superUser = _yotei.Actors.Create();
        superUser.SetName(SUPERUSER_NAME);
        
        try
        {
            _userActorService.SetActorCredentials(superUser, SUPERUSER_USERNAME, SUPERUSER_DEFAULT_PASSWORD);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при установке учётных данных SuperUser: {ex.Message}");
        }

        _superUserId = superUser.Id.ToString();
        
        return superUser;
    }

    public bool IsSuperUser(Actor? actor)
    {
        if (actor == null)
            return false;

        
        if (string.IsNullOrEmpty(_superUserId))
        {
            GetOrCreateSuperUser();
        }

        return actor.Id.ToString() == _superUserId;
    }

    public string? GetSuperUserId()
    {
        if (string.IsNullOrEmpty(_superUserId))
        {
            GetOrCreateSuperUser();
        }
        
        return _superUserId;
    }

    public void SetSuperUser(Actor actor)
    {
        _superUserId = actor.Id.ToString();
    }
}
