using System.Text.Json;

namespace HandsontableBlazor;

public class Callbacks 
{
    public interface ICallbackArgs {}

    public class ValidateArgs : ICallbackArgs
    {
        public ValidateArgs(JsonDocument jdoc)
        {
            IsValid = jdoc.RootElement[0].Deserialize<bool>();
        }

        public bool IsValid { get; set; }
    }
}