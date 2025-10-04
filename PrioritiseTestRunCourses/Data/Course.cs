using System.Collections.Frozen;

namespace PrioritiseTestRunCourses.Data;

internal record Course(string Name, FrozenSet<string> Controls)
{
    public bool IsDominatedByAnyIn(IEnumerable<Course> courses) => courses
        .Any(y => y.Name != Name && Controls.All(z => y.Controls.Contains(z)));

    public static Course FromIOF(IOF.Xml.Course iofCourse)
    {
        var controls = iofCourse.CourseControl
            .Where(x => x.type == IOF.Xml.ControlType.Control)
            .SelectMany(x => x.Control)
            .ToFrozenSet();

        return new Course(iofCourse.Name, controls);
    }
}
