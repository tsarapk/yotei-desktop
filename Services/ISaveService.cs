using YoteiTasks.Models;

namespace YoteiTasks.Services;


public interface ISaveService
{
 
    void Save(SaveData data);


    SaveData? Load();
}








