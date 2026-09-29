using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Wallets.DTOs;
using NinetyBackend.Modules.Wallets.Services;

namespace NinetyBackend.Modules.Wallets.Controllers;

[ApiController, Authorize, Route("api/payment")]
public class PaymentController(IPaymentService payments) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost] public Task<WalletTransactionResponseDto> Process(PaymentDto dto, CancellationToken ct) => payments.ProcessAsync(UserId, dto, ct);
    [HttpPost("refund")] public Task<WalletTransactionResponseDto> Refund(RefundDto dto, CancellationToken ct) => payments.RefundAsync(UserId, dto, ct);
}
