﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientMessenger
{
    public class MyGif : MediaElement
    {
        public bool isPressed;

        public MyGif() : base()
        {
            isPressed = false;
        }
    }
}
