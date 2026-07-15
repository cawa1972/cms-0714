using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class CoursesControllerTests
{
    private static Course SampleCourse(int pkid, string courseId, string title, short partner = 1,
        byte publishStatus = 1, bool canRepeat = false) => new()
    {
        Pkid = pkid,
        Title = title,
        CourseId = courseId,
        ProdCourseId = $"PROD-{courseId}",
        FriendlyUrl = courseId.ToLowerInvariant(),
        DisplayOrder = pkid,
        PartnerPkid = partner,
        CourseGroupPkid = null,
        PublishStatusPkid = publishStatus,
        ScheduleOn = new DateOnly(2026, 1, 1),
        ScheduleOff = new DateOnly(2036, 1, 1),
        Hour = 8,
        ListPrice = 12000m,
        LearningCredit = 3.5m,
        CanRepeat = canRepeat,
    };

    private static FakeCourseRepository SeededRepo() => new(
        SampleCourse(1, "AZ-900", "Azure 基礎", partner: 1, publishStatus: 1, canRepeat: true),
        SampleCourse(2, "AWS-SAA", "AWS 架構師", partner: 2, publishStatus: 2, canRepeat: false),
        SampleCourse(3, "RHCSA", "紅帽系統管理", partner: 2, publishStatus: 1, canRepeat: false));

    private static CourseRequest NewRequest(int pkid = 0) => new()
    {
        Pkid = pkid,
        Title = "Kubernetes 入門",
        CourseId = "K8S-101",
        ProdCourseId = "PROD-K8S-101",
        FriendlyUrl = "k8s-101",
        DisplayOrder = 5,
        PartnerPkid = 1,
        CourseGroupPkid = null,
        PublishStatusPkid = 1,
        ScheduleOn = new DateOnly(2026, 3, 1),
        ScheduleOff = new DateOnly(2036, 3, 1),
        Hour = 16,
        ListPrice = 20000m,
        LearningCredit = 5.0m,
        CanRepeat = true,
    };

    private static CoursesController Controller(FakeCourseRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllCourses()
    {
        var result = await Controller(SeededRepo()).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(3, Assert.IsAssignableFrom<IEnumerable<Course>>(ok.Value).Count());
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_ByKeyword_MatchesCourseId()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { Keyword = "AWS" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var courses = Assert.IsAssignableFrom<IEnumerable<Course>>(ok.Value).ToList();
        Assert.Single(courses);
        Assert.Equal(2, courses[0].Pkid);
    }

    [Fact]
    public async Task Query_ByPartnerPkid_FiltersOnFk()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { PartnerPkid = 2 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<Course>>(ok.Value).Count());
    }

    [Fact]
    public async Task Query_ByCanRepeat_FiltersOnBit()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery { CanRepeat = true });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var courses = Assert.IsAssignableFrom<IEnumerable<Course>>(ok.Value).ToList();
        Assert.Single(courses);
        Assert.Equal("AZ-900", courses[0].CourseId);
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAll()
    {
        var result = await Controller(SeededRepo()).Query(new CourseQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(3, Assert.IsAssignableFrom<IEnumerable<Course>>(ok.Value).Count());
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingCourse_ReturnsCourse()
    {
        var result = await Controller(SeededRepo()).GetById(2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("AWS 架構師", Assert.IsType<Course>(ok.Value).Title);
    }

    [Fact]
    public async Task GetById_MissingCourse_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_NewCourse_ReturnsCreatedWithDatabaseAssignedPkid()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);

        var result = await controller.Create(NewRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var course = Assert.IsType<Course>(created.Value);
        Assert.Equal(4, course.Pkid);
        Assert.Equal("Kubernetes 入門", course.Title);
        Assert.NotNull(await repo.GetByIdAsync(4));
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingCourse_ReturnsUpdatedFields()
    {
        var controller = Controller(SeededRepo());
        var request = NewRequest(pkid: 3);
        request.Title = "紅帽系統管理（改版）";

        var result = await controller.Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("紅帽系統管理（改版）", Assert.IsType<Course>(ok.Value).Title);
    }

    [Fact]
    public async Task Update_MissingCourse_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).Update(NewRequest(pkid: 99));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingCourse_ReturnsNoContent()
    {
        var result = await Controller(SeededRepo()).Delete(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingCourse_ReturnsNotFound()
    {
        var result = await Controller(SeededRepo()).Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
