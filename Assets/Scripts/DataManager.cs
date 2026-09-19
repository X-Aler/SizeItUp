using System.Collections.Generic;
using UnityEngine;

public class ObjectDatabase : MonoBehaviour
{
    private List<SizeObjectItem> _items = new List<SizeObjectItem>();
    public bool IsLoaded => _items != null && _items.Count > 0;

    private void Awake()
    {
        LoadDatabase();
    }

    public void LoadDatabase()
    {
        // Загружаем текстовый ассет из Resources/objects.json
        TextAsset jsonFile = Resources.Load<TextAsset>("objects");
        if (jsonFile == null)
        {
            Debug.LogError("Не найден файл Resources/objects.json!");
            return;
        }

        // Оборачиваем JSON в контейнер, если он начинается с массива '['
        string rawJson = jsonFile.text.Trim();
        if (rawJson.StartsWith("["))
        {
            rawJson = $"{{\"objects\":{rawJson}}}";
        }

        SizeObjectListWrapper wrapper = JsonUtility.FromJson<SizeObjectListWrapper>(rawJson);
        _items = wrapper.objects;

        Debug.Log($"Успешно загружено объектов из базы: {_items.Count}");
    }

    /// <summary>
    /// Выбирает случайную пару объектов для раунда
    /// </summary>
    public (SizeObjectItem blue, SizeObjectItem red) GetRandomPair()
    {
        if (_items.Count < 2)
        {
            Debug.LogError("В базе меньше двух объектов!");
            return (null, null);
        }

        int blueIndex = Random.Range(0, _items.Count);
        int redIndex;

        // Следим, чтобы красный не выпал тем же самым объектом
        do
        {
            redIndex = Random.Range(0, _items.Count);
        } while (redIndex == blueIndex);

        return (_items[blueIndex], _items[redIndex]);
    }

    /// <summary>
    /// Загружает спрайт из папки Resources/Objects/ по его id
    /// </summary>
    public Sprite LoadSprite(string id)
    {
        Sprite sprite = Resources.Load<Sprite>($"Objects/{id}");
        if (sprite == null)
        {
            Debug.LogWarning($"Не найден спрайт по пути: Resources/Objects/{id}");
        }
        return sprite;
    }
}