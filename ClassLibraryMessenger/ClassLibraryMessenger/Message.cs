using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//библиотека. сделано так, чтобы 2 разных проекта имели её у себя под крылом и обращались к ней.
namespace ClassLibraryMessenger
{
    //это третья глава. четвертая - Chat.Xaml.cs (ClientMessenger)
    [Serializable()] //обязательно. нужно для работы сериализации. я про эти штуки:
        //BinaryFormatter outFormatter = new BinaryFormatter();
        //Message msg = (Message)outFormatter.Deserialize(stream);
        //BinaryFormatter answFormatter = new BinaryFormatter();
        //answFormatter.Serialize(_stream, (object)message); 

    public class Message : Object
    {
            public string clientName; //имя клиента
            public string text; //его сообщение
            public bool firstVisit; //зашел ли он впервые
            public bool gotOut; //ушёл ли он
            public string[] clients; //список имен клиентов
            public byte[] image; //картиночка. тут используется массив byte[], а не объект класса Image, потому что объект класса Image НЕЛЬЗЯ сериализовать (и ImageSource тоже)! (одна из тонкостей, к которым нужно идти несколько часов)
        //у меня здесь много разных конструкторов.
            public Message(string _clientName, string _text) //стандартный. вызывается приложением ClientMessenger, чтобы передать серверу, что клиент просто отправляет сообщение
            {
                //поэтому всё null кроме имени и текста сообщения
                clientName = _clientName;
                text = _text;
                firstVisit = false;
                gotOut = false;
                clients = new string[0];
                image = null;
            }

            public Message(string _clientName, string _text, bool _firstVisit)//вызывается приложением ClientMessenger, чтобы передать серверу, что клиент только что зашёл
            {
                //(здесь так же присутствует параметр _text, но он здесь как заглушка, потому что без него этот конструктор будет выглядит как следующий (конструктор выхода)
                //(_firstVisit должен быть тру!)
                clientName = _clientName;
                text = "";
                firstVisit = _firstVisit;
                gotOut = false;
                clients = new string[0];
                image = null;
            }

            public Message(string _clientName, bool _gotOut)//вызывается приложением ClientMessenger, чтобы передать серверу, что клиент ушёл (а может и вылетел - без разницы)
            {
                //поэтому здесь только имя да булевское gotOut (оно должно быть тру!)
                clientName = _clientName;
                text = "";
                firstVisit = false;
                gotOut = _gotOut;
                clients = new string[0];
                image = null;
            }

            public Message(string _clientName, byte[] _image)//вызывается приложением ClientMessenger, чтобы передать серверу, что клиент отправил картинку
            {
                //только имя, да картинка
                clientName = _clientName;
                text = "";
                firstVisit = false;
                gotOut = false;
                clients = new string[0];
                image = _image;
            }        

            //public static byte[] ObjectToByteArray(object obj)  //это я использовал до тех крутых двух строчек
            //{
            //    if (obj == null)
            //        return null;
            //    BinaryFormatter bf = new BinaryFormatter();
            //    using (MemoryStream ms = new MemoryStream())
            //    {
            //        bf.Serialize(ms, obj);
            //        return ms.ToArray();
            //    }
            //}

            //public static Object ByteArrayToObject(byte[] arrBytes)
            //{
            //    MemoryStream memStream = new MemoryStream();
            //    BinaryFormatter binForm = new BinaryFormatter();
            //    memStream.Write(arrBytes, 0, arrBytes.Length);
            //    memStream.Seek(0, SeekOrigin.Begin);
            //    Object obj = (Object)binForm.Deserialize(memStream);

            //    return obj;
            //}

            public static byte[] ImageSourceToBytes(BitmapEncoder encoder, BitmapSource imageSource) //разбивает ImageSource (содержимое элемента Image на байты). найти правильный алгоритм было очень трудно, кучу раз байты пересчитывал и вообще пиздос бля
            {
                byte[] bytes = null;
                if (imageSource != null)
                {
                    encoder.Frames.Add(BitmapFrame.Create(imageSource));

                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        bytes = stream.ToArray();
                    }
                }

                return bytes;
            }//это статические метод, его вызывает слушатель ClientMessanger, отправляющий сообщения. он вызывает эту штуку, чтобы разбить свою картинку на байты и отправить на сервер

            public static BitmapImage BytesToImageSource(byte[] bytes) //собирает ImageSource. найти правильный алгоритм было ЕЩЕ ТРУДНЕЕ, кучу раз байты пересчитывал и вообще пиздос бля
            {
                MemoryStream stream = new MemoryStream(bytes);
                BitmapImage imgSource = new BitmapImage();
                imgSource.BeginInit();
                imgSource.StreamSource = stream;
                imgSource.EndInit();

                return imgSource;
            }//это статические метод, его вызывает слушатель ClientMessanger (GetterMessages), принимающий сообщения. он вызывает эту штуку, если принял массив байтов image
        }
}
