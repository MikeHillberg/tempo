using System.Diagnostics.CodeAnalysis;

namespace ExampleClassLibrary
{
    public class ExampleClass
    {
        // Fields
        public string StringField1a = "default";
        [Experimental("EXPLIB001")]
        public string StringField1b = "experimental";

        private int _intField1a;
        [Experimental("EXPLIB001")]
        private int _intField1b;

        // Properties
        public int Property1a { get; set; }
        [Experimental("EXPLIB001")]
        public int Property1b { get; set; }

        public string AutoProperty1a { get; set; } = string.Empty;
        [Experimental("EXPLIB001")]
        public string AutoProperty1b { get; set; } = string.Empty;

        internal bool InternalProperty1a { get; private set; }
        [Experimental("EXPLIB001")]
        internal bool InternalProperty1b { get; private set; }

        protected virtual double VirtualProperty1a { get; set; }
        [Experimental("EXPLIB001")]
        protected virtual double VirtualProperty1b { get; set; }

        // Events
        public event EventHandler? Event1a;
        [Experimental("EXPLIB001")]
        public event EventHandler? Event1b;

        public event Action<string>? ActionEvent1a;
        [Experimental("EXPLIB001")]
        public event Action<string>? ActionEvent1b;

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

        public string StringMethod1a(int param)
        {
            return $"StringMethod1a: {param}";
        }

        [Experimental("EXPLIB001")]
        public string StringMethod1b(int param)
        {
            return $"StringMethod1b: {param}";
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

        protected virtual void VirtualMethod1a()
        {
            OnEvent1a(EventArgs.Empty);
        }

        [Experimental("EXPLIB001")]
        protected virtual void VirtualMethod1b()
        {
            OnEvent1b(EventArgs.Empty);
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
    }
}
