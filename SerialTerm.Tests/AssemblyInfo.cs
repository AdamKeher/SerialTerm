using Xunit;

// SerialTerm is one terminal program holding its state in static fields - the
// port, the output stream, the view flags. That is right for what it is, but it
// means two test classes running at once fight over the same globals, which
// showed up as different tests failing on each run. Tests run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
