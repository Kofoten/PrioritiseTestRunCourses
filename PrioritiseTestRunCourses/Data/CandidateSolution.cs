using System.Collections.Frozen;

namespace PrioritiseTestRunCourses.Data;

internal record CandidateSolution(FrozenSet<string> CourseOrder, FrozenSet<string> UnvisitedControls)
{
    public bool IsComplete => UnvisitedControls.Count == 0;

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

            return x.CourseOrder.Count.CompareTo(y.CourseOrder.Count);
        }
    }
}
