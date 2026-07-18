using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MED
{
    public partial class ProcessControl : UserControl
    {
        public ProcessControl()
        {
            InitializeComponent();
        }


        /**
         * Init process statuses
         * */
        private void ProcessControl_VisibleChanged(object sender, EventArgs e)
        {
            ProcessStateChanged(null);
        }

        private IProcess _ActiveProcess;
        public IProcess ActiveProcess
        {
            get
            {
                if (_ActiveProcess == null && ParentForm is ProcessForm)
                    return (IProcess)ParentForm;
                return _ActiveProcess;
            }
            set
            {
                if (_ActiveProcess != null)
                {
                    if (_ActiveProcess is ProcessForm)
                        (_ActiveProcess as ProcessForm).ProcessStateChanged -= ProcessStateChanged;
                    else if (_ActiveProcess is Process)
                        (_ActiveProcess as Process).ProcessStateChanged -= ProcessStateChanged;
                }
                _ActiveProcess = value;

                if (_ActiveProcess != null)
                {
                    if (_ActiveProcess is ProcessForm)
                        (_ActiveProcess as ProcessForm).ProcessStateChanged += ProcessStateChanged;
                    else if (_ActiveProcess is Process)
                        (_ActiveProcess as Process).ProcessStateChanged += ProcessStateChanged;
                }
            }
        }

        private bool _ProcessStateChanging;
        private void ProcessStateChanged(IProcess sender, System.Threading.ThreadState state = System.Threading.ThreadState.Unstarted)
        {
            if (sender == null)
                sender = ActiveProcess;

            if (sender == null)
            {
                cmdStart.Enabled = false;
                chkPause.Enabled = false;
                chkPause.Checked = false;
                cmdStop.Enabled = false;
                return;
            }

            if (state == System.Threading.ThreadState.Unstarted)
                state = sender.ProcessState;
            bool isRunning = state == System.Threading.ThreadState.Running;
            bool isPaused = state == System.Threading.ThreadState.Suspended;

            _ProcessStateChanging = true;

            cmdStart.Enabled = !isRunning && !isPaused;
            chkPause.Enabled = isRunning || isPaused;
            chkPause.Checked = isPaused;
            chkPause.Font = new Font(chkPause.Font, isPaused ? FontStyle.Bold : FontStyle.Regular);
            cmdStop.Enabled = isRunning || isPaused;

            _ProcessStateChanging = false;
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            ActiveProcess?.Start();
            ProcessStateChanged(ActiveProcess);
        }

        private void chkPause_CheckedChanged(object sender, EventArgs e)
        {
            if (_ProcessStateChanging)
                return;

            var p = ActiveProcess;
            if (p != null)
            {
                if (p.ProcessState == System.Threading.ThreadState.Running)
                    p.Pause();
                else if (p.ProcessState == System.Threading.ThreadState.Suspended)
                    p.Resume();
            }
            ProcessStateChanged(p);
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            ActiveProcess?.Stop();
            ProcessStateChanged(ActiveProcess);
        }
    }
}
