using System.Text.Json;
using System.Text.Json.Serialization;

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
    public interface ICallbackArgs 
    {
        /**
        * The JSON document containing the serialzied callback arguments invoked 
        * from JavaScript.
        */
        JsonDocument JsArgs { get; } 
    }

    public abstract class BaseCallbackArgs : ICallbackArgs
    {
        public JsonDocument JsArgs { get; private set; }

        public BaseCallbackArgs(JsonDocument jdoc)
        {
            JsArgs = jdoc;
        }
    }

    /**
    * Callback argument for all core Validate methods 
    * (ValidateCells(), ValidateColumns(), ValidateRows()).
    */
    public class ValidateArgs : BaseCallbackArgs
    {
        public ValidateArgs(JsonDocument jdoc)
            : base(jdoc)
        {
            IsValid = jdoc.RootElement[0].Deserialize<bool>();
        }

        public bool IsValid { get; set; }
    }

    public class ScrollToFocusedCellArgs : BaseCallbackArgs
    {
        public ScrollToFocusedCellArgs(JsonDocument jdoc)
            : base(jdoc)
        { }
    }
}