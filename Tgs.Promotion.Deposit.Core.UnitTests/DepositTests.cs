using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.Deposit.Core.UnitTests;

public class DepositTests
{
	[Test]
	public async Task Deposit()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();

		var sut = new SendDepositPromotionService(
			fakePromotionRepository,
			fakePromotionBonusRepository,
			fakePlayerBucketClient,
			NullLogger<SendDepositPromotionService>.Instance);

		var promotion = new Promotion
		{
			Percentage = 10,
			BonusAmountLimit = 100,
			TurnoverMultiple = 5
		};

		_ = fakePromotionRepository.GetPromotionWithCacheAsync(Arg.Any<Guid>()).Returns(promotion);

		var command = new SendDepositPromotion(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"playerCode",
			Guid.NewGuid(),
			333,
			"",
			Guid.NewGuid(),
			null);

		// Act
		_ = await sut.GetAsync(command);
		// Assert
		_ = fakePromotionBonusRepository.Received(1)
			.CreatePromotionBonusAsync(Arg.Is(promotion),Arg.Is(command.PlayerCode),Arg.Is((decimal)33.3),Arg.Is((decimal)1498.5), default);
	}
}