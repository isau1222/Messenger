using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientMessenger
{
    public class MyBorder : Border
    {
        public string myText;
        public bool isPressed;
        public bool firstPress;
        //public MediaPlayer music;
        public MediaElement music;
        public MyBorder()
            : base()
        {
            myText = "";
            isPressed = false;
            firstPress = true;
        }
    }
}
