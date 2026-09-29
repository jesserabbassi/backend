using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinetyBackend.Modules.Wallets.DTOs;
using NinetyBackend.Modules.Wallets.Services;

namespace NinetyBackend.Modules.Wallets.Controllers;

[ApiController, Authorize, Route("api/wallet")]
public class WalletController(IWalletService walletService, ITransactionService transactions) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public Task<WalletResponseDto> Get() => walletService.GetOrCreateAsync(UserId);
    [HttpGet("transactions")] public Task<List<WalletTransactionResponseDto>> Transactions() => walletService.GetTransactionsAsync(UserId);
    [HttpPost("credit")] public Task<WalletTransactionResponseDto> Credit(WalletOperationDto dto, CancellationToken ct) => transactions.CreateAsync(UserId, dto.Amount, dto.IdempotencyKey, dto.Reason, false, cancellationToken: ct);
    [HttpPost("debit")] public Task<WalletTransactionResponseDto> Debit(WalletOperationDto dto, CancellationToken ct) => transactions.CreateAsync(UserId, dto.Amount, dto.IdempotencyKey, dto.Reason, true, cancellationToken: ct);
}
