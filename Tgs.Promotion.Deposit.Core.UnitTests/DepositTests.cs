//using System.Text.Json;
//using Microsoft.Extensions.Logging.Abstractions;
//using NSubstitute;
//using Tgs.PlayerBucket.Client;

//namespace Tgs.Promotion.Deposit.Core.UnitTests;

//public class DepositTests
//{
//    [Test]
//    public async Task Deposit()
//    {
//        // Arrange
//        var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
//        var fakePromotionRepository = Substitute.For<IPromotionRepository>();
//        var fakePromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();

//        var sut = new SendDepositPromotionService(
//            fakePromotionRepository,
//            fakePromotionBonusRepository,
//            fakePlayerBucketForTypesClient,
//            NullLogger<SendDepositPromotionService>.Instance);

//        var rule = new DepositRule(new[]{ "Sportsbook" });

//        var promotion = new Promotion
//        {
//            Percentage = 10,
//            BonusAmountLimit = 100,
//            TurnoverMultiple = 5,
//            CampaignDetail = JsonSerializer.Serialize(rule)
//        };

//        _ = fakePromotionRepository.GetPromotionWithCacheAsync(Arg.Any<Guid>()).Returns(promotion);

//        var command = new SendDepositPromotion(
//            Guid.NewGuid(),
//            Guid.NewGuid(),
//            "playerCode",
//            Guid.NewGuid(),
//            333,
//            "",
//            Guid.NewGuid(),
//            null);

//        // Act
//        _ = await sut.GetAsync(command);
//        // Assert
//        _ = await fakePromotionBonusRepository.Received(1)
//            .CreatePromotionBonusAsync(Arg.Is(promotion),
//                Arg.Is(command.PlayerCode),
//                Arg.Is((decimal)33.3),
//                Arg.Is((decimal)1498.5),
//                default);
//    }

//    [Test]
//    public async Task Add_Player_Bucket()
//    {
//        // Arrange
//        var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
//        var fakePromotionRepository = Substitute.For<IPromotionRepository>();
//        var fakePromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();

//        var sut = new SendDepositPromotionService(
//            fakePromotionRepository,
//            fakePromotionBonusRepository,
//            fakePlayerBucketForTypesClient,
//            NullLogger<SendDepositPromotionService>.Instance);

//        var rule = new DepositRule(new[] { "Sportsbook" });

//        var promotion = new Promotion
//        {
//            Percentage = 10,
//            BonusAmountLimit = 100,
//            TurnoverMultiple = 5,
//            AutoSentPlayerBonus = true,
//			CampaignDetail = JsonSerializer.Serialize(rule)
//        };

//        _ = fakePromotionRepository.GetPromotionWithCacheAsync(Arg.Any<Guid>()).Returns(promotion);

//        var command = new SendDepositPromotion(
//            Guid.NewGuid(),
//            Guid.NewGuid(),
//            "playerCode",
//            Guid.NewGuid(),
//            333,
//            "",
//            Guid.NewGuid(),
//            null);

//        // Act
//        _ = await sut.GetAsync(command);

//        // Assert
//        await fakePlayerBucketForTypesClient.ReceivedWithAnyArgs(1)
//            .CreateBucketForBucketTypesAsync("playerCode", 0, Service.PlayerBuckets.BucketType.None, null, null);
//    }
//}