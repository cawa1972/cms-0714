using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class FeaturedPromoItemsControllerTests
{
    // Week under test: Monday 2026-03-16 … Sunday 2026-03-22.
    private static readonly DateOnly Monday = new(2026, 3, 16);
    private static readonly DateOnly Sunday = new(2026, 3, 22);

    private static FeaturedPromoItem Item(int pkid, DateOnly scheduleOn, short center, byte slot,
        int promotionPkid, string promoCode, string topic = "主題", string description = "說明") => new()
    {
        Pkid = pkid,
        ScheduleOn = scheduleOn,
        TrainingCenterPkid = center,
        Slot = slot,
        PromotionPkid = promotionPkid,
        Topic = topic,
        Description = description,
        PromoCode = promoCode,
        TrainingCenterName = center == 1 ? "台北" : "台中",
    };

    private static PromoCodeLookup Promo(int pkid, string code, string topic = "促銷主題", string description = "促銷說明")
        => new() { PromotionPkid = pkid, PromoCode = code, Topic = topic, Description = description };

    private static FakeFeaturedPromoItemRepository SeededRepo() => new(
        items:
        [
            // Selected week, center 1: full Monday (slots 1–3).
            Item(1, Monday, center: 1, slot: 1, promotionPkid: 10, promoCode: "20251204_SkillTrainAI", topic: "成為能AI協作的程式設計師"),
            Item(2, Monday, center: 1, slot: 2, promotionPkid: 11, promoCode: "251211_GoogleAI", topic: "Google AI工具一次掌握"),
            Item(3, Monday, center: 1, slot: 3, promotionPkid: 12, promoCode: "20251215_n8n", topic: "n8n自動化三部曲"),
            // Selected week, other center.
            Item(4, Monday.AddDays(1), center: 2, slot: 1, promotionPkid: 10, promoCode: "20251204_SkillTrainAI"),
            // Outside the week (previous Sunday / next Monday).
            Item(5, Monday.AddDays(-1), center: 1, slot: 1, promotionPkid: 11, promoCode: "251211_GoogleAI"),
            Item(6, Sunday.AddDays(1), center: 1, slot: 1, promotionPkid: 12, promoCode: "20251215_n8n"),
        ],
        promotions:
        [
            Promo(10, "20251204_SkillTrainAI", "成為能AI協作的程式設計師", "轉職就業養成班"),
            Promo(11, "251211_GoogleAI", "Google AI工具一次掌握", "不需技術基礎"),
            Promo(12, "20251215_n8n", "n8n自動化三部曲", "從自動化新手到企業級AI架構師"),
        ]);

    private static FeaturedPromoItemRequest NewRequest(int pkid = 0, byte slot = 1) => new()
    {
        Pkid = pkid,
        ScheduleOn = Monday.AddDays(2), // Wednesday — empty in the seed
        TrainingCenterPkid = 1,
        Slot = slot,
        PromotionPkid = 11,
        Topic = "Google AI工具一次掌握",
        Description = "不需技術基礎！最新Google AI實戰課程",
    };

    private static FeaturedPromoItemsController Controller(FakeFeaturedPromoItemRepository repo)
        => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllItems()
    {
        var result = await Controller(SeededRepo()).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(6, Assert.IsAssignableFrom<IEnumerable<FeaturedPromoItem>>(ok.Value).Count());
    }

    // ---- One-week ScheduleOn filter -------------------------------------

    [Fact]
    public async Task Query_ByWeekRange_ExcludesDaysOutsideMondayToSunday()
    {
        var result = await Controller(SeededRepo()).Query(new FeaturedPromoItemQuery
        {
            ScheduleOnFrom = Monday,
            ScheduleOnTo = Sunday,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<FeaturedPromoItem>>(ok.Value).ToList();
        Assert.Equal(4, items.Count);                       // pkids 1–4; 5 and 6 fall outside
        Assert.All(items, i => Assert.InRange(i.ScheduleOn, Monday, Sunday));
    }

    [Fact]
    public async Task Query_ByWeekRange_IncludesBothBoundaryDays()
    {
        var repo = SeededRepo();
        // Occupy the Sunday boundary too.
        await repo.CreateAsync(new FeaturedPromoItemRequest
        {
            ScheduleOn = Sunday,
            TrainingCenterPkid = 1,
            Slot = 1,
            PromotionPkid = 10,
            Topic = "週日主題",
            Description = "週日說明",
        });

        var result = await Controller(repo).Query(new FeaturedPromoItemQuery
        {
            ScheduleOnFrom = Monday,
            ScheduleOnTo = Sunday,
            TrainingCenterPkid = 1,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<FeaturedPromoItem>>(ok.Value).ToList();
        Assert.Contains(items, i => i.ScheduleOn == Monday);
        Assert.Contains(items, i => i.ScheduleOn == Sunday);
    }

    // ---- TrainingCenter filter ------------------------------------------

    [Fact]
    public async Task Query_ByTrainingCenter_ReturnsOnlyThatCenter()
    {
        var result = await Controller(SeededRepo()).Query(new FeaturedPromoItemQuery
        {
            TrainingCenterPkid = 2,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<FeaturedPromoItem>>(ok.Value).ToList();
        Assert.Single(items);
        Assert.Equal(4, items[0].Pkid);
    }

    [Fact]
    public async Task Query_WeekAndCenterCombined_MatchesTheBoardLoad()
    {
        var result = await Controller(SeededRepo()).Query(new FeaturedPromoItemQuery
        {
            ScheduleOnFrom = Monday,
            ScheduleOnTo = Sunday,
            TrainingCenterPkid = 1,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<FeaturedPromoItem>>(ok.Value).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, items.Select(i => i.Pkid));
        Assert.Equal(new byte[] { 1, 2, 3 }, items.Select(i => i.Slot));
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAll()
    {
        var result = await Controller(SeededRepo()).Query(new FeaturedPromoItemQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(6, Assert.IsAssignableFrom<IEnumerable<FeaturedPromoItem>>(ok.Value).Count());
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingItem_ReturnsItem()
    {
        var result = await Controller(SeededRepo()).GetById(3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var item = Assert.IsType<FeaturedPromoItem>(ok.Value);
        Assert.Equal("20251215_n8n", item.PromoCode);
    }

    [Fact]
    public async Task GetById_MissingItem_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_EmptySlot_ReturnsCreatedWithDatabaseAssignedPkid()
    {
        var repo = SeededRepo();

        var result = await Controller(repo).Create(NewRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var item = Assert.IsType<FeaturedPromoItem>(created.Value);
        Assert.Equal(7, item.Pkid);
        Assert.Equal("251211_GoogleAI", item.PromoCode); // resolved from PromotionPkid 11
        Assert.NotNull(await repo.GetByIdAsync(7));
    }

    [Fact]
    public async Task Create_OccupiedSlot_ReturnsConflict()
    {
        var request = NewRequest();
        request.ScheduleOn = Monday; // Monday/center 1/slot 1 is taken by pkid 1
        request.Slot = 1;

        var result = await Controller(SeededRepo()).Create(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingItem_ReturnsUpdatedFields()
    {
        var request = NewRequest(pkid: 1);
        request.ScheduleOn = Monday;
        request.Slot = 1;
        request.Topic = "改版主題";

        var result = await Controller(SeededRepo()).Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("改版主題", Assert.IsType<FeaturedPromoItem>(ok.Value).Topic);
    }

    [Fact]
    public async Task Update_ToOccupiedSlot_ReturnsConflict()
    {
        var request = NewRequest(pkid: 1);
        request.ScheduleOn = Monday;
        request.Slot = 2; // taken by pkid 2

        var result = await Controller(SeededRepo()).Update(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_MissingItem_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).Update(NewRequest(pkid: 99));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        var result = await Controller(SeededRepo()).Delete(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingItem_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Move (+ / −) ----------------------------------------------------

    [Fact]
    public async Task Move_Down_SwapsWithOccupantOfTargetSlot()
    {
        var repo = SeededRepo();

        var result = await Controller(repo).Move(1, "down");

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(2, (await repo.GetByIdAsync(1))!.Slot); // 1 → 2
        Assert.Equal(1, (await repo.GetByIdAsync(2))!.Slot); // swapped back 2 → 1
    }

    [Fact]
    public async Task Move_UpIntoEmptySlot_JustMoves()
    {
        var repo = SeededRepo();

        var result = await Controller(repo).Move(4, "down"); // center 2 Tuesday: slot 2 is empty

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(2, (await repo.GetByIdAsync(4))!.Slot);
    }

    [Fact]
    public async Task Move_UpFromSlot1_ReturnsBadRequest()
    {
        var result = await Controller(SeededRepo()).Move(1, "up");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Move_DownFromSlot3_ReturnsBadRequest()
    {
        var result = await Controller(SeededRepo()).Move(3, "down");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Move_InvalidDirection_ReturnsBadRequest()
    {
        var result = await Controller(SeededRepo()).Move(1, "sideways");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Move_MissingItem_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).Move(99, "down");

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- PromoCode lookup ------------------------------------------------

    [Fact]
    public async Task PromoLookup_KnownCode_ReturnsPkidAndDefaultTexts()
    {
        var result = await Controller(SeededRepo()).PromoLookup("20251215_n8n");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var promo = Assert.IsType<PromoCodeLookup>(ok.Value);
        Assert.Equal(12, promo.PromotionPkid);
        Assert.Equal("n8n自動化三部曲", promo.Topic);
    }

    [Fact]
    public async Task PromoLookup_TrimsWhitespaceBeforeMatching()
    {
        var result = await Controller(SeededRepo()).PromoLookup("  251211_GoogleAI  ");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(11, Assert.IsType<PromoCodeLookup>(ok.Value).PromotionPkid);
    }

    [Fact]
    public async Task PromoLookup_UnknownCode_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).PromoLookup("NOPE");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PromoLookup_BlankCode_ReturnsBadRequest()
    {
        var result = await Controller(SeededRepo()).PromoLookup("   ");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
