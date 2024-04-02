using Tgs.Promotion.Deposit.Core;
using Tgs.Promotion.Deposit.Repository.Entities;

namespace Tgs.Promotion.Deposit.Repository;

internal class TgsRepository(TgsContext dbContext)
	: ITgsRepository
{
	public async ValueTask<Guid?> GetPromotionCalculateIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var row = await dbContext.DepositPromotionCalculatePairs.FindAsync(
			[promotionId],
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return row?.CalculateId;
	}

	public async ValueTask SetPromotionCalculateIdAsync(Guid promotionId, Guid calculateId, CancellationToken cancellationToken = default)
	{
		_ = await dbContext.DepositPromotionCalculatePairs
			.AddAsync(
				new DepositPromotionCalculatePair
				{
					PromotionId = promotionId,
					CalculateId = calculateId
				},
				cancellationToken)
			.ConfigureAwait(false);

		_ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
