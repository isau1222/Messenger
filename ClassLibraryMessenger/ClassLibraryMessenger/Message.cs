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
        public enum MyMessageMode { NewMessage, LogIn, LogOut, NewImage, NewMusic}

            public string clientName; //имя клиента
            public string text; //его сообщение

            public string[] clients; //список имен клиентов
            public byte[] image; //картиночка. тут используется массив byte[], а не объект класса Image, потому что объект класса Image НЕЛЬЗЯ сериализовать (и ImageSource тоже)! (одна из тонкостей, к которым нужно идти несколько часов)

            public byte[] fileBytes;
            public string fileType;
            public string fileName;

            public MyMessageMode mesMode;

            public Message(MyMessageMode _mesMode, object _myTuple)
            {
                mesMode = _mesMode;

                if (_mesMode == MyMessageMode.NewMessage)
                {
                    Tuple<string, string> UnCodeTuple = (Tuple<string, string>)_myTuple;
                    clientName = UnCodeTuple.Item1;
                    text = UnCodeTuple.Item2;
                }
                else if (_mesMode == MyMessageMode.LogIn)
                {
                    Tuple<string> UnCodeTuple = (Tuple<string>)_myTuple;
                    clientName = UnCodeTuple.Item1;
                }
                else if (_mesMode == MyMessageMode.LogOut)
                {
                    Tuple<string> UnCodeTuple = (Tuple<string>)_myTuple;
                    clientName = UnCodeTuple.Item1;
                }
                else if (_mesMode == MyMessageMode.NewImage)
                {
                    Tuple<string, byte[]> UnCodeTuple = (Tuple<string, byte[]>)_myTuple;
                    clientName = UnCodeTuple.Item1;
                    image = UnCodeTuple.Item2;
                }
                else if (_mesMode == MyMessageMode.NewMusic)
                {
                    Tuple<string, string, byte[], string> UnCodeTuple = (Tuple<string, string, byte[], string>)_myTuple;
                    clientName = UnCodeTuple.Item1;
                    fileType = UnCodeTuple.Item2;
                    fileBytes = UnCodeTuple.Item3;
                    fileName = UnCodeTuple.Item4;
                }
                
            }

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
