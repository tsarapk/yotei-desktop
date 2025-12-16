using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using YoteiLib.Core;
using YoteiTasks.Adapters;
using YoteiTasks.Models;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Services;


public class JsonSaveService : ISaveService
{
    private readonly string _saveFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSaveService(string? saveFilePath = null)
    {
        Console.WriteLine(GetDefaultSavePath());
        _saveFilePath =  GetDefaultSavePath();
    }

    private static string GetDefaultSavePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "YoteiTasks");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "save.json");
    }

    public void Save(SaveData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
        }
    }

    public SaveData? Load()
    {
        try
        {
            if (!File.Exists(_saveFilePath))
                return null;

            var json = File.ReadAllText(_saveFilePath);
            return JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            return null;
        }
    }
}








