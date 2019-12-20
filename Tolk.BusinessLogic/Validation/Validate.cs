using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Validation
{
    public static class Validate
    {
        /// <summary>
        /// Evaluates if condition is true. If not true, then ValidationException will be thrown.
        /// </summary>
        /// <param name="condition">Condition to test</param>
        /// <param name="userMessage">Message to show in exception, if thrown</param>
        /// <exception cref="ValidationException">Thrown if condition is false</exception>
        public static void Ensure(bool condition, string userMessage)
        {
            if (!condition)
            {
                throw new ValidationException(userMessage);
            }
        }
    }
}
