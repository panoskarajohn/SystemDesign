using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Web.Extensions;

namespace Shared.Web.Context;

public interface IIdentityContext {
    bool IsAuthenticated { get; }
    public Guid Id { get; }
    string Role { get; }
    Dictionary<string, IEnumerable<string>> Claims { get; }
    bool IsUser();
    bool IsAdmin();
}

public class IdentityContext : IIdentityContext {
    public bool IsAuthenticated { get; }
    public Guid Id { get; }
    public string Role { get; } = "guest";
    public Dictionary<string, IEnumerable<string>> Claims { get; } = [];

    public IdentityContext(Guid? id) {
        Id = id ?? Guid.Empty;
        IsAuthenticated = id.HasValue;
    }

    public IdentityContext(ClaimsPrincipal principal) {
        if (principal is null || principal.Identity is null || string.IsNullOrWhiteSpace(principal.Identity.Name)) {
            return;
        }

        IsAuthenticated = principal.Identity?.IsAuthenticated is true;
        Id = IsAuthenticated ? Guid.Parse(principal.Identity!.Name) : Guid.Empty;
        Role = principal.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Role)?.Value ?? "guest";
        Claims = principal.Claims.GroupBy(x => x.Type)
            .ToDictionary(x => x.Key, x => x.Select(c => c.Value.ToString()));
    }

    public bool IsUser() => Role is "user";

    public bool IsAdmin() => Role is "admin";

}

public interface IContext {
    Guid RequestId { get; }
    Guid CorrelationId { get; }
    string TraceId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    IIdentityContext? Identity { get; }
}

public class Context : IContext {
    public Guid RequestId { get; } = Guid.NewGuid();
    public Guid CorrelationId { get; }
    public string TraceId { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public IIdentityContext? Identity { get; }

    public Context() : this(Guid.NewGuid(), $"{Guid.NewGuid():N}", null) {
    }

    public Context(HttpContext context) : this(context.TryGetCorrelationId(), context.TraceIdentifier,
        new IdentityContext(context.User), context.GetUserIpAddress(),
        context.Request.Headers["user-agent"]) {
    }

    public Context(Guid? correlationId, string traceId, IIdentityContext? identity = null, string? ipAddress = null,
        string? userAgent = null) {
        CorrelationId = correlationId ?? Guid.NewGuid();
        TraceId = traceId;
        Identity = identity;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public static IContext Empty => new Context();
}