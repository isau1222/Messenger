using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using ClassLibraryMessenger;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Globalization;
using System.Diagnostics;
using System.Media;

namespace ClientMessenger
{
    //это пятая глава. шестая (конец) - MessageControl
    class GetterMessages
    {
        public static int IDFile = 0;
        NetworkStream stream;
        TextBox clientsBox;
        TcpClient client;

        Chat chat;
        string clientName;

        //передаём нашему объекту информацию о том, кто является TcpClient клиентом, передаём стрим, передаём контрол клиетБокс (там пишется кто сейчас на сервере)
        //передаём наше главное окно (Chat), передаём имя клиента (введенное при авторизации)
        public GetterMessages(TcpClient _client, NetworkStream _stream, TextBox _clientsBox, Chat _chat, string _clientName)
        {
            stream = _stream;
            client = _client;
            clientsBox = _clientsBox;

            chat = _chat;
            clientName = _clientName;

            //это всё нужно, чтобы с ними работать
        }

        public void GetMessage()//запускается отдельным потоком из главного окна
        {
            try
            {
                while (true) //бесконечно слушаем
                {
                    BinaryFormatter outFormatter = new BinaryFormatter();
                    Message msg = (Message)outFormatter.Deserialize(stream); //ловим массив байтов и превращаем его в объект Message

                    if (msg.firstVisit == true)//узнаем, что клиент только что зашел
                    {
                        //тут начинается жесть. фундаментальная инфа: чтобы изменять объекты (или еще что-то) чужого потока, нужны делегаты
                        //нельзя просто так взять и сказать: эй, главное окно, я хочу изменить текст в твоём textSend. нужно вклиниваться туда
                        //именно таким способом:
                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            //MessageControl.CreateText(), напомню, нужен для "серверных" сообщений (ошибки, уведомления)
                            TextBox message = MessageControl.CreateText(); //И ЭТОТ ТЕКСТБОКС ТОЖЕ НАДО СОЗДАВАТЬ ВНУТРИ СКОБОК
                            message.Text = "К нам присоединился " + msg.clientName;
                            message.HorizontalAlignment = HorizontalAlignment.Center;
                            chat.panelPole.Children.Add(message); //добавляем этот месседж на панель сообщений
                        }));
                        //то есть всё внутри этих скобок уже сработает.
                        //!!!!! нельзя просто так взять и изменить свойства объекта из чужого потока !!!!!//
                        clientsBox.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            clientsBox.Text = "Сейчас на сервере:\n";
                            foreach (string name in msg.clients)//пробегаемся по полученной инфе об именах присутствующих клиентов
                            {
                                clientsBox.Text += name + "\n"; //выводим их всех
                            }
                        }));

                    }
                    else if (msg.gotOut)//опаньки. кто-то ушел
                    {
                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            TextBox message = MessageControl.CreateText(); //опять создаём MessageControl.CreateText(), т.к. это уведомление, а не сообщение другого клиента
                            message.Text = "От нас ушёл " + msg.clientName;
                            message.HorizontalAlignment = HorizontalAlignment.Center;
                            chat.panelPole.Children.Add(message);
                        }));

                        clientsBox.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            clientsBox.Text = "Сейчас на сервере:\n";
                            foreach (string name in msg.clients)
                            {
                                clientsBox.Text += name + "\n";
                            }
                        }));

                        PlayMessSound();
                    }
                    else if (msg.image != null) //если мы получили НЕ пустой массив байтов image
                    {//то есть это условие нам гарантирует, что мы не увидим нашу картинку еще раз
                        BitmapImage bitImg = Message.BytesToImageSource(msg.image); //конвертируем массив байтов в BitmapImage (напомню, что BitmapImage, ImageSource, BitmapSource супер схожие вещи)
                        //то есть мы получаем ImageSource, а не контрол Image, потому что ImageSource - это картинка, находящаяся внитри Image. именно она то нам и нужна

                        bitImg.Freeze(); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1 ОБЯЗАТЕЛЬНО! иначе будет ошибка (еще одна мелочь, к которой я шёл часов 7)

                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            Border messageText = MessageControl.CreateUserText(msg.clientName + " прислал фото:");
                            Image img = MessageControl.CreateImage(bitImg);//создаем контрол img, содержимым которого становится bitImg (см. MessageControl.CreateImage(), там все свойства устанавливаются)

                            chat.panelPole.Children.Add(messageText);//показываем уведомление "имя + прислал фото"
                            chat.panelPole.Children.Add(img);//показываем контрол Image, внутри которого принятый нами массив байтов msg.image, преобразованный в ImageSource (BitmapImage)
                        }));
                        PlayMessSound();
                    }
                    else if (msg.fileType != null)
                    {
                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            if (msg.fileType == ".wav" || msg.fileType == ".mp3")
                            {
                                string directoryName = "Sounds";
                                CheckName(msg, directoryName);

                                MyBorder border = MessageControl.CreateUserText(msg.clientName + " отправил файл " + msg.fileName);

                                border.myText = directoryName + "\\" + msg.fileName;

                                Thickness padding = border.Padding;
                                padding.Right = 25;
                                border.Padding = padding;

                                using (FileStream fs = new FileStream(directoryName + "\\" + msg.fileName, FileMode.Create))
                                {
                                    fs.Write(msg.fileBytes, 0, msg.fileBytes.Length);
                                    fs.Close();
                                    if (msg.fileType == ".wav" || msg.fileType == ".mp3")
                                    {
                                        border.music = new MediaPlayer();
                                        border.music.Open(new Uri(border.myText, UriKind.Relative));

                                        border.MouseDown += chat.border_MouseDown;
                                        chat.player = border.music;
                                        chat.soundBorders.Add(border);
                                    }
                                }
                                chat.panelPole.Children.Add(border);
                            }
                            else if (msg.fileType == ".gif")
                            {
                                string directoryName = "Gifes";
                                CheckName(msg, directoryName);
                                using (FileStream fs = new FileStream(directoryName + "\\" + msg.fileName, FileMode.Create))
                                {
                                    fs.Write(msg.fileBytes, 0, msg.fileBytes.Length);
                                    fs.Close();

                                    MediaElement myGif = MessageControl.CreateMediaElement(new Uri(Directory.GetCurrentDirectory() + "\\" + directoryName + "\\" + msg.fileName));

                                    Border messageText = MessageControl.CreateUserText(msg.clientName + " прислал gif:");
                                    chat.panelPole.Children.Add(messageText);//показываем уведомление "имя + прислал фото"
                                    chat.panelPole.Children.Add(myGif);//показываем контрол Image, внутри которого принятый нами массив байтов msg.image, преобразованный в ImageSource (BitmapImage)
                                }
                            }
                        }));
                        PlayMessSound();
                    }
                    else //последний вариант: никто не ушел, не пришел, не отправил картинку
                    {
                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            Border border = MessageControl.CreateUserText(msg.clientName + ": " + msg.text);
                            chat.panelPole.Children.Add(border);
                        }));
                        PlayMessSound();
                    }

                    MessageControl.ScrollToBottom(chat.scrollViewer);//листаем панель вниз (там внутри метода тоже делегат!)
                }
            }
            catch (Exception ex)
            {
                ChangeControlText(clientsBox, "Ошибка");
                chat.Dispatcher.Invoke(new ThreadStart(delegate
                {
                    chat.repeateButton.Visibility = Visibility.Visible;
                    chat.sendButton.IsEnabled = false;

                    TextBox errorMessage = MessageControl.CreateText();
                    errorMessage.Text = "Ошибка: видимо сервер упал";
                    chat.panelPole.Children.Add(errorMessage);
                }));
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        //private void border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    MediaPlayer player = new MediaPlayer();
        //    player.Open(new Uri((sender as MyBorder).myText, UriKind.Relative));
        //    player.Play();            
        //}


        void ChangeControlText(TextBox _textBox, string text)
        {
            _textBox.Dispatcher.Invoke(new ThreadStart(delegate
            {
                _textBox.Text = text;
            }));
        }

        void PlayMessSound()
        {
            SoundPlayer mesSound = new SoundPlayer("chat_sound.wav");
            mesSound.Play();
        }

        void CheckName(Message msg, string directoryName)
        {
            int index = 0;
            string name = msg.fileName.Substring(0, msg.fileName.LastIndexOf('.'));
            string type = msg.fileType;
            while (File.Exists(directoryName + "\\" + msg.fileName))
            {
                int k;
                int p = msg.fileName.LastIndexOf('(');
                int p2 = msg.fileName.LastIndexOf(')');
                if (p != -1 && p2 != -1 && Int32.TryParse(msg.fileName.Substring(p + 1, p2 - p - 1), out k))
                {
                    msg.fileName = msg.fileName.Substring(0, p) + "(" + ++k + ")" + type;
                    index = k;
                }
                else msg.fileName = name + "(" + index++ + ")" + type;
            }
        }

        //void border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    chat.Dispatcher.Invoke(new ThreadStart(delegate
        //    {
        //        chat.PlayMusic(new Uri((sender as MyBorder).myText, UriKind.Relative));
        //    }));
        //}
    }
}

