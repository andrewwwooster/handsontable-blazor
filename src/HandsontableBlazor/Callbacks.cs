using System.Text.Json;

namespace HandsontableBlazor;

/**
 * This class defines the classes used to capture callback arguments for the core 
 * HandsontableBlazor methods.
 * In Javascript, arguments are passed as individual parameters to the callback function.
 * Instead here we capture these areguments in a single class that is passed to the
 * callback.  This simplifies the callback signature.
 */
public class Callbacks 
{
    public interface ICallbackArgs {}

    /**
    * Callback argument for all core Validate methods 
    * (ValidateCells(), ValidateColumns(), ValidateRows()).
    */
    public class ValidateArgs : ICallbackArgs
    {
        public ValidateArgs(JsonDocument jdoc)
        {
            IsValid = jdoc.RootElement[0].Deserialize<bool>();
        }

        public bool IsValid { get; set; }
    }
}