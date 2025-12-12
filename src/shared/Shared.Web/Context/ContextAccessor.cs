namespace Shared.Web.Context;

//TODO: Refactor the nullable handling
public sealed class ContextAccessor {
    private static readonly AsyncLocal<ContextHolder> Holder = new();

    public IContext Context {
        get => Holder.Value?.Context ?? throw new InvalidOperationException("Context is not set for the current async flow.");
        set {
            var holder = Holder.Value;
            if (holder != null) {
                holder.Context = null;
            }

            if (value != null) {
                Holder.Value = new ContextHolder { Context = value };
            }
        }
    }

    private class ContextHolder {
        public IContext? Context;
    }
}
