using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    /**
     * Wrapper for polymorphistic model binding
     */
    public interface ICheckboxGroup { }

    public class CheckboxGroup<T> : ICheckboxGroup
    {
        public HashSet<T> SelectedItems { get; set; }
    }
}
