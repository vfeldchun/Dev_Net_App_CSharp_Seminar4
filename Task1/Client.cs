using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Task1
{
    internal static class Client
    {
        static private CancellationTokenSource cts = new CancellationTokenSource();
        static private CancellationToken ct;

        private static async Task UdpClientRecieverAsync(UdpClient udpClient)
        {
            while (ct.IsCancellationRequested != true)
            {
                try
                {
                    var receiveResult = await udpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(receiveResult.Buffer);                    

                    if (message == "XML" || message == "JSON")
                    {
                        GlobalVariables.SerializingFormat = message;
                        GlobalVariables.IsExchangeFormatSync = true;
                    }
                    else
                    {
                        Converter converter;

                        if (GlobalVariables.SerializingFormat == "XML")
                            converter = new XmlConverter();
                        else
                            converter = new JsonConverter();

                        Message? newMessage = converter.Deserialize(message);

                        if (newMessage?.MessageText == GlobalVariables.SERVER_SHUTDOWN_MESSAGE)
                        {
                            Console.WriteLine(newMessage);
                            cts.Cancel();
                        }                        
                        else
                        {
                            Console.WriteLine(newMessage);
                            Console.WriteLine(GlobalVariables.CLIENT_INPUT_MESSAGE);
                        }
                    }
                    
                                        
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }


        }

        private static async Task SendMessageAsync(UdpClient udpClient, Message message, Converter converter, IPEndPoint udpServerEndPoint)
        {            
            string newMsg = converter.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(newMsg);
            await udpClient.SendAsync(bytes, udpServerEndPoint);
        }

        public static async Task UdpSenderAsync(string name)
        {
            IPEndPoint udpServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), GlobalVariables.SERVER_RECEIVER_PORT);
            UdpClient udpClient = new UdpClient(GlobalVariables.CLIENT_UDP_CLIENT_PORT);
            
            ct = cts.Token;

            // Запускаем локальный получатель сообщений клиента
            new Task(async () => { await UdpClientRecieverAsync(udpClient); }).Start();

            // Запрос типа сериализации у сервера            
            byte[] jsonExchangeMsgBytes = Encoding.UTF8.GetBytes(GlobalVariables.CLIENT_REQUEST_MESSAGE_FORMAT);
            await udpClient.SendAsync(jsonExchangeMsgBytes, udpServerEndPoint);
            
            while (!GlobalVariables.IsExchangeFormatSync) { }
            Console.WriteLine($"Формат сериализации при обменен с сервером {GlobalVariables.SerializingFormat}");

            // Factory method
            Converter converter;
            if (GlobalVariables.SerializingFormat == "XML")
                converter = new XmlConverter();
            else
                converter = new JsonConverter();


            while (ct.IsCancellationRequested != true)
            {
                Console.WriteLine(GlobalVariables.CLIENT_INPUT_MESSAGE);
                string? messageText = Console.ReadLine();

                if (messageText?.ToLower() == GlobalVariables.CLIENT_EXIT_COMMAND)
                {
                    cts.Cancel();
                    Message exitMessage = new Message(name, messageText);
                    await SendMessageAsync(udpClient, exitMessage, converter, udpServerEndPoint);
                }
                else
                {
                    Message newMessage = new Message(name, messageText!);
                    await SendMessageAsync(udpClient, newMessage, converter, udpServerEndPoint);
                }                                
            }
        }
    }
}
