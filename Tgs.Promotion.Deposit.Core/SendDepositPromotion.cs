namespace Tgs.Promotion.Deposit.Core;

public record SendDepositPromotion(
    Guid BrandId,
    Guid? PromotionId,
    string PlayerCode,
    Guid DefaultWalletId,
    decimal DepositAmount,
    string Remark,
    Guid FormId,
    Guid? UserId);