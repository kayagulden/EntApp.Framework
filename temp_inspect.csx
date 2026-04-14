using System;
using System.Reflection;
using System.Linq;

var asm = Assembly.LoadFile(@"C:\Users\kaya\.nuget\packages\elsa.workflows.runtime\3.6.0\lib\net8.0\Elsa.Workflows.Runtime.dll");
var types = asm.GetExportedTypes().Where(t => t.Name.Contains("Resume") || t.Name.Contains("runtime") || t.Name.Contains("Options")).ToList();
foreach (var t in types) Console.WriteLine($"{t.FullName}");

// IWorkflowRuntime aramasý
var rt = asm.GetExportedTypes().FirstOrDefault(t => t.Name == "IWorkflowRuntime");
if (rt != null) {
    Console.WriteLine($"\n=== IWorkflowRuntime methods ===");
    foreach (var m in rt.GetMethods()) {
        var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({parms})");
    }
}
