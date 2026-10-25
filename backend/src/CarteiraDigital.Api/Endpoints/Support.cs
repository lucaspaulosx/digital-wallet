using System.Security.Claims;

namespace CarteiraDigital.Api.Endpoints;

public static class Support
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse((principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier))!);

    public static IResult Problem(int status, string title, string detail) =>
        Results.Problem(statusCode: status, title: title, detail: detail);
}
