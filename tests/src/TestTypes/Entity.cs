using System;


namespace NLuaTest.TestTypes
{
    public class Entity
    {
        public event EventHandler<EventArgs> Clicked;

        protected virtual void OnEntityClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler = Clicked;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public string Property
        {
            get;
            set;
        }

        // default ctor
        public Entity()
        {
            Property = "Default";
        }
        // string ctor
        public Entity(string param)
        {
            Property = "String";

        }

        public Entity(int param)
        {
            Property = "Int";
        }

        public void Click()
        {
            OnEntityClicked(new EventArgs());
        }
    }
}
