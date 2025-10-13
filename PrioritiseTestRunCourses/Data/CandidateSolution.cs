using System.Collections.Frozen;
using System.Collections.Immutable;

namespace PrioritiseTestRunCourses.Data;

internal record CandidateSolution(ImmutableDictionary<string, int> Courses, ImmutableHashSet<string> UnvisitedControls)
{
    public bool IsComplete => UnvisitedControls.Count == 0;

    public static CandidateSolution Initial(IEnumerable<string> unvisitedControls) => new(
        ImmutableDictionary<string, int>.Empty,
        [.. unvisitedControls]);

    internal class RarityPriorityComparer(
        FrozenDictionary<string, float> controlRarityLookup,
        float defaultRarity)
        : IComparer<CandidateSolution>
    {
        public int Compare(CandidateSolution? x, CandidateSolution? y)
        {
            if (x is null)
            {
                return y is null ? 0 : -1;
            }

            if (y is null)
            {
                return 1;
            }

            var raritySumX = x.UnvisitedControls.Sum(z => controlRarityLookup.GetValueOrDefault(z, defaultRarity));
            var raritySumY = y.UnvisitedControls.Sum(z => controlRarityLookup.GetValueOrDefault(z, defaultRarity));
            var rarityComparison = raritySumX.CompareTo(raritySumY);
            if (rarityComparison != 0)
            {
                return rarityComparison;
            }

            return x.Courses.Count.CompareTo(y.Courses.Count);
        }
    }
}
