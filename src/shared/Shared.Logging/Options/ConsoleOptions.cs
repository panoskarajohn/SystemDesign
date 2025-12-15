using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Logging.Options;

public class ConsoleOptions {
    public bool Enabled { get; set; }
}

public class FileOptions {
    public bool Enabled { get; set; }
    public required string Path { get; set; }
    public required string Interval { get; set; }
}

public class LoggerOptions {
    public required string Level { get; set; }
    public required ConsoleOptions Console { get; set; }
    public required FileOptions File { get; set; }
    public required IDictionary<string, string> Overrides { get; set; }
    public required IEnumerable<string> ExcludePaths { get; set; }
    public required IEnumerable<string> ExcludeProperties { get; set; }
    public required IDictionary<string, object> Tags { get; set; }
}