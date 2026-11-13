using System.Text.Json.Serialization;
using AppliLeCrocodile.Services;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.Infrastructure.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ReviewApiSettings))]
[JsonSerializable(typeof(SubmitReviewRequest))]
[JsonSerializable(typeof(ReviewResponse))]
[JsonSerializable(typeof(ReviewSummary[]))]
[JsonSerializable(typeof(ReviewPage))]
internal sealed partial class ReviewApiJsonContext : JsonSerializerContext
{
}
