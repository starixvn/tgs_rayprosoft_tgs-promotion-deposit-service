using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Calculate.Clients;
using Tgs.Payment.Clients;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Deposit.Core.ApplicationServices;

namespace Tgs.Promotion.Deposit.Core.UnitTests;

public class DepositRewardTests
{
	[Test]
	public async Task Deposit()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeCalculateClient = Substitute.For<ICalculateClient>();

		var sut = new DistributeDepositRewardBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeCalculateClient,
			NullLogger<DistributeDepositRewardBonusCommandService>.Instance);

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			PromotionType = "DepositReward"
		});

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<PromotionRule>(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns(new PromotionRule(new DepositRewardSetting(10, 10, 10, 10, [], [])));

		_ = fakePromotionServiceClient
			.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
			.Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10));

		_ = fakeTgsRepository.GetPromotionCalculateIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

		_ = fakeCalculateClient.ExchangeAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
			.Returns(new ExchangeResult(Guid.NewGuid(), 10));

		var command = new DistributeDepositRewardBonusCommand(
			new DepositFormInfo
			{
				Id = Guid.NewGuid(),
				ApplyAmount = 10,
				PlayerCode = "playerCode",
				PromotionId = Guid.NewGuid()
			});

		// Act
		await sut.ExecuteAsync(command);
		// Assert
		_ = fakePromotionServiceClient.ReceivedWithAnyArgs()
			.DistributePromotionBonusAsync(default, null, 0, 0, null, null, null, default);

		_ = fakePromotionServiceClient.ReceivedWithAnyArgs().UpdatePromotionUsedAmountAsync(default, 0, null, default);
	}
}