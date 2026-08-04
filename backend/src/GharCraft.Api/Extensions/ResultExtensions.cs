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
        return error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(Problem(error, StatusCodes.Status404NotFound)),
            ErrorType.Validation => new BadRequestObjectResult(Problem(error, StatusCodes.Status400BadRequest)),
            ErrorType.Conflict => new ConflictObjectResult(Problem(error, StatusCodes.Status409Conflict)),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(Problem(error, StatusCodes.Status401Unauthorized)),
            ErrorType.Forbidden => new ObjectResult(Problem(error, StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new BadRequestObjectResult(Problem(error, StatusCodes.Status400BadRequest))
        };
    }

    private static ProblemDetails Problem(Error error, int status) => new()
    {
        Title = error.Code,
        Detail = error.Description,
        Status = status
    };
}
