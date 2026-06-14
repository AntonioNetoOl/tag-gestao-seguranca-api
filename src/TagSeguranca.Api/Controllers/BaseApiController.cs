using Microsoft.AspNetCore.Mvc;
using TagSeguranca.Api.Application.Common.Responses;

namespace TagSeguranca.Api.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected ActionResult ApiBadRequest(string mensagem, object? detalhes = null)
    {
        return BadRequest(ApiErrorResponse.Criar(mensagem, detalhes));
    }

    protected ActionResult ApiNotFound(string mensagem, object? detalhes = null)
    {
        return NotFound(ApiErrorResponse.Criar(mensagem, detalhes));
    }

    protected ActionResult ApiConflict(string mensagem, object? detalhes = null)
    {
        return Conflict(ApiErrorResponse.Criar(mensagem, detalhes));
    }
}