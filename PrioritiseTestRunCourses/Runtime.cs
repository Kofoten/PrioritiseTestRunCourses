using Microsoft.Extensions.Logging;
using PrioritiseTestRunCourses.Data;
using PrioritiseTestRunCourses.Extensions;
using PrioritiseTestRunCourses.Logging;
using PrioritiseTestRunCourses.Xml;
using System.Collections.Frozen;

namespace PrioritiseTestRunCourses;

internal class Runtime(Options options, ILogger logger)
{
    public int Run()
    {
        var iofReader = IOFXmlReader.Create();
        if (!iofReader.TryLoad(options.IOFXmlFilePath, out var courseData, out var errors))
        {
            logger.FailedToLoadFile(options.IOFXmlFilePath, errors.FormatErrors());
            return 2;
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
                var rarestControl = x.Controls
                    .OrderByDescending(x => controlRarityLookup[x])
                    .First();

                return coursesInvertedIndex[rarestControl]
                    .Where(y => y.Name != x.Name && y.Controls.Count > x.Controls.Count)
                    .Any(y => x.Controls.IsSubsetOf(y.Controls));
            })
            .Select(x => x.Name)
            .ToFrozenSet();

        var availableCourses = courses
            .Where(x => !dominatedCourses.Contains(x.Name))
            .ToFrozenSet();

        List<CandidateSolution> beam = [
            new CandidateSolution(
                FrozenSet<string>.Empty,
                controlRarityLookup.Keys.ToFrozenSet())
        ];

        while (beam.Count > 0)
        {
            var comparer = new CandidateSolution.RarityPriorityComparer(controlRarityLookup, 1F);
            var topCandidates = new PriorityQueue<CandidateSolution, CandidateSolution>(comparer);
            var partitionedBeam = beam.ToLookup(c => c.IsComplete);
            var expandedSolutions = partitionedBeam[false].SelectMany(
                x => availableCourses.Where(y => !x.CourseOrder.Contains(y.Name)),
                (x, y) => new CandidateSolution(
                    x.CourseOrder.Append(y.Name).ToFrozenSet(),
                    x.UnvisitedControls.Except(y.Controls).ToFrozenSet()));

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
            logger.NoSolutionFound();
            return 3;
        }

        var required = beam[0].CourseOrder;
        string[] completeOrder = [
            .. required,
            .. availableCourses
                .Where(x => !required.Any(y => y == x.Name))
                .OrderByDescending(x => x.Controls.Count)
                .ThenBy(x => x.Name)
                .Select(x => x.Name),
            .. dominatedCourses.Order(),
        ];

        for (int i = 0; i < completeOrder.Length; i++)
        {
            if (i < required.Count)
            {
                Console.WriteLine($"{completeOrder[i]} (required)");
            }
            else
            {
                Console.WriteLine(completeOrder[i]);
            }
        }

        return 0;
    }
}
