using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInterface.Tests
{
    public class Entity
    {
        public event EventHandler<EventArgs> Clicked;

        protected virtual void OnEntityClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler = Clicked;

            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }        

        public Entity()
        {

        }

        public void Click()
        {
            OnEntityClicked(new EventArgs());
        }
    }
}
