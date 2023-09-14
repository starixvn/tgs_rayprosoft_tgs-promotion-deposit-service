using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.PlayerBucket.Client;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.Deposit.Core.UnitTests;

public class DepositRewardTests
{
	[Test]
	public async Task Deposit()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();

		var sut = new SendDepositRewardPromotionService(
			fakePromotionRepository,
			fakePromotionBonusRepository,
			fakePromotionServiceClient,
			fakePaymentServiceClient,
			fakePlayerBucketClient,
			NullLogger<SendDepositRewardPromotionService>.Instance);

		var rule = new DepositRewardRule(100, 10, 5, 100, null);

		var promotion = new Promotion
		{
			Unlimited = 1,
			TotalAmountLimit = 0,
			CampaignDetail = JsonSerializer.Serialize(rule)
		};

		var depositForm = new DepositFormInfo
		{
			ActualAmount = 100,
			Amount = 100
		};

		_ = fakePromotionRepository.GetPromotionWithCacheAsync(Arg.Any<Guid>()).Returns(promotion);
		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Any<Guid>()).Returns(depositForm);

		var command = new SendDepositRewardPromotion(
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
			.CreatePromotionBonusAsync(Arg.Is(promotion), Arg.Is(command.PlayerCode), Arg.Is((decimal)33.3),
				Arg.Is((decimal)1498.5), default);
	}

	[Test]
	public async Task Add_Player_Bucket()
	{
		// Arrange
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();

		var sut = new SendDepositRewardPromotionService(
			fakePromotionRepository,
			fakePromotionBonusRepository,
			fakePromotionServiceClient,
			fakePaymentServiceClient,
			fakePlayerBucketForTypesClient,
			NullLogger<SendDepositRewardPromotionService>.Instance);

		var rule = new DepositRewardRule(100, 10, 5, 100, "Sportsbook");

		var promotion = new Promotion
		{
			Unlimited = 1,
			TotalAmountLimit = 0,
			AutoSentPlayerBonus = true,
			CampaignDetail = JsonSerializer.Serialize(rule)
		};

		var depositForm = new DepositFormInfo
		{
			ActualAmount = 100,
			Amount = 100
		};

		_ = fakePromotionRepository.GetPromotionWithCacheAsync(Arg.Any<Guid>()).Returns(promotion);
		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Any<Guid>()).Returns(depositForm);

		var command = new SendDepositRewardPromotion(
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
		await fakePlayerBucketForTypesClient.ReceivedWithAnyArgs(1)
			.CreateBucketForBucketTypesAsync(null, 0, Service.PlayerBuckets.BucketType.None, null, null);
	}
}