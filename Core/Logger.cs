using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MED
{
    public class Logger
    {
        private StringBuilder Buffer = new();


        public void Clear(bool clear = true) => Buffer.Clear();

        public void AppendLine(string msg, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                msg = msg.Replace("{" + i.ToString() + "}", args[i].ToString());
            }

            Buffer.AppendLine(msg);
        }
        public void Append(string msg, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                msg = msg.Replace("{" + i.ToString() + "}", args[i].ToString());
            }

            Buffer.Append(msg);
        }

        public string BufferString(bool clear = true)
        {
            var s = Buffer.ToString();
            if (clear) Buffer.Clear();
            return s;
        }

        public int BufferLength
        {
            get
            {
                return Buffer.Length;
            }
        }
    }
}
