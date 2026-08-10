using Xunit;

// Every integration test class boots its own API host, and each host migrates (and in
// Development seeds) the database on start-up. Running those classes in parallel means several
// hosts hitting one database at once, which on a *fresh* database races: duplicate CREATE TABLE
// and duplicate demo users. DbSeeder now serialises that work behind an advisory lock; this
// keeps the suite single-file as well, so hosts start one at a time and a failure points at the
// test rather than at start-up contention. The suite runs in ~16s, so there is nothing to gain
// from parallelism here.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
