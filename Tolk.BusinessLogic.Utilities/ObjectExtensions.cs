namespace Tolk.BusinessLogic.Utilities
{
    public static class ObjectExtensions
    {
        public static T GetPropertyValue<T>(this object obj, string propName) =>
            (T)obj.GetType().GetProperty(propName).GetValue(obj, null);
    }
}
