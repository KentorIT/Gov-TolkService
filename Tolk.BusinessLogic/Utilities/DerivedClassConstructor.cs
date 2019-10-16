using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public static class DerivedClassConstructor
    {

        /// <summary>
        /// Construct a derived class of from a base class
        /// </summary>
        /// <typeparam name="TBase">Type of base class</typeparam>
        /// <typeparam name="T">Type of derived class you want</typeparam>
        /// <param name="Base">The instance of the base class</param>
        public static T Construct<TBase, T>(TBase Base) where T : TBase, new()
        {
            // create derived instance
            T derived = new T();
            // get all base class properties
            PropertyInfo[] properties = typeof(TBase).GetProperties();
            foreach (PropertyInfo bp in properties)
            {
                // get derived matching property
                PropertyInfo dp = typeof(T).GetProperty(bp.Name, bp.PropertyType);

                // this property must not be index property
                if (
                    (dp != null)
                    && (dp.GetSetMethod() != null)
                    && (bp.GetIndexParameters().Length == 0)
                    && (dp.GetIndexParameters().Length == 0)
                )
                    dp.SetValue(derived, dp.GetValue(Base, null), null);
            }
            return derived;
        }
    }
}
