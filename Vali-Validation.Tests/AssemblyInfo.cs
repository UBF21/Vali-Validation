using Xunit;

namespace Vali_Validation.Tests;

// Several test classes mutate process-wide, unsynchronized mutable statics
// (ValiValidationOptions.Global.*, CultureInfo.CurrentUICulture) and restore them afterward. xUnit
// runs different test classes as separate collections in parallel by default, so a validator running
// concurrently in another class could observe another test's mid-flight mutation — flaky,
// timing-dependent failures. Placing every class that touches this ambient state into one shared
// collection serializes only those classes against each other, leaving the rest of the (much larger)
// suite free to run in parallel.
[CollectionDefinition("Global state")]
public class GlobalStateCollection : ICollectionFixture<object>
{
}
