using System.Diagnostics.CodeAnalysis;

namespace ExampleClassLibrary
{
    public class ExampleClass
    {
        // Fields
        public string StringField1a = "default";
        [Experimental("EXPLIB001")]
        public string StringField1b = "experimental";

        public string StringField2a = "default";
        [Experimental("EXPLIB001")]
        public string StringField2b = "experimental";

        private int _intField1a;
        [Experimental("EXPLIB001")]
        private int _intField1b;

        private int _intField2a;
        [Experimental("EXPLIB001")]
        private int _intField2b;

        // Properties
        public int Property1a { get; set; }
        [Experimental("EXPLIB001")]
        public int Property1b { get; set; }

        public int Property2a { get; set; }
        [Experimental("EXPLIB001")]
        public int Property2b { get; set; }

        public string AutoProperty1a { get; set; } = string.Empty;
        [Experimental("EXPLIB001")]
        public string AutoProperty1b { get; set; } = string.Empty;

        public string AutoProperty2a { get; set; } = string.Empty;
        [Experimental("EXPLIB001")]
        public string AutoProperty2b { get; set; } = string.Empty;

        internal bool InternalProperty1a { get; private set; }
        [Experimental("EXPLIB001")]
        internal bool InternalProperty1b { get; private set; }

        internal bool InternalProperty2a { get; private set; }
        [Experimental("EXPLIB001")]
        internal bool InternalProperty2b { get; private set; }

        protected virtual double VirtualProperty1a { get; set; }
        [Experimental("EXPLIB001")]
        protected virtual double VirtualProperty1b { get; set; }

        protected virtual double VirtualProperty2a { get; set; }
        [Experimental("EXPLIB001")]
        protected virtual double VirtualProperty2b { get; set; }

        // Events
        public event EventHandler? Event1a;
        [Experimental("EXPLIB001")]
        public event EventHandler? Event1b;

        public event EventHandler? Event2a;
        [Experimental("EXPLIB001")]
        public event EventHandler? Event2b;

        public event Action<string>? ActionEvent1a;
        [Experimental("EXPLIB001")]
        public event Action<string>? ActionEvent1b;

        public event Action<string>? ActionEvent2a;
        [Experimental("EXPLIB001")]
        public event Action<string>? ActionEvent2b;

        // Methods
        public void Method1a()
        {
            Console.WriteLine("Method1a called");
        }

        [Experimental("EXPLIB001")]
        public void Method1b()
        {
            Console.WriteLine("Method1b called - Experimental");
        }

        public void Method2a()
        {
            Console.WriteLine("Method2a called");
        }

        [Experimental("EXPLIB001")]
        public void Method2b()
        {
            Console.WriteLine("Method2b called - Experimental");
        }

        public string StringMethod1a(int param)
        {
            return $"StringMethod1a: {param}";
        }

        [Experimental("EXPLIB001")]
        public string StringMethod1b(int param)
        {
            return $"StringMethod1b: {param}";
        }

        public string StringMethod2a(int param)
        {
            return $"StringMethod2a: {param}";
        }

        [Experimental("EXPLIB001")]
        public string StringMethod2b(int param)
        {
            return $"StringMethod2b: {param}";
        }

        internal async Task<bool> AsyncMethod1a(string input)
        {
            await Task.Delay(100);
            return !string.IsNullOrEmpty(input);
        }

        [Experimental("EXPLIB001")]
        internal async Task<bool> AsyncMethod1b(string input)
        {
            await Task.Delay(100);
            return !string.IsNullOrEmpty(input);
        }

        internal async Task<bool> AsyncMethod2a(string input)
        {
            await Task.Delay(100);
            return !string.IsNullOrEmpty(input);
        }

        [Experimental("EXPLIB001")]
        internal async Task<bool> AsyncMethod2b(string input)
        {
            await Task.Delay(100);
            return !string.IsNullOrEmpty(input);
        }

        protected virtual void VirtualMethod1a()
        {
            OnEvent1a(EventArgs.Empty);
        }

        [Experimental("EXPLIB001")]
        protected virtual void VirtualMethod1b()
        {
            OnEvent1b(EventArgs.Empty);
        }

        protected virtual void VirtualMethod2a()
        {
            OnEvent2a(EventArgs.Empty);
        }

        [Experimental("EXPLIB001")]
        protected virtual void VirtualMethod2b()
        {
            OnEvent2b(EventArgs.Empty);
        }

        public List<T> GenericMethod1a<T>(T item)
        {
            return new List<T> { item };
        }

        [Experimental("EXPLIB001")]
        public List<T> GenericMethod1b<T>(T item)
        {
            return new List<T> { item };
        }

        public List<T> GenericMethod2a<T>(T item)
        {
            return new List<T> { item };
        }

        [Experimental("EXPLIB001")]
        public List<T> GenericMethod2b<T>(T item)
        {
            return new List<T> { item };
        }

        // Event raisers
        protected virtual void OnEvent1a(EventArgs e)
        {
            Event1a?.Invoke(this, e);
        }

        [Experimental("EXPLIB001")]
        protected virtual void OnEvent1b(EventArgs e)
        {
            Event1b?.Invoke(this, e);
        }

        protected virtual void OnEvent2a(EventArgs e)
        {
            Event2a?.Invoke(this, e);
        }

        [Experimental("EXPLIB001")]
        protected virtual void OnEvent2b(EventArgs e)
        {
            Event2b?.Invoke(this, e);
        }
    }
}
