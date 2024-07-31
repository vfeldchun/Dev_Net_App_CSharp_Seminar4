using System.Xml.Serialization;

namespace Task1
{
    public class Message : ICloneable
    {
        [XmlElement]
        public string SenderName { get; set; }
        [XmlElement]
        public string? RecipientName { get; set; }
        [XmlElement]
        public string? MessageText { get; set; }
        [XmlElement]
        public DateTime MessageTime { get; set; }

        // Вводим конструктор без параметров для XML сериализации
        public Message() 
        {}

        public Message(string senderName, string messageText, string recipientName = "")
        {
            SenderName = senderName;            
            MessageTime = DateTime.Now;

            if (messageText.Split(':').Length > 1)
            {                
                RecipientName = messageText.Split(":")[0].Trim();
                MessageText = messageText.Split(":")[1].Trim();
            }
            else
            {
                MessageText = messageText;
                RecipientName = recipientName;
            }                
        }       

        public override string ToString()
        {
            return $"От: {SenderName} ({MessageTime.ToString("HH:mm:ss")}): {MessageText}";
        }

        public object Clone()
        {
            var newMessage = new Message(this.SenderName, this.MessageText!, this.RecipientName!);
            newMessage.MessageTime = this.MessageTime;
            return newMessage;
        }
    }
}
