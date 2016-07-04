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

namespace ClientMessenger
{
    //это пятая глава. шестая (конец) - MessageControl
    class GetterMessages
    {
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
                    }
                    else if (msg.image != null) //если мы получили НЕ пустой массив байтов image И имя отправителя не равно нашему собственному (зачем по сети отправлять себе же картинку? поэтому мы её сразу отображаем у себя при отправлении)
                    {//то есть это условие нам гарантирует, что мы не увидим нашу картинку еще раз
                        BitmapImage bitImg = Message.BytesToImageSource(msg.image); //конвертируем массив байтов в BitmapImage (напомню, что BitmapImage, ImageSource, BitmapSource супер схожие вещи)
                        //то есть мы получаем ImageSource, а не контрол Image, потому что ImageSource - это картинка, находящаяся внитри Image. именно она то нам и нужна

                        bitImg.Freeze(); //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1 ОБЯЗАТЕЛЬНО! иначе будет ошибка (еще одна мелочь, к которой я шёл часов 7)

                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            TextBox messageText = MessageControl.CreateText();
                            messageText.Text = msg.clientName + " прислал фото:";
                            Image img = MessageControl.CreateImage(bitImg);//создаем контрол img, содержимым которого становится bitImg (см. MessageControl.CreateImage(), там все свойства устанавливаются)
                            
                            chat.panelPole.Children.Add(messageText);//показываем уведомление "имя + прислал фото"
                            chat.panelPole.Children.Add(img);//показываем контрол Image, внутри которого принятый нами массив байтов msg.image, преобразованный в ImageSource (BitmapImage)
                        }));
                    }
                    else //последний вариант: никто не ушел, не пришел, не отправил картинку
                    {
                        chat.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            Border border = MessageControl.CreateUserText(msg.clientName + ": " + msg.text);
                            chat.panelPole.Children.Add(border);
                        }));
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

        
        void ChangeControlText(TextBox _textBox, string text)
        {
            _textBox.Dispatcher.Invoke(new ThreadStart(delegate
            {
                _textBox.Text = text;
            }));
        }
    }
}

