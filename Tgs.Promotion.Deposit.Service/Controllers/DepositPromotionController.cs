using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.MessageQueue;
using Tgs.Promotion.Deposit.Service.Model;

namespace Tgs.Promotion.Deposit.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepositPromotionController : ControllerBase
{
    private readonly IMessageSender _messageSender;

    public DepositPromotionController(IMessageSender messageSender)
    {
        _messageSender = messageSender;
    }

    [HttpGet]
    [Description("deposit")]
    public async Task<string> SendDepositPromotionAsync([FromQuery] DepositPromotion request)
    {
        var response = await _messageSender!
            .RequestAsync("tgs.promotion.dailyfirstdeposit.distribute",
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))
            .ConfigureAwait(false);

        return Encoding.UTF8.GetString(response.Span);
    }

    [HttpGet("gift")]
    [Description("deposit gift")]
    public async Task<string> SendDepositGiftPromotionAsync([FromQuery] DepositPromotion request)
    {
        var response = await _messageSender!
            .RequestAsync("tgs.promotion.depositreward.distribute",
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))
            .ConfigureAwait(false);

        return Encoding.UTF8.GetString(response.Span);
    }
}