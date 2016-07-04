using ClassLibraryMessenger;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientMessenger
{
    /// <summary>
    /// Логика взаимодействия для Chat.xaml (chat.xaml - наше wpf окно. в этому классе мы имеет доступ к нашим объектам на в окне)
    /// </summary>
    //это четвертая глава.  пятая - GetterMessages
    public partial class Chat : Window
    {
        NetworkStream stream;
        TcpClient client;
        Thread threadNet; //поток дляпринимателя сообщений
        const int port = 8080;
        //const string address = "95.73.181.69";
        //const string address = "192.168.1.19";//doma
        const string address = "128.204.46.128";//yula
        //const string address = "95.72.62.103";
        //const string address = "95.73.213.161";
        //const string address = "95.73.173.95";
        

        public string clientName; //имя клиента

        public Chat(string _clientName) //конструктор вызывается при закрытии окна Welcome. окно велком его вызывает и передаёт имя, введеноем клиентом при авторизации
        {
            InitializeComponent();

            clientsBox.IsReadOnly = true; //не даём клиенту вручную менять содержимое списка имен клиетов

            clientName = _clientName;

            Connect(); //коннектимся
        }

        void Connect()
        {
            TurnOffAll(); //restart services (thread, client, stream)

            repeateButton.Visibility = Visibility.Collapsed; //прячем кнопку репит

            sendButton.IsEnabled = true; //делаем кнопку отправки сообщений активной (мы выключим её, если что-то пойдёт не так)

            try
            {
                client = new TcpClient(address, port);
                stream = client.GetStream();

                GetterMessages getterMes = new GetterMessages(client, stream, clientsBox, this, clientName); //создаем объект GetterMessages. он будет отвечать за отдельным поток по принятию сообщений
                //ведь нам нужно клиенту дать 2 потока - один для отдачи, один для приема

                threadNet = new Thread(getterMes.GetMessage);
                threadNet.Start(); //запускаем поток принятия сообщений

                string messageText = ""; //заглушка. нам не нужен текст, ведь клиент просто зашел. он пока что ничего не написал
                Message msg = new Message(clientName, messageText, true); //создаём конструктором, который говорит, что это firstVisit == true

                BinaryFormatter answFormatter = new BinaryFormatter();
                answFormatter.Serialize(stream, (object)msg); //разбиваем и отправляем наш объект msg

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        private void Button_Click_1(object sender, MouseButtonEventArgs e)//при клике на кнопку Send
        {
            SendMessage();
        }

        void SendMessage()
        {
            try
            {
                string messageText = textSend.Text; //получаем что там клиент написал

                while (messageText.IndexOf("  ") != -1) messageText = messageText.Replace("  ", " ");
                messageText.Trim(); //форматируем строку

                if (messageText == "" || messageText == " " || messageText[messageText.Length-1] == ' ') //убрать второе и третье словие(мб)
                {
                    ClearLine(textSend); //очищаем линию ввода текста
                    return; //выходим из функции. клиент ничего не написал, зачем нам что-то отправлять?
                }

                //этот контрол ownText сперва отобразится в окне у отправляющего клиента. то есть мы показали клиенту, что он написал, что это еще не отправлено на сервер
                Border ownText = MessageControl.CreateUserText(messageText); //создаём контрол (объект, содиржащийся в окне) (см. в конструкторе написано messageText)
                //это контрол для сообщений пользователя. он типа Border, чтобы можно было закруглять края. в него уже вложен контрол TextBox хранящий в себе вообщение messageText
                ownText.HorizontalAlignment = HorizontalAlignment.Right;//говорим нашему контролу с сообщением, что он должен выравниваться по правому краю, потому что его будет видеть сам отправитель
                panelPole.Children.Add(ownText);//добавляем этот контрол на нашу панель сообщений (а она там сама всё компанует как надо)
                //класс MessageControl состоит из статических методов. я его сделал, чтобы каждый новый контрол был желаемого мне типа (вида)
                //(например типа "сообщение от сервера" или "сообщение от клиента (другого)") автоматически заполняло мне все желаемые свойства (например скругленные края; или оступы от други контролов)

                //теперь уже создаём сообщение, серелизуем его и отправляем на сервер
                Message msg = new Message(clientName, messageText);//см. это стандартный конструктор, при котором клиент просто хочет отправить сообщение
                BinaryFormatter answFormatter = new BinaryFormatter();
                answFormatter.Serialize(stream, (object)msg);
                //там на сервере сервер отошлет это сообщение всем свои клиентам, а они в методе GetterMessages.GetMessages уже будут его получать и отображать

                ClearLine(textSend);//отправили текс => надо очистить поле текста
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TurnOffAll();//окно закрылось - гасим всё нахуй, иначе ошибок повылезает, мол поток не закрыт, клиент не закрыт, в результате сервер пиздой накроется
        }

        void ClearLine(TextBox _textBox)
        {
            _textBox.Clear();
        }

        void ShowError(Exception _ex) //фатальная ошибка
        {
            TextBox message = MessageControl.CreateText(); //это уже контрол типа TextBox, а не Border. просто у него нет круглых краев. это сообщение, которое отобразится как сообщение от сервера 
            //(хотя здесь не идёт прием от самого сервера. ClientMessenger здесь сам сообщает, что что-то пошло не так)
            message.Text = "Ошибка: " + _ex.Message;
            //message.HorizontalAlignment = HorizontalAlignment.Center; //он у нас по центру всегда должен быть
            panelPole.Children.Add(message); //выводим его на панель

            clientsBox.Text = "Ошибка";
            repeateButton.Visibility = Visibility.Visible;//показываем рипит боттон

            sendButton.IsEnabled = false; //отключаем возможность отправить сообщение (связи же нету)
            MessageControl.ScrollToBottom(scrollViewer); //наша стакПанель (панель для сообщений) находится внути контрола scrollViewer
            //scrollViewer предназначен для прокрутки того, что у него внутри (а именно стакПанели)

            TurnOffAll(); //ну ты понял. пиздой накроется сервер без этого (мб)
        }

        void ShowMyError(string errMessage) //это уже не фатальная ошибка. это просто неправильное расширение картинки
        {
            TextBox message = MessageControl.CreateText(); //создаем текстБокс с надписью:
            message.Text = "Неправильное расширение файла!";
            //message.HorizontalAlignment = HorizontalAlignment.Center; //ставим по центру
            panelPole.Children.Add(message);
        }

        private void textSend_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sendButton.IsEnabled == true)
            {
                SendMessage();
            }
        }

        private void textSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            //это чтобы textSend скролился вниз, если сообщение в нём слишком большое
            textSend.ScrollToEnd();
        }

        private void repeateButton_Click(object sender, RoutedEventArgs e)
        {
            //нажали репит - пробуем законнектится
            Connect();
        }

        private void fileAdder_Click(object sender, RoutedEventArgs e)//кнопка добавления картинки
        {
            string filePath = "";
            string rashirenie;
            OpenFileDialog ofd = new OpenFileDialog();
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true) //пользователь выбрал файл и нажал OK
            {
                filePath = ofd.FileName;
                rashirenie = System.IO.Path.GetExtension(filePath);
                if (rashirenie != ".png" && rashirenie != ".jpg" && rashirenie != ".gif")
                {
                    ShowMyError(rashirenie);
                    return;
                }
            }
            else //он нажал Отмена
            {
                return;
            }

            try
            {
                Image img = new Image();
                img.Source = new BitmapImage(new Uri(filePath));  //считываем картинку, которую выбрал клиент

                Image ownImage = MessageControl.CreateImage(img.Source); //это контрол, который не пойдёт на сервер. этот контрол сразу увидет отправитель (и поймёт, что отправил картинку)
                ownImage.HorizontalAlignment = HorizontalAlignment.Right; //ставим справа картинку. это же мы её отправили
                panelPole.Children.Add(ownImage); //показываем
                
                //дальше идёт самый потный момент
                BitmapSource sc = (BitmapSource)img.Source;               

                //BitmapSource, ImageSource, BitmapImage - это всё классы чуть ли не одного типа, так что они легко друг в друга переходят
                byte[] imageBytes = Message.ImageSourceToBytes(new PngBitmapEncoder(), sc); //получаем массив байтов нашим статическим методом. этот массив байтов и есть наша картинка (точнее её содержимое ImageSource, потому что Image - это контрол, а одно из его свойств - ImageSource)
                Message msg = new Message(clientName, imageBytes);//создаем сообщение конструктором, указывающим, что пользователь захотел отправить картинку

                BinaryFormatter answFormatter = new BinaryFormatter();
                answFormatter.Serialize(stream, (object)msg); //передаём наш мессандж
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        void TurnOffAll()//вырубаем всё (что-то пошло не так)
        {
            if (threadNet != null)
                threadNet.Abort();
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
        }
    }
}
//руководство: как добавить контрол на панель:
 //TextBox txtBox = new TextBox();
 //    myPanel.Children.Add(txtBox);//все остальные элементы добавляются по аналогии 
