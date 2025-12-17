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
            Console.WriteLine($"[JsonSaveService] Данные успешно сохранены в: {_saveFilePath}");
            
            // Log resource usages for debugging
            int totalResourceUsages = 0;
            foreach (var graph in data.Graphs)
            {
                foreach (var node in graph.Nodes)
                {
                    totalResourceUsages += node.ResourceUsages?.Count ?? 0;
                }
            }
            Console.WriteLine($"[JsonSaveService] Сохранено использований ресурсов: {totalResourceUsages}");
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
            {
                Console.WriteLine($"[JsonSaveService] Файл сохранения не найден: {_saveFilePath}");
                return null;
            }

            var json = File.ReadAllText(_saveFilePath);
            var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
            
            if (data != null)
            {
                Console.WriteLine($"[JsonSaveService] Данные успешно загружены из: {_saveFilePath}");
                
                // Log resource usages for debugging
                int totalResourceUsages = 0;
                foreach (var graph in data.Graphs)
                {
                    foreach (var node in graph.Nodes)
                    {
                        totalResourceUsages += node.ResourceUsages?.Count ?? 0;
                    }
                }
                Console.WriteLine($"[JsonSaveService] Загружено использований ресурсов: {totalResourceUsages}");
            }
            
            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }
}








