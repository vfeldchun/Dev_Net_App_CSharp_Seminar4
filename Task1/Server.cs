using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Task1
{
    internal static class Server
    {
        private static CancellationTokenSource cts = new CancellationTokenSource();
        private static CancellationToken ct;
        private static Dictionary<string, IPEndPoint> _userDict = new Dictionary<string, IPEndPoint>();

        private static async Task SendMessageAsync(UdpClient udpClient, IPEndPoint remoteEndPoint, Message message)
        {
            string resultString = "";

            if (GlobalVariables.SerializingFormat == "XML")
            {
                Converter converter = new XmlConverter();
                resultString = converter.Serialize(message);
            }
            else
            {
                Converter converter = new JsonConverter();
                resultString = converter.Serialize(message);
            }
            // string jsonMsg = message.GetJson();
            
            byte[] respondBytes = Encoding.UTF8.GetBytes(resultString);
            await udpClient.SendAsync(respondBytes, remoteEndPoint);            
        }

        public static async Task UdpRecieverAsync()
        {
            IPEndPoint receiverEndPoint = new IPEndPoint(IPAddress.Any, 12345);
            UdpClient udpClient = new UdpClient(12345);           

            Console.WriteLine(GlobalVariables.SERVER_START_MESSAGE);

            ct = cts.Token;

            new Task(() =>
            {
                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)                           
                        break;
                }

                // Отправка сообщения о завершении работы в консоль сервера                         
                Message escapeMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_ESC_MESSAGE);
                Console.WriteLine("x" + escapeMessage);
                Environment.Exit(0);                
            }).Start();               

            while (ct.IsCancellationRequested != true)
            {
                try
                {
                    // byte[] bytes = udpClient.Receive(ref receiverEndPoint);
                    var receiveResult = await udpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(receiveResult.Buffer);
                    if (message == GlobalVariables.CLIENT_REQUEST_MESSAGE_FORMAT)
                    {
                        byte[] exchangeMethodBytes = Encoding.UTF8.GetBytes(GlobalVariables.SerializingFormat);
                        await udpClient.SendAsync(exchangeMethodBytes, receiveResult.RemoteEndPoint);
                    }
                        
                    else
                    {
                        await Task.Run(async () =>
                        {
                            // Message? newMessage = Message.GetMessage(message);
                            Message? newMessage;

                            // Factory method
                            if (GlobalVariables.SerializingFormat == "XML")
                            {
                                Converter converter = new XmlConverter();
                                newMessage = converter.Deserialize(message);
                            }
                            else
                            {
                                Converter converter = new JsonConverter();
                                newMessage = converter.Deserialize(message);
                            }

                            if (newMessage?.MessageText?.ToLower() == GlobalVariables.USER_SERVER_SHUTDOWN_COMMAND)
                            {
                                cts.Cancel();

                                // Отправка подтверждения получения сообщения завершения работы сервера
                                Message acceptMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_SHUTDOWN_MESSAGE);
                                await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, acceptMessage);
                                Console.WriteLine(acceptMessage);
                                Thread.Sleep(500);
                            }
                            else
                            {
                                if (newMessage != null)
                                {
                                    if (newMessage.MessageText!.ToLower().Contains(GlobalVariables.USER_REGISTER_COMMAND))
                                    {
                                        if (!_userDict.ContainsKey(newMessage.SenderName))
                                        {
                                            _userDict[newMessage.SenderName] = receiveResult.RemoteEndPoint;
                                            await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {newMessage.SenderName} зарегестрирован!"));
                                        }
                                        else
                                            await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {newMessage.SenderName} уже зарегестрирован!"));
                                    }
                                    else if (newMessage.MessageText.ToLower().Contains(GlobalVariables.USER_UNREGISTER_COMMAND))
                                    {
                                        if (_userDict.ContainsKey(newMessage.SenderName))
                                        {
                                            _userDict.Remove(newMessage.SenderName);
                                            await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, new Message(GlobalVariables.SERVER_NAME, $"Регистрация пользователя {newMessage.SenderName} отменена!\n" +
                                                $"Вы можете отправлять сообщения другим пользователям!"));
                                        }
                                        else
                                            await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {newMessage.SenderName} не зарегистрирован!\n" +
                                                $"Вы больше не можете отправлять сообщения другим пользователям"));
                                    }
                                    else if (newMessage.MessageText.ToLower().Contains(GlobalVariables.USER_LIST_COMMAND))
                                    {
                                        string userList = "[ ";
                                        foreach (var key in _userDict.Keys)
                                            userList += key + " ";
                                        userList += "]";

                                        await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, new Message(GlobalVariables.SERVER_NAME, $"Список зарегестрированных пользователей\n{userList}"));
                                    }
                                    else
                                    {
                                        if (newMessage.RecipientName != "" && _userDict.ContainsKey(newMessage.RecipientName!))
                                        {
                                            IPEndPoint recipientEndPoint = _userDict[newMessage.RecipientName!];
                                            await SendMessageAsync(udpClient, recipientEndPoint, newMessage);
                                        }
                                        else
                                        {

                                            foreach (var item in _userDict)
                                            {
                                                // Шаблон Prototype
                                                var cloneMessage = newMessage.Clone() as Message;
                                                cloneMessage!.RecipientName = item.Key;
                                                await SendMessageAsync(udpClient, item.Value, cloneMessage);
                                            }
                                        }
                                    }

                                    Console.WriteLine(newMessage);

                                    // Отправка подтверждения получения сообщения
                                    Message acceptMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_ACCEPTED_MESSAGE);
                                    await SendMessageAsync(udpClient, receiveResult.RemoteEndPoint, acceptMessage);
                                }
                                else
                                    Console.WriteLine(GlobalVariables.SERVER_COMMON_ERROR_MESSAGE);
                            }
                        });
                    }                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


            }
        }
    }
}
