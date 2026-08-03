using GharCraft.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace GharCraft.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        return result.IsSuccess ? new OkResult() : MatchError(result.Error);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess ? new OkObjectResult(result.Value) : MatchError(result.Error);
    }

    private static IActionResult MatchError(Error error)
    {
        var problemDetails = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description
        };

        return error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(problemDetails),
            ErrorType.Validation => new BadRequestObjectResult(problemDetails),
            ErrorType.Conflict => new ConflictObjectResult(problemDetails),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(problemDetails),
            ErrorType.Forbidden => new ObjectResult(problemDetails) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new BadRequestObjectResult(problemDetails)
        };
    }
}
