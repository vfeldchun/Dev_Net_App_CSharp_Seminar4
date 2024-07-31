using System.Text.Json;

namespace Task1
{
    internal class JsonConverter : Converter
    {
        public override string Serialize(Message message)
        {
            return JsonSerializer.Serialize(message); ;
        }
        public override Message? Deserialize(string jsonString)
        {
            return JsonSerializer.Deserialize<Message>(jsonString); 
        }
    }
}
