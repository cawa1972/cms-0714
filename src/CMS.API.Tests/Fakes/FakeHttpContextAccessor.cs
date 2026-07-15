using Microsoft.AspNetCore.Http;

namespace CMS.API.Tests.Fakes;

/// <summary>Hands a fixed (or absent) HttpContext to code that reads the current request's user.</summary>
public sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}
