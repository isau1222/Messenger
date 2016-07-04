using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ClassLibraryMessenger;

namespace ConsoleServer
{
    //это вторая глава. третья - Message (из библиотеки)
    public class ClientObject
    {
        public TcpClient client;
        public NetworkStream stream;

        List<ClientObject> clientObjects;
        string clientName; //у каждого клиента есть имя (то же, что и когда авторизовываемся)

        public ClientObject(TcpClient tcpClient, List<ClientObject> _clientObjects) //получаем с кем работать и инфу о всех clientObjects
        {
            client = tcpClient;
            clientObjects = _clientObjects;
            stream = client.GetStream();
            clientName = ""; 
        }

        public void Process()
        {
            try
            {
                while (true) //запускаем бесконечный цикл отлова сообщений от нашего клиента
                {
                    BinaryFormatter outFormatter = new BinaryFormatter();
                    Message msg = (Message)outFormatter.Deserialize(stream); //эти 2 строки просто сказка.
                    //они просто получаеют на вход стрим, и сами же извлекают из него байты
                    //в итоге msg - готовый объект, полученный с клиента. и теперь мы крутим этот msg как хотим, в нём много инфы о клиенте

                    string message = msg.text; //получаем, что клиент написал
                    clientName = msg.clientName; //получаем имя клиента

                    if (msg.firstVisit == true) //получаем, авторизировался ли клиент только что
                    {
                        HeJoinedUs(msg); //говорим всем (и даже самому клиенту), что клиент, отправивший объект msg только что зашел
                    }
                    else
                    { //firstVisit == false => он не только что зашел, а отправил объект msg, потому что хочет отправить что-то
                        if (msg.image != null) //если есть картинка (msg.image - массив байтов), значит клиент отправил картинку
                        {
                            Console.WriteLine(msg.clientName + ": прислал фото");
                        }
                        else if (msg.fileType != null)
                        {
                            Console.WriteLine(msg.clientName + ": прислал файл");
                        }
                        else
                        {
                            Console.WriteLine(msg.clientName + ": " + msg.text);
                        }

                        for (int i = 0; i < clientObjects.Count; i++)
                        {
                            if (clientObjects[i]!=this)
                                clientObjects[i].SendMessage(clientObjects[i].stream, msg); //говорим всем нашим clientObjects, чтобы они оповестили своих клиентов о том, что клиент что-то написал
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                HeLeftUs(); //если ошибка, то думаем, что клиент отрубился и говорим всем об этом
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        //void SendMessage(NetworkStream _stream, string message) //раньше я слал стринги
        //{
        //    byte[] data = new byte[256];
        //    data = Encoding.Unicode.GetBytes(message);
        //    _stream.Write(data, 0, data.Length);
        //}

        void SendMessage(NetworkStream _stream, Message message) //а теперь шлю объекты типа Message (Message : Object)
        {
            BinaryFormatter answFormatter = new BinaryFormatter();
            answFormatter.Serialize(_stream, (object)message); //в коде сверху мы доставали объект из байтов, теперь делаем наоборот - разбиваем объект и отсылаем его в _stream 
            //(это в цикле, так что каждый clientObject отсылает эти байты своему клиенту (_stream = clientObjects[i].stream)
        }

        void HeJoinedUs(Message msg)
        {
            Console.WriteLine("Зашёл " + msg.clientName);

            string[] clients = new string[clientObjects.Count];
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = clientObjects[i].clientName;
            }
            msg.clients = clients; //передаём клиенту (этот msg уйдёт обратно клиенту, проверь еще раз код, если не заметил) имена всех клиентов, находящихся на сервере

            for (int i = 0; i < clientObjects.Count; i++)
            {
                clientObjects[i].SendMessage(clientObjects[i].stream, msg); //отправляем всем клиентам, что этот ушел
            }
        }

        void HeLeftUs()
        {
            //тут уже нужно новое сообщение, тк связь с клиентом потерена => от него и не было никаких сообщений
            Message msg = new Message(clientName, true); //конструктор, создающий сообщение говорящее о том, что клиент ушел (clientName мы узнали, когда он зашел, так что можем всем сказать, как его звали)
            clientObjects.Remove(this);//обязательно удаляем этого clientObject из списка clientObjects, чтобы сервер к нему не обращался (ведь клиента больше нет)
            Console.WriteLine("Нас покинул " + clientName);

            string[] clients = new string[clientObjects.Count];
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = clientObjects[i].clientName;
            }
            msg.clients = clients; //-|-|-|-|-

            for (int i = 0; i < clientObjects.Count; i++)
            {
                clientObjects[i].SendMessage(clientObjects[i].stream, msg); //отправляем всем клиентам, что этот ушел
            }
        }
    }
}
