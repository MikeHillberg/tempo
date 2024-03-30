using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Management.Automation;
using System.Text;
using Tempo;

namespace TempoPSProvider
{
    // This PowerShell cmdlet lets you pipe MemberViewModels back to the Tempo app for display

    [Cmdlet(VerbsData.Out, "Tempo")]
    [OutputType(typeof(MemberViewModelBase))]
    public class OutTempo : Cmdlet
    {
        // Talking to the Tempo app
        static NamedPipeClientStream _pipeClient;
        static StreamWriter _writer;

        // Copy of all the Members property values between Begin and EndProcessing
        List<MemberOrTypeViewModelBase> _allMembers = new List<MemberOrTypeViewModelBase>();

        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public MemberViewModelBase[] Members { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _allMembers.Clear();
        }

        protected override void ProcessRecord()
        {
            base.EndProcessing();

            // If objects are piped to this cmdlet, they show up one at a time rather than as an array.
            // But we might get more than one also.
            foreach (var member in this.Members)
            {
                _allMembers.Add(member);
            }
        }

        // Send the members to Tempo
        protected override void EndProcessing()
        {
            // In all the documentation and blogs, if you pipe into an array property of a cmdlet,
            // all the objects show up as an array. But what really happens is they show up one at a time
            // to ProcessRecord. So in BeginProcessing() we clear _allMembers, in ProcessRecord() we add to it
            // and in EndProcessing() we do the work.
            // It is still possible to get a multi-item array in ProcessRecord though, if you actually pass
            // an array object to the property (rather than pipe to it).


            // Convert the list of types and/or members to a bunch of semicolon-delimited strings
            // Examples:
            //     Type:Windows.UI.Xaml.Controls.Button
            //     Member:Windows.UI.Xaml.Controls.Button:Windows.UI.Xaml.Controls.Button.Flyout

            var payload = new StringBuilder();
            foreach (var member in _allMembers)
            {
                if (member is TypeViewModel)
                {
                    payload.Append($"Type:{member.FullName};");
                }
                else
                {
                    payload.Append($"Member:{member.DeclaringType.FullName}:{member.FullName};");
                }
            }

            // First time this cmdlet is called, set up the pipe back to the app
            if (_pipeClient == null)
            {
                var pipeName = Environment.GetEnvironmentVariable(TempoPSProvider.PipeNameKey);
                _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                _pipeClient.Connect();
                _writer = new StreamWriter(_pipeClient);
            }

            try
            {
                // Write the types/members string
                _writer.WriteLine(payload.ToString());
                _writer.Flush();
                _pipeClient.Flush();

                // Bring the app to the foreground. That has to be done from this process since it
                // has to be done by the foreground app.
                var hwnd = Win32Native.FindWindow(null, "Tempo");
                if ((int)hwnd != -1)
                {
                    Win32Native.SetForegroundWindow(hwnd);
                }
            }
            catch (IOException)
            {
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
            }
        }
    }
}
