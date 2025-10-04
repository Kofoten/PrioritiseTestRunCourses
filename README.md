# Course Prioritizer

A sophisticated command-line tool that solves a complex routing problem by finding the smallest set of pre-defined routes (`courses`) required to visit all unique locations (`controls`). It uses an advanced beam search algorithm to find a high-quality, near-optimal solution efficiently.

This project was built to demonstrate advanced algorithmic problem-solving, performance optimization, and robust software design principles in .NET.

---
## Features

* **Advanced Search Algorithm**: Implements a **Beam Search** with a `PriorityQueue` to intelligently explore the most promising paths, balancing solution quality and performance.
* **Strategic Heuristics**: The search is guided by a **rarity score**, prioritizing routes that cover the most strategically important (i.e., rarest) controls first.
* **Efficient Pre-processing**:
    * An **inverted index** is used to efficiently calculate control rarity and to identify and remove **dominated courses** (routes that are objectively worse than others).
    * Useless courses (those with no controls) are filtered out at startup.
* **Robust Command-Line Interface (CLI)**:
    * A clean, hand-rolled argument parser handles user input.
    * Supports standard `--help` and `-h` flags.
    * Allows users to tune the `beam-width` and filter courses by name.
* **Safe and Modern Code**:
    * Written in modern C# with a focus on clarity and performance.
    * Uses a `TryParse` pattern for safe, exception-free argument parsing.
    * Implements structured logging with `Microsoft.Extensions.Logging`.
    * Correctly handles XML loading and schema validation for input files.

---
## Usage

To run the application, provide the path to an IOF XML 3.0 file containing the course data. You can also provide optional arguments to tune the search and filter the courses.

### Syntax
```shell
PrioritiseTestRunCourses.exe <IOFXmlFilePath> [options]
```

### Arguments
| Argument | Description |
| :--- | :--- |
| `<IOFXmlFilePath>` | **(Required)** The path to the IOF XML 3.0 file. |

### Options
| Option | Alias | Description | Default |
| :--- | :--- | :--- | :--- |
| `--beam-width <int>` | `-w` | Sets the width of the beam for the search. | `3` |
| `--filters <string>` | `-f` | One or more strings to filter course names by. | (none) |
| `--help` | `-h` | Show this help message and exit. | |

### Example
```shell
PrioritiseTestRunCourses.exe SampleData/courses.xml -w 5 -f "LongCourse" "NightCourse"
```

---
## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.