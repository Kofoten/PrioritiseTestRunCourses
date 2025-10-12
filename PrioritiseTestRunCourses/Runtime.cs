using Microsoft.Extensions.Logging;
using PrioritiseTestRunCourses.Data;
using PrioritiseTestRunCourses.Extensions;
using PrioritiseTestRunCourses.Logging;
using PrioritiseTestRunCourses.Xml;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace PrioritiseTestRunCourses;

internal class Runtime(Options options, ILogger logger)
{
    public Result<Unit, ErrorCode> Run()
    {
        var iofReader = IOFXmlReader.Create();
        if (!iofReader.TryLoad(options.IOFXmlFilePath, out var courseData, out var errors))
        {
            logger.FailedToLoadFile(options.IOFXmlFilePath, errors.FormatErrors());
            return new Failure<Unit, ErrorCode>(ErrorCode.FailedToLoadFile);
        }

        var courses = courseData.RaceCourseData
            .SelectMany(x => x.Course, (_, y) => Course.FromIOF(y))
            .Where(x => options.Filters.Count == 0 || options.Filters.Any(y => x.Name.Contains(y)))
            .Where(x => x.Controls.Count > 0)
            .ToFrozenSet();

        var coursesInvertedIndex = courses
            .SelectMany(x => x.Controls, (x, y) => (ControlCode: y, Course: x))
            .ToLookup(x => x.ControlCode, x => x.Course);

        var controlRarityLookup = coursesInvertedIndex
            .ToFrozenDictionary(x => x.Key, x => 1F / x.Count());

        var dominatedCourses = courses
            .Where(x =>
            {
                var rarestControl = x.Controls.MaxBy(x => controlRarityLookup[x]);
                if (rarestControl is null)
                {
                    return true;
                }

                return coursesInvertedIndex[rarestControl]
                    .Where(y => y.Name != x.Name && y.Controls.Count > x.Controls.Count)
                    .Any(y => x.Controls.IsSubsetOf(y.Controls));
            })
            .Select(x => x.Name)
            .ToFrozenSet();

        var availableCourses = courses
            .Where(x => !dominatedCourses.Contains(x.Name))
            .ToFrozenSet();

        var requiredCourses = FindRequiredCourseOrder(availableCourses, controlRarityLookup);
        if (requiredCourses is null)
        {
            logger.NoSolutionFound();
            return new Failure<Unit, ErrorCode>(ErrorCode.NoSolutionFound);
        }

        string[] completeOrder = [
            .. requiredCourses,
            .. availableCourses
                .Where(x => !requiredCourses.Contains(x.Name))
                .OrderByDescending(x => x.Controls.Count)
                .ThenBy(x => x.Name)
                .Select(x => x.Name),
            .. dominatedCourses.Order(),
        ];

        for (int i = 0; i < completeOrder.Length; i++)
        {
            if (i < requiredCourses.Count)
            {
                Console.WriteLine($"{completeOrder[i]} (required)");
            }
            else
            {
                Console.WriteLine(completeOrder[i]);
            }
        }

        return new Success<Unit, ErrorCode>(Unit.Value);
    }

    private ImmutableList<string>? FindRequiredCourseOrder(FrozenSet<Course> courses, FrozenDictionary<string, float> controlRarityLookup)
    {
        List<CandidateSolution> beam = [
            new CandidateSolution([], [.. controlRarityLookup.Keys])
        ];

        var comparer = new CandidateSolution.RarityPriorityComparer(controlRarityLookup, 1F);
        while (beam.Count > 0)
        {
            var topCandidates = new PriorityQueue<CandidateSolution, CandidateSolution>(comparer);
            var partitionedBeam = beam.ToLookup(c => c.IsComplete);
            var expandedSolutions = partitionedBeam[false].SelectMany(
                x => courses.Where(y => !x.CourseOrder.Contains(y.Name)),
                (x, y) => new CandidateSolution(
                    x.CourseOrder.Add(y.Name),
                    x.UnvisitedControls.Except(y.Controls)));

            foreach (var candidate in partitionedBeam[true])
            {
                topCandidates.Enqueue(candidate, candidate);
            }

            foreach (var candidate in expandedSolutions)
            {
                topCandidates.Enqueue(candidate, candidate);
            }

            beam = [];
            while (beam.Count < options.BeamWidth && topCandidates.Count > 0)
            {
                beam.Add(topCandidates.Dequeue());
            }

            if (beam.Count > 0 && beam[0].IsComplete)
            {
                break;
            }
        }

        if (beam.Count == 0)
        {
            return null;
        }

        return beam[0].CourseOrder;
    }
}
