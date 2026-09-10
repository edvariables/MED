using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public interface IGameController : IProcess, IProvider
    {
        void OnControllerChanged(IGameController sender, PropertyChangedEventArgs e);
        object? GetControllerPropertyValue(string controllerProperty);
        void SetControllerPropertyValue(string controllerProperty, object? value);
    }
}
