using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ConsoleServer
{
    //привет Андрей. это первая глава. вторая глава - ClientObject
    class Program
    {
        //const string ip = "192.168.1.19";//doma
        const string ip = "128.204.40.151";//andrew
        //const string ip = "192.168.3.8";//yula
        //const string ip = "10.210.51.4";//uni
        //const string ip = "192.168.70.1";
        const int port = 8080;
        static TcpListener listener;
        static void Main(string[] args)
        {
            StartListener:
            List<ClientObject> clientObjects = new List<ClientObject>(); //массив наших клиентов. каждый clientObject имеет к нему доступ
            Console.WriteLine(GetMyIp().ToString()); //просто смотрю, что даёт эта функция
            try
            {
                listener = new TcpListener(IPAddress.Parse(ip), port);
                listener.Start(); //создаём слушателя
                Console.WriteLine("Мои IP: " + ip + ":" + port);
                Console.WriteLine("Ожидание подключений...");

                while (true)
                {
                    //нужно понимать, что clientObject - это не клент, с которым мы работаем, 
                    //а объект, который обслуживает клиента, с которым мы работаем
                    TcpClient client = listener.AcceptTcpClient(); //клиент нашелся
                    ClientObject clientObject = new ClientObject(client, clientObjects);//создаем clientObject (посылаем ему инфу с каким клиентом он работает 
                    //и список clientObject, чтобы он мог находить других клиентов и общаться с ними тоже)
                    clientObjects.Add(clientObject); //добавляем его в наш список (хорошо, что это объект и все clientObject имеют свежую инфу об этом листе)

                    // создаем новый поток для обслуживания нового клиента
                    Thread clientThread = new Thread(clientObject.Process);//говорим clientObject, чтобы обслуживал клиента (в отдельном потоке, ведь clientObject не один)
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                goto StartListener;
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }

        private static IPAddress GetMyIp()
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName()); //мб чекнуть, если ретернит локальный ип 
            return ipHostInfo.AddressList[0];
        }
    }
}