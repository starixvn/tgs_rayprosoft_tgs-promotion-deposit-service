namespace Tgs.Promotion.Deposit.Service.Model
{
    public record DepositPromotion(
        Guid BrandId,
        Guid? PromotionId,
        string PlayerCode,
        Guid DefaultWalletId,
        decimal DepositAmount,
        string? Remark,
        Guid FormId,
        Guid? UserId);
}
