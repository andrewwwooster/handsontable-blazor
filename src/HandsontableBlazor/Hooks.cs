namespace HandsontableBlazor;

public class Hooks {

    public delegate Task AfterChangeHook(AfterChangeArgs args);

    public class AfterChangeArgs {
        public required IList<IList<object>> Data { get; set; }
        public required string Source { get; set; }

        public IEnumerable<DataChange> GetDataChanges() 
        {
            return Data.Select(args => new DataChange(args));
        }

        public class DataChange(IList<object> args)
        {
            private readonly IList<object> _args = args;

            public int Row => (int) _args[0];
            public string Prop => (string) _args[1];
            public object? OldVal => _args[2];
            public object? NewVal => _args[3];
        }
    }
}