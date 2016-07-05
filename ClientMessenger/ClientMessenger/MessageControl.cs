using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientMessenger
{
    //тут создаются разных типов контроллы. просто для удобства, чтобы тысячу раз все свойства не переназначать
    public class MessageControl
    {
        public static MyBorder CreateUserText(string _text)//клиентовский текст
        {
            MyBorder border = new MyBorder();
            border.Background = Brushes.White;
            border.SnapsToDevicePixels = true;
            border.HorizontalAlignment = HorizontalAlignment.Left;

            Thickness borderThickness = border.BorderThickness;
            borderThickness.Left = 0;
            borderThickness.Top = 0;
            borderThickness.Right = 0;
            borderThickness.Bottom = 0;
            border.BorderThickness = borderThickness;

            border.CornerRadius = SetCornerRadius(12, border);//закругления (обрати внимание, эти 3 метода SetCornerRadius, SetPadding, SetMargin написаны мной)
            border.Padding = SetPadding(4, border);
            border.Margin = SetMargin(5, border);//всякие отступы

            TextBox textBox = CreateText();
            textBox.Text = _text;
            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;//растягиваем текстбокс повсему содержимому его РОДИТЕЛЯ (тут вся соль этих "свойств зависимостей". контрол ведет себя по разному находять внутри разных контролов!)
            textBox.Margin = SetMargin(0, textBox);
            textBox.Padding = SetPadding(0, textBox); //мы создали стандартный текст для уведомлений, чтобы положить его в наш красивый бордер. а там по стандарту есть отступы, вот мы их и зануляем

            border.Child = textBox;//усыновляем текстбокс
            return border;
        }

        //public static MyBorder CreateMusicBorder(string _text, string _directoryName, string _fileName)
        //{
        //    MyBorder border = CreateUserText(_text);
        //    Thickness padding = border.Padding;
        //    padding.Left = 25;
        //    border.Padding = padding;

        //    using (FileStream fs = new FileStream(_directoryName + "\\" + _fileName, FileMode.Create))
        //    {
        //        fs.Write(msg.fileBytes, 0, msg.fileBytes.Length);
        //        fs.Close();
                
        //    }

        //    return border;
        //}

        public static TextBox CreateText()//текст для уведомлений
        {
            TextBox textBox = new TextBox();
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.FontSize = 18;
            textBox.IsReadOnly = true;
            textBox.HorizontalAlignment = HorizontalAlignment.Center;

            Thickness borderThickness = textBox.BorderThickness;
            borderThickness.Left = 0;
            borderThickness.Top = 0;
            borderThickness.Right = 0;
            borderThickness.Bottom = 0;
            textBox.BorderThickness = borderThickness;
            
            textBox.Margin = SetMargin(5, textBox);
            textBox.Padding = SetPadding(5, textBox);

            textBox.MaxWidth = 750; //максимальная ширина

            return textBox;
        }

        public static MyGif CreateMediaElement(Uri uri)//создатель сообщений-картинок
        {
            MyGif myGif = new MyGif();
            myGif.MediaEnded += myGif_MediaEnded;
            //myGif.MouseDown += myGif_MouseDown;
            myGif.MouseLeftButtonDown += myGif_MouseLeftButtonDown;
            myGif.MouseRightButtonDown += myGif_MouseRightButtonDown;
            myGif.UnloadedBehavior = MediaState.Manual;
            myGif.LoadedBehavior = MediaState.Manual;
            myGif.Source = uri;


            myGif.Stretch = Stretch.Uniform;
            myGif.MaxHeight = 300;
            myGif.MaxWidth = 300;

            myGif.Margin = SetMargin(5, myGif);
            myGif.HorizontalAlignment = HorizontalAlignment.Left; //по умолчанию картинка будем жаться к левому боку, потому что чужие сообщения слева
            myGif.Play();

            return myGif;
        }

        static void myGif_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as MyGif).isPressed == false)
            {
                (sender as MyGif).isPressed = true;
                (sender as MyGif).Pause();
            }
            else
            {
                (sender as MyGif).isPressed = false;
                (sender as MyGif).Play();
            }
        }

        private static void myGif_MouseLeftButtonDown(object sender, EventArgs e)
        {

            if ((sender as FrameworkElement).MaxHeight != 300)
            {
                (sender as FrameworkElement).MaxHeight = 300;
                (sender as FrameworkElement).MaxWidth = 300;
            }
            else
            {
                (sender as FrameworkElement).MaxHeight = (sender is MyGif) ? (sender as MyGif).NaturalVideoHeight : (sender as MyImage).Source.Height;
                (sender as FrameworkElement).MaxWidth = (sender is MyGif) ? (sender as MyGif).NaturalVideoWidth : (sender as MyImage).Source.Width;

            }
        }

        static void myGif_MediaEnded(object sender, RoutedEventArgs e)
        {
            if ((sender as MyGif).isPressed == false)
            {
                (sender as MediaElement).Position = new TimeSpan(0, 0, 1);
                (sender as MediaElement).Play();
            }
        }

        public static MyImage CreateImage(ImageSource imSource)//создатель сообщений-картинок
        {
            MyImage myImage = new MyImage();
            myImage.Source = imSource;

            myImage.Stretch = Stretch.Uniform;
            myImage.MaxHeight = 300;
            myImage.MaxWidth = 300;

            if (myImage.Height <= myImage.Width)
            {
                myImage.Height -= imSource.Height;
            }
            else
            {
                myImage.Width -= imSource.Width;
            }

            if (myImage.MaxHeight < myImage.Source.Height) 
            {
                myImage.canResize = true;
            }

            myImage.MouseDown += image_MouseDown;

            myImage.Margin = SetMargin(5, myImage);
            myImage.HorizontalAlignment = HorizontalAlignment.Left; //по умолчанию картинка будем жаться к левому боку, потому что чужие сообщения слева
            //но вот когда мы хотим отправить такую штуку, мы помимо этого статического метода CreateImage(source) пишем в создавшийся Image: 
            //image.HorizontalAlignment = HorizontalAlignment.Right; потому что мы отправитель и должны видеть наше детище справа
            return myImage;
        }

        static void image_MouseDown(object sender, RoutedEventArgs e)
        {
            if ((sender as MyImage).canResize)
            {
                myGif_MouseLeftButtonDown(sender, e);
            }
        }


        //дальше всякая херня, убирающая границы, внутренние оступы, внешние отступы
        public static Thickness SetPadding(int objPad, Control control)
        {
            Thickness padding = control.Padding;
            padding.Left = objPad;
            padding.Top = objPad;
            padding.Right = objPad;
            padding.Bottom = objPad;

            return padding;
        }

        public static Thickness SetMargin(double objMarg, Control control)
        {
            Thickness margin = control.Margin;
            margin.Top = objMarg;
            margin.Bottom = objMarg;

            return margin;
        }

        public static Thickness SetMargin(double objMarg, Image control)
        {
            Thickness margin = control.Margin;
            margin.Top = objMarg;
            margin.Bottom = objMarg;

            return margin;
        }

        public static Thickness SetMargin(double objMarg,  MediaElement control)
        {
            Thickness margin = control.Margin;
            margin.Top = objMarg;
            margin.Bottom = objMarg;

            return margin;
        }

        public static void ScrollToBottom(ScrollViewer scrollViewer)
        {
            scrollViewer.Dispatcher.Invoke(new ThreadStart(delegate
            {
                scrollViewer.UpdateLayout();
                scrollViewer.ScrollToEnd();
            }));
        }

        public static Thickness SetPadding(double objPad, Border control)
        {
            Thickness padding = control.Padding;
            padding.Left = objPad;
            padding.Top = objPad;
            padding.Right = objPad;
            padding.Bottom = objPad;

            return padding;
        }

        public static Thickness SetMargin(double objMarg, Border control)
        {
            Thickness margin = control.Margin;
            margin.Top = objMarg;
            margin.Bottom = objMarg;

            return margin;
        }

        //добавить скругление бордеру
        public static CornerRadius SetCornerRadius(double rad, Border control)
        {
            CornerRadius cornerRadius = control.CornerRadius;
            cornerRadius.BottomLeft = rad;
            cornerRadius.BottomRight = rad;
            cornerRadius.TopLeft= rad;
            cornerRadius.TopRight= rad;

            return cornerRadius;
        }
    }
}
