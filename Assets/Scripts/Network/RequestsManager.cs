using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Prog2step.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class RequestsManager
{
    
    public Container GetJsonContainer()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
        Container container = JsonConvert.DeserializeObject<Container>(File.ReadAllText(saveFilePath));
        return container;
    }

    
    public async Task CreatePlayer(string name, List<object> res)
    {
        Debug.Log("Start creating player");
        var gameId = GetUUID();

        var apiUrl = $"https://2025.nti-gamedev.ru/api/games/{gameId}/players/";

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

        if (res == null) res = new List<object>();
        var requestBody = new
        {
            name = name, 
            resources = res
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Debug.Log($"Failed to create player. Status Code: {response.StatusCode}. Error: {error}");
        }
        await CreateLog($"Регистрация игрока: {name}", name, res);
        Debug.Log("Player created successfully.");
    }
    public async Task GetShops()
    {
        var res = new AllShopsData();
        var client = new HttpClient();
        var gameId = GetUUID();

        var apiUrl = $"https://2025.nti-gamedev.ru/api/games/{gameId}/players/{GetJsonContainer().Name}/shops";

        try
        {
            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log(apiUrl);
                Debug.Log($"Failed to fetch shops/// code: {response.StatusCode}");
                GameManager.Instance.shopsAcitve = false;
            }
            else
            {
                GameManager.Instance.shopsAcitve = true;
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            res = JsonConvert.DeserializeObject<AllShopsData>(responseBody);
            GameManager.Instance.shopConts = res.Resources;
            await CreateLog($"Попытка получения данных магазинов", "shops", new List<object>());
        }
        catch (Exception ex)
        {
            //ignore
        }
        
        string saveFilePath = Path.Combine(Application.persistentDataPath, "shops.json");
        string jsonOut = JsonConvert.SerializeObject(res, Formatting.Indented);
        await File.WriteAllTextAsync(saveFilePath,jsonOut);
        res = JsonConvert.DeserializeObject<AllShopsData>(File.ReadAllText(saveFilePath));
        await File.WriteAllTextAsync(saveFilePath,jsonOut);
        res = JsonConvert.DeserializeObject<AllShopsData>(File.ReadAllText(saveFilePath));
        GameManager.Instance.shopConts = res.Resources;
        Debug.Log(jsonOut);
        //return res.Resources;
    }
    
    public async Task CreateShop()
    {
        Debug.Log("Start creating player");
        var gameId = GetUUID();

        AllShopsData sh = new AllShopsData()
        {
            Resources = new List<ShopContainer>()
            {
                new ShopContainer()
                {
                    Name = "Radio",
                    Resources = new List<ResourceToSave>()
                    {
                        new ResourceToSave(){Count = 5, Name = "Plan_Engine"},
                        new ResourceToSave(){Count = 5, Name = "Plan_Wings"},
                        new ResourceToSave(){Count = 5, Name = "Plan_Body"},
                        new ResourceToSave(){Count = 5, Name = "Plan_FuelTank"},
                        new ResourceToSave(){Count = 5, Name = "Plan_ControlPanel"}
                    }
                },
                new ShopContainer()
                {
                    Name = "Rocket",
                    Resources = new List<ResourceToSave>()
                    {
                        new ResourceToSave(){Count = 5, Name = "Wood"},
                        new ResourceToSave(){Count = 5, Name = "Iron"},
                        new ResourceToSave(){Count = 5, Name = "Stone"},
                        new ResourceToSave(){Count = 5, Name = "Coal"},
                        new ResourceToSave(){Count = 5, Name = "Copper"}
                    }
                }
            }
        };
        string saveFilePath = Path.Combine(Application.persistentDataPath, "shop.json");
        string jsonOut = JsonConvert.SerializeObject(sh, Formatting.Indented);
        await File.WriteAllTextAsync(saveFilePath,jsonOut);
        GameManager.Instance.shopConts = sh.Resources;
        Debug.Log(jsonOut);

        var apiUrl = $"https://2025.nti-gamedev.ru/api/games/{gameId}/players/{GetJsonContainer().Name}/shops/";
        Debug.Log(apiUrl);

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        
        var requestBody = new
        {
            name = "Shops", 
            resources = sh.Resources
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log(jsonBody);

        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Debug.Log($"Failed to create shop. Status Code: {response.StatusCode}. Error: {error}");
        }
        await CreateLog($"Регистрация магазина ", "Shops", new List<object>());
        Debug.Log("Shop created successfully.");
    }

    public async Task<Container> GetResourcesForPlayer(string name)
    {
        var res = new Container();
        var client = new HttpClient();
        var gameId = GetUUID();

        var apiUrl = $"https://2025.nti-gamedev.ru/api/games/{gameId}/players/{name}/";

        try
        {
            
            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log($"Failed to fetch players. Status code: {response.StatusCode}");
                try
                {
                    Debug.Log("Trying to create player");
                    await CreatePlayer(name, null);
                }
                catch (Exception e)
                {
                    // ignored
                }
                
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            res = JsonConvert.DeserializeObject<Container>(responseBody);
            res.Name = name;
            await CreateLog($"Попытка входа в аккаунт {name}", res.Name, res.Resources);
        }
        catch (Exception ex)
        {
            res.Name = name;
            res.Resources = new List<object>();
        }

        string saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
        string jsonOut = JsonConvert.SerializeObject(res, Formatting.Indented);
        await File.WriteAllTextAsync(saveFilePath,jsonOut);
        Debug.Log(jsonOut);
        return res;
    }

    public async Task UpdateResources()
    {
        Container res = GetJsonContainer();
        var gameId = GetUUID();
        string name = res.Name;

        var apiUrl = $"https://2025.nti-gamedev.ru/api/games/{gameId}/players/{name}/";

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Put, apiUrl);

        var requestBody = JsonConvert.SerializeObject(res);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                Debug.Log(
                    $"Failed to update resources. Status code: {response.StatusCode}, Error: {errorResponse}");
                await CreatePlayer(name, res.Resources);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error updating resources for player {name}: {ex.Message}");
            await CreatePlayer(name, res.Resources);
        }
    }
    
    public async Task CreateLog(string comment, string name, List<object> resourcesChanged)
    {
        var gameId = GetUUID();

        var apiUrl = $"https://2025.nti-gamedev.ru/api/games/{gameId}/logs/";

        var logEntry = new
        {
            comment = comment,
            player_name = name,
            resources_changed = resourcesChanged
        };

        string jsonBody = JsonConvert.SerializeObject(logEntry);

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating log: {ex.Message}");
        }
    }
    
   
    private string GetUUID()
    {
        return "7317031d-266a-41b0-a311-c16c6e3b974a";
    }
    
}