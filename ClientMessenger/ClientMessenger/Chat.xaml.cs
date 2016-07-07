using ClassLibraryMessenger;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Speech.Synthesis;
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
        const string address = "176.194.51.236";//andrew
        //const string address = "10.210.51.4";//uni    
        //const string address = "192.168.3.8";//yula
        //const string address = "79.111.23.247";//andr
        //const string address = "95.72.62.103";
        //const string address = "95.73.213.161";
        //const string address = "95.73.173.95";
        int soundBorderNum = 0;
        public List<MyBorder> soundBorders;
        public string clientName; //имя клиента

        bool canScrollBottom;

        public Chat(string _clientName) //конструктор вызывается при закрытии окна Welcome. окно велком его вызывает и передаёт имя, введеноем клиентом при авторизации
        {
            InitializeComponent();

            clientsBox.IsReadOnly = true; //не даём клиенту вручную менять содержимое списка имен клиетов

            clientName = _clientName;

            soundBorders = new List<MyBorder>();
            turnLeft.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Left.png"));
            turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Pause.png"));
            turnRight.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Right.png"));

            playerPanel.Visibility = Visibility.Collapsed;

            Connect(); //коннектимся
        }

        void Connect()
        {
            canScrollBottom = true;
            TurnOffAll(); //restart services (thread, client, stream)

            repeateButton.Visibility = Visibility.Collapsed; //прячем кнопку репит
            bottomButton.Visibility = Visibility.Collapsed; //прячем кнопку репит

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
                Message msg = new Message(Message.MyMessageMode.LogIn, new Tuple<string>(clientName)); //создаём конструктором, который говорит, что это firstVisit == true

                BinaryFormatter answFormatter = new BinaryFormatter();
                answFormatter.Serialize(stream, (object)msg); //разбиваем и отправляем наш объект msg

                //PromptBuilder prompt = new PromptBuilder();
                //prompt.AppendText("Привет,");
                //prompt.AppendText(clientName, PromptEmphasis.Strong);
                //SpeechSynthesizer synthesizer = new SpeechSynthesizer();
                //synthesizer.Speak(prompt);
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
            canScrollBottom = true;
            try
            {
                string messageText = textSend.Text; //получаем что там клиент написал

                while (messageText.IndexOf("  ") != -1) messageText = messageText.Replace("  ", " ");
                messageText.Trim(); //форматируем строку

                if (messageText == "" || messageText == " ") //убрать второе и третье словие(мб) //тут всё нормик
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
                Message msg = new Message(Message.MyMessageMode.NewMessage, new Tuple<string,string>(clientName, messageText));//см. это стандартный конструктор, при котором клиент просто хочет отправить сообщение
                BinaryFormatter answFormatter = new BinaryFormatter();
                answFormatter.Serialize(stream, (object)msg);
                //там на сервере сервер отошлет это сообщение всем свои клиентам, а они в методе GetterMessages.GetMessages уже будут его получать и отображать
                ClearLine(textSend);//отправили текс => надо очистить поле текста

                MessageControl.ScrollToBottom(scrollViewer);
                //scrollViewer.UpdateLayout();
                //scrollViewer.ScrollToEnd();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TurnOffAll();//окно закрылось - гасим всё, иначе ошибок повылезает, мол поток не закрыт, клиент не закрыт, в результате сервер накроется
        }

        void ClearLine(TextBox _textBox)
        {
            _textBox.Clear();
        }

        void ShowError(Exception _ex) //фатальная ошибка
        {
            canScrollBottom = true;
            TextBox message = MessageControl.CreateText(); //это уже контрол типа TextBox, а не Border. просто у него нет круглых краев. это сообщение, которое отобразится как сообщение от сервера 
            //(хотя здесь не идёт прием от самого сервера. ClientMessenger здесь сам сообщает, что что-то пошло не так)
            message.Text = "Ошибка: " + _ex.Message;
            //message.HorizontalAlignment = HorizontalAlignment.Center; //он у нас по центру всегда должен быть
            panelPole.Children.Add(message); //выводим его на панель

            clientsBox.Text = "Ошибка";
            repeateButton.Visibility = Visibility.Visible;//показываем рипит боттон

            sendButton.IsEnabled = false; //отключаем возможность отправить сообщение (связи же нету)
            //MessageControl.ScrollToBottom(scrollViewer); //наша стакПанель (панель для сообщений) находится внути контрола scrollViewer
            //scrollViewer предназначен для прокрутки того, что у него внутри (а именно стакПанели)
            MessageControl.ScrollToBottom(scrollViewer);
            TurnOffAll(); //ну ты понял. пиздой накроется сервер без этого (мб)
        }

        void ShowMyError(string errMessage) //это уже не фатальная ошибка. это просто неправильное расширение картинки
        {
            canScrollBottom = true;
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
            try
            {
                string filePath = "";
                string rashirenie;
                OpenFileDialog ofd = new OpenFileDialog();
                Nullable<bool> result = ofd.ShowDialog();
                if (result == true) //пользователь выбрал файл и нажал OK
                {
                    filePath = ofd.FileName;
                    rashirenie = System.IO.Path.GetExtension(filePath);

                    canScrollBottom = true;

                    if (rashirenie == ".jpg" || rashirenie == ".png" || rashirenie == ".jpeg")//с .jpeg рабоатет, н окривовато
                    {
                        SendImage(filePath);
                    }
                    else if (rashirenie == ".wav" || rashirenie == ".mp3")
                    {
                        if (SendFile(filePath))
                        {
                            MyBorder border = MessageControl.CreateUserText(System.IO.Path.GetFileName(filePath));
                            border.myText = filePath;
                            border.HorizontalAlignment = HorizontalAlignment.Right;
                            Thickness padding = border.Padding;
                            padding.Left = 25;
                            border.Padding = padding;

                            border.MouseDown += border_MouseDown;
                            soundBorders.Add(border);
                            border.myNum = soundBorders.IndexOf(border);
                            border.Background = Brushes.LightGray;

                            panelPole.Children.Add(border);
                        }
                        else
                        {
                            foreach (MyBorder b in soundBorders)
                            {
                                if (GetterMessages.myPlayer.Source == new Uri(b.myText))
                                {
                                    MyBorder border = MessageControl.CreateUserText(System.IO.Path.GetFileName(b.myText));
                                    border.myText = b.myText;
                                    border.HorizontalAlignment = HorizontalAlignment.Right;
                                    Thickness padding = border.Padding;
                                    padding.Left = 25;
                                    border.Padding = padding;

                                    border.MouseDown += border_MouseDown;

                                    panelPole.Children.Add(border);
                                    break;
                                }
                            }
                            
                        }
                    }
                    else if (rashirenie == ".gif")
                    {
                        if (SendFile(filePath))
                        {

                            MyGif myGif = MessageControl.CreateMediaElement(new Uri(filePath));
                            myGif.HorizontalAlignment = HorizontalAlignment.Right;
                            panelPole.Children.Add(myGif);
                        }
                    }
                    else
                    {
                        ShowMyError("Неправильное расширение!");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public void border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            soundBorders[soundBorderNum].Background = Brushes.LightGray;
            soundBorderNum = (sender as MyBorder).myNum;
           
            if (playerPanel.Visibility == Visibility.Collapsed)
            {
                playerPanel.Visibility = Visibility.Visible;
            }

            if (GetterMessages.myPlayer != null && GetterMessages.myPlayer.Source == new Uri((sender as MyBorder).myText, UriKind.Absolute))
            {

                if (GetterMessages.isPlay)
                {
                    turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Play.png"));
                    GetterMessages.myPlayer.Pause();
                    GetterMessages.isPlay = false;
                    (sender as MyBorder).Background = Brushes.LightGreen;
                }
                else
                {
                    turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Pause.png"));
                    GetterMessages.myPlayer.Play();
                    GetterMessages.isPlay = true;
                    (sender as MyBorder).Background = Brushes.YellowGreen;
                }
            }
            else
            {
                if (GetterMessages.myPlayer != null)
                    GetterMessages.myPlayer.Close();
                GetterMessages.myPlayer = new MediaElement();
                GetterMessages.myPlayer.UnloadedBehavior = MediaState.Manual;
                GetterMessages.myPlayer.Source = new Uri((sender as MyBorder).myText, UriKind.Absolute);
                GetterMessages.myPlayer.Play();
                GetterMessages.isPlay = true;
                (sender as MyBorder).Background = Brushes.YellowGreen;
            }
        }

        void SendImage(string filePath)
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
            Message msg = new Message(Message.MyMessageMode.NewImage, new Tuple<string,byte[]>(clientName, imageBytes));//создаем сообщение конструктором, указывающим, что пользователь захотел отправить картинку

            BinaryFormatter answFormatter = new BinaryFormatter();
            answFormatter.Serialize(stream, (object)msg); //передаём наш мессандж
        }

        bool SendFile(string filePath)
        {
            byte[] fileBytes;
            string fileName = System.IO.Path.GetFileName(filePath);
            try
            {
                using (FileStream readMyFile = new FileStream(filePath, FileMode.Open))
                {
                    fileBytes = new byte[readMyFile.Length]; //создаем массив байтов такой длины, чтобы наш файл влез полностью
                    readMyFile.Read(fileBytes, 0, fileBytes.Length); // считываем байты файла в наш массив
                    //readMyFile.Close();
                }
            }
            catch
            {
                return false;
            }

            Message msg = new Message(Message.MyMessageMode.NewMusic,new Tuple<string,string,byte[],string>(clientName, System.IO.Path.GetExtension(filePath),fileBytes, fileName));//создаем сообщение конструктором, указывающим, что пользователь захотел отправить картинку

            Border ownText = MessageControl.CreateUserText("Файл " + fileName + " отправлен"); //создаём контрол (объект, содиржащийся в окне) (см. в конструкторе написано messageText)
            //это контрол для сообщений пользователя. он типа Border, чтобы можно было закруглять края. в него уже вложен контрол TextBox хранящий в себе вообщение messageText
            ownText.HorizontalAlignment = HorizontalAlignment.Right;//говорим нашему контролу с сообщением, что он должен выравниваться по правому краю, потому что его будет видеть сам отправитель
            panelPole.Children.Add(ownText);
            BinaryFormatter answFormatter = new BinaryFormatter();
            answFormatter.Serialize(stream, (object)msg); //передаём наш мессандж
            return true;
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

        private void panelPole_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (canScrollBottom)
            {
                MessageControl.ScrollToBottom(scrollViewer);
            }
            else
            {
                bottomButton.Visibility = Visibility.Visible;
            }
        }

        private void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                canScrollBottom = true;
                bottomButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                canScrollBottom = false;
            }
        }

        private void bottomButton_Click(object sender, RoutedEventArgs e)
        {
            MessageControl.ScrollToBottom(scrollViewer);
        }

        private void turnLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            GetterMessages.myPlayer.Close();
            GetterMessages.myPlayer = new MediaElement();
            GetterMessages.myPlayer.UnloadedBehavior = MediaState.Manual;
            soundBorders[soundBorderNum].Background = Brushes.LightGray;
            if (soundBorderNum == 0)
            {
                GetterMessages.myPlayer.Source = new Uri(soundBorders[soundBorders.Count - 1].myText, UriKind.Absolute);
                soundBorderNum = soundBorders.Count - 1;
            }
            else
            {
                GetterMessages.myPlayer.Source = new Uri(soundBorders[soundBorderNum - 1].myText, UriKind.Absolute);
                soundBorderNum--;
            }
            GetterMessages.myPlayer.Play();
            GetterMessages.isPlay = true;
            soundBorders[soundBorderNum].Background = Brushes.YellowGreen;
            turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Pause.png"));
        }

        private void turnRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            GetterMessages.myPlayer.Close();
            GetterMessages.myPlayer = new MediaElement();
            GetterMessages.myPlayer.UnloadedBehavior = MediaState.Manual;
            soundBorders[soundBorderNum].Background = Brushes.LightGray;
            if (soundBorderNum == soundBorders.Count - 1)
            {
                GetterMessages.myPlayer.Source = new Uri(soundBorders[0].myText, UriKind.Absolute);
                soundBorderNum = 0;
            }
            else
            {
                GetterMessages.myPlayer.Source = new Uri(soundBorders[soundBorderNum + 1].myText, UriKind.Absolute);
                soundBorderNum++;
            }
            
            GetterMessages.myPlayer.Play();
            GetterMessages.isPlay = true;
            soundBorders[soundBorderNum].Background = Brushes.YellowGreen;
            turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Pause.png"));
        }

        private void turnPlayer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (GetterMessages.isPlay == true)
            {
                turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Play.png"));
                GetterMessages.myPlayer.Pause();
                GetterMessages.isPlay = false;
                soundBorders[soundBorderNum].Background = Brushes.LightGreen;
            }
            else
            {
                turnPlayer.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/StandartImages/Pause.png"));
                GetterMessages.myPlayer.Play();
                GetterMessages.isPlay = true;
                soundBorders[soundBorderNum].Background = Brushes.YellowGreen;
            }
        }
    }
}
//руководство: как добавить контрол на панель:
//TextBox txtBox = new TextBox();
//myPanel.Children.Add(txtBox);//все остальные элементы добавляются по аналогии 
