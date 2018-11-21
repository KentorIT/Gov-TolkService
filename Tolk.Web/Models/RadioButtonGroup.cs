using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    /**
     * Wrapper for polymorphistic model binding
     */
    public interface IRadioButtonGroup { }

    public class RadioButtonGroup<T> : IRadioButtonGroup
    {
        public T SelectedItem { get; set; }
    }
}
