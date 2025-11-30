using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.RyanW84.Controllers;

public static class ControllerResponseExtensions
{
    public static IActionResult FromFailure(this ControllerBase controller, HttpStatusCode statusCode, string errorMessage)
    {
        if (statusCode == HttpStatusCode.NoContent)
        {
            return controller.NoContent();
        }

        var message = string.IsNullOrWhiteSpace(errorMessage)
            ? "Request could not be completed."
            : errorMessage;
        var payload = new { message };

        return statusCode switch
        {
            HttpStatusCode.BadRequest => controller.BadRequest(payload),
            HttpStatusCode.NotFound => controller.NotFound(payload),
            HttpStatusCode.Conflict => controller.Conflict(payload),
            HttpStatusCode.Unauthorized => controller.Unauthorized(payload),
            _ => controller.StatusCode((int)statusCode, payload)
        };
    }
}
