using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Core.Helpers
{
    public class Singleton<T>
    {
        private static Singleton<T> instance;

        public Singleton() { }

        public static Singleton<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Singleton<T>();
                }
                return instance;
            }
        }
    }
}
