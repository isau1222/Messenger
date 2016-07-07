using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ClientMessenger
{
    class MyStackPanel:StackPanel
    {
        public event EventHandler OnAdd;

        public MyStackPanel()
            : base()
        {

        }

        public UIElementCollection Children { get 
        {
            if (null != OnAdd)
            {
                OnAdd(this, null);
            }
            return base.Children;
        } }

    }
}
