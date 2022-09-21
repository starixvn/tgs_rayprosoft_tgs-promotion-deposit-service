namespace Tgs.Promotion.Deposit.Service.MessageReply;

public record DepositMessageData(
    Guid BrandId,
    Guid? PromotionId,
    string PlayerCode,
    Guid DefaultWalletId,
    decimal DepositAmount,
    string Remark,
    Guid FormId,
    Guid? UserId);