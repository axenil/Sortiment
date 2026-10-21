using System.Globalization;

namespace Sortiment.Core.Tests;

public sealed class ResultTests
{
    private static readonly ResultError FirstError = new("test.first", "First error.");
    private static readonly ResultError SecondError = new("test.second", "Second error.");

    [Fact]
    public void SuccessHasNoErrors()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void FailureKeepsAllErrorsInOrder()
    {
        var result = Result.Failure([FirstError, SecondError]);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldBe([FirstError, SecondError]);
    }

    [Fact]
    public void FailureWithoutErrorsIsRejected()
    {
        Should.Throw<ArgumentException>(() => Result.Failure([]));
        Should.Throw<ArgumentException>(() => Result.Failure<int>([]));
    }

    [Fact]
    public void FailureWithNullErrorIsRejected()
    {
        Should.Throw<ArgumentException>(() => Result.Failure([FirstError, null!]));
    }

    [Fact]
    public void SuccessWithValueExposesTheValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ReadingTheValueOfAFailureThrows()
    {
        var result = Result.Failure<int>(FirstError);

        var exception = Should.Throw<InvalidOperationException>(() => result.Value);
        exception.Message.ShouldContain("test.first");
    }

    [Fact]
    public void MapTransformsTheValueOfASuccess()
    {
        var result = Result.Success(21).Map(value => value * 2);

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void MapKeepsTheErrorsOfAFailureWithoutCallingTheTransformation()
    {
        var called = false;

        var result = Result.Failure<int>([FirstError, SecondError]).Map(value =>
        {
            called = true;
            return value.ToString(CultureInfo.InvariantCulture);
        });

        called.ShouldBeFalse();
        result.Errors.ShouldBe([FirstError, SecondError]);
    }
}
