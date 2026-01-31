using System.Text.Json;

namespace ClientLibrary.Helpers
{
    public static class Serializations
    {
        //Chuyển một đối tượng C# thành chuỗi Json
        public static string SerializeObj<T>(T modelObject) =>
            JsonSerializer.Serialize(modelObject);

        //Chuyển chuỗi Json thành một đối tượng C#
        public static T DeserializeJsonString<T>(string jsonString) =>
            JsonSerializer.Deserialize<T>(jsonString)!;
        public static IList<T> DeserializeJsonStringList<T>(string jsonString) =>
            JsonSerializer.Deserialize<IList<T>>(jsonString)!;

    }
}
