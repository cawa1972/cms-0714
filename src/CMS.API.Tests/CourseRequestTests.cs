using System.ComponentModel.DataAnnotations;
using CMS.API.Models;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Regression: ISSUE-001 — ScheduleOff before ScheduleOn saved with no validation error.
/// Found by /qa on 2026-07-17
/// Report: .gstack/qa-reports/qa-report-cms-2026-07-17.md
/// </summary>
public class CourseRequestTests
{
    private static CourseRequest RequestWith(DateOnly on, DateOnly off) => new()
    {
        Title = "Kubernetes 入門",
        CourseId = "K8S-101",
        ProdCourseId = "PROD-K8S-101",
        FriendlyUrl = "k8s-101",
        PartnerPkid = 1,
        PublishStatusPkid = 1,
        ScheduleOn = on,
        ScheduleOff = off,
        Hour = 16,
        ListPrice = 20000m,
        LearningCredit = 5.0m,
    };

    private static IList<ValidationResult> Validate(CourseRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ScheduleOffBeforeScheduleOn_IsInvalid()
    {
        var request = RequestWith(new DateOnly(2026, 3, 1), new DateOnly(2020, 3, 1));

        var results = Validate(request);

        var result = Assert.Single(results);
        Assert.Contains(nameof(CourseRequest.ScheduleOff), result.MemberNames);
    }

    [Fact]
    public void ScheduleOffAfterScheduleOn_IsValid()
    {
        var request = RequestWith(new DateOnly(2026, 3, 1), new DateOnly(2036, 3, 1));

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void ScheduleOffEqualsScheduleOn_IsValid()
    {
        var sameDay = new DateOnly(2026, 3, 1);
        var request = RequestWith(sameDay, sameDay);

        Assert.Empty(Validate(request));
    }
}
