using System;
using System.Collections.Generic;

[Serializable]
public class SizeObjectItem
{
    public string id;
    public string name_ru;
    public string name_en;
    public double size_meters;
    public string dimension_type;
    public string category;
}

// Вспомогательный класс-обертка, так как JsonUtility в Unity не парсит массивы верхнего уровня напрямую
[Serializable]
public class SizeObjectListWrapper
{
    public List<SizeObjectItem> objects;
}