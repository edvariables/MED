using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED.Core
{
    public interface IUndo
    {
        Dictionary<string, object>? Undo(int length = 1);
        void UndoClear();
        Dictionary<string, object> UndoModeSaveProperties();
    }
}
