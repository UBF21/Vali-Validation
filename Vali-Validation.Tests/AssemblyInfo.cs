using Xunit;

// Several tests mutate process-wide, unsynchronized mutable statics (ValiValidationOptions.Global.*,
// CultureInfo.CurrentUICulture) and restore them afterward. xUnit runs different test classes as
// separate collections in parallel by default, so a validator running concurrently in another class
// could observe another test's mid-flight mutation — flaky, timing-dependent failures. Disabling
// collection parallelism removes that race for the whole assembly, present and future.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
