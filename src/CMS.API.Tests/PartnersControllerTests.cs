using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class PartnersControllerTests
{
    private static FakePartnerRepository SeededRepo() => new(
        new Partner { Pkid = 1, Name = "台灣微軟", AppKey = "MS", NameOnPartnerMenu = "台灣微軟合作課程", NameOnCourseDetailPage = "台灣微軟", DisplayOrder = 1, ImageFilename = "ms.png" },
        new Partner { Pkid = 2, Name = "甲骨文", AppKey = "ORACLE", NameOnPartnerMenu = "甲骨文原廠課程", NameOnCourseDetailPage = "甲骨文", DisplayOrder = 2, ImageFilename = null },
        new Partner { Pkid = 3, Name = "思科", AppKey = "CISCO", NameOnPartnerMenu = "思科網路課程", NameOnCourseDetailPage = "思科", DisplayOrder = 3, ImageFilename = "cisco.png" });

    private static PartnersController Controller(FakePartnerRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllPartners()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var partners = Assert.IsAssignableFrom<IEnumerable<Partner>>(ok.Value);
        Assert.Equal(3, partners.Count());
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_ByKeyword_MatchesName()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PartnerQuery { Keyword = "甲骨文" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var partners = Assert.IsAssignableFrom<IEnumerable<Partner>>(ok.Value).ToList();
        Assert.Single(partners);
        Assert.Equal((short)2, partners[0].Pkid);
    }

    [Fact]
    public async Task Query_ByKeyword_MatchesAppKey()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PartnerQuery { Keyword = "CISCO" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var partners = Assert.IsAssignableFrom<IEnumerable<Partner>>(ok.Value).ToList();
        Assert.Single(partners);
        Assert.Equal("思科", partners[0].Name);
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAllOrderedByDisplayOrder()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PartnerQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var partners = Assert.IsAssignableFrom<IEnumerable<Partner>>(ok.Value).ToList();
        Assert.Equal(3, partners.Count);
        Assert.Equal(new short[] { 1, 2, 3 }, partners.Select(p => p.Pkid));
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingPartner_ReturnsPartner()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var partner = Assert.IsType<Partner>(ok.Value);
        Assert.Equal("甲骨文", partner.Name);
    }

    [Fact]
    public async Task GetById_MissingPartner_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_NewPartner_ReturnsCreatedWithAssignedPkid()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new PartnerRequest
        {
            Name = "紅帽",
            AppKey = "REDHAT",
            NameOnPartnerMenu = "紅帽 Linux 課程",
            NameOnCourseDetailPage = "紅帽",
            DisplayOrder = 4,
            ImageFilename = "redhat.png",
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var partner = Assert.IsType<Partner>(created.Value);
        Assert.Equal((short)4, partner.Pkid);
        Assert.NotNull(await repo.GetByIdAsync(4));
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingPartner_ReturnsUpdatedFields()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new PartnerRequest
        {
            Pkid = 3,
            Name = "思科系統",
            AppKey = "CISCO",
            NameOnPartnerMenu = "思科系統網路課程",
            NameOnCourseDetailPage = "思科系統",
            DisplayOrder = 5,
            ImageFilename = "cisco.png",
        };

        var result = await controller.Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var partner = Assert.IsType<Partner>(ok.Value);
        Assert.Equal("思科系統", partner.Name);
        Assert.Equal(5, partner.DisplayOrder);
    }

    [Fact]
    public async Task Update_MissingPartner_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());
        var request = new PartnerRequest { Pkid = 99, Name = "Ghost", AppKey = "X", NameOnPartnerMenu = "G", NameOnCourseDetailPage = "G", DisplayOrder = 1 };

        var result = await controller.Update(request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingPartner_ReturnsNoContent()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingPartner_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
