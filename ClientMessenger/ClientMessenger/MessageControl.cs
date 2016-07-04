using System;
using System.Collections.Generic;
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
    public class MessageControl : Button
    {
        public static Border CreateUserText(string _text)//клиентовский текст
        {
            Border border = new Border();
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

        public static Image CreateImage(ImageSource imSource)//создатель сообщений-картинок
        {
            Image image = new Image();
            image.Source = imSource;

            image.Stretch = Stretch.Uniform;
            image.MaxHeight = 300;
            image.MaxWidth = 300;

            image.Width = imSource.Width;
            image.Height = imSource.Height;

            image.MouseDown += image_MouseDown;

            image.Margin = SetMargin(5, image);
            image.HorizontalAlignment = HorizontalAlignment.Left; //по умолчанию картинка будем жаться к левому боку, потому что чужие сообщения слева
            //но вот когда мы хотим отправить такую штуку, мы помимо этого статического метода CreateImage(source) пишем в создавшийся Image: 
            //image.HorizontalAlignment = HorizontalAlignment.Right; потому что мы отправитель и должны видеть наше детище справа
            return image;
        }

        static void image_MouseDown(object sender, RoutedEventArgs e)
        {
            if ((sender as Image).MaxHeight != 300)
            {
                (sender as Image).MaxHeight = 300;
                (sender as Image).MaxWidth = 300;
            }
            else
            {
                (sender as Image).MaxHeight = (sender as Image).Source.Height;
                (sender as Image).MaxWidth = (sender as Image).Source.Width;
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
