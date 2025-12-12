using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Web;

public class AppOptions {
    public string Name { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool DisplayBanner { get; set; } = true;
    public bool DisplayVersion { get; set; } = true;
}
