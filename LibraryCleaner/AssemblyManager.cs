using System;
using System.Linq;
using System.Reflection;

namespace LibraryCleaner {

  internal static class AssemblyManager {

    public static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
    public static string[] EmbeddedLibraries = ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();

    public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
      // Get assembly name
      var assemblyName = new AssemblyName(args.Name).Name + ".dll";

      // Get resource name
      var resourceName = EmbeddedLibraries.FirstOrDefault(x => x.EndsWith(assemblyName));
      if (resourceName == null) {
        return null;
      }

      // Load assembly from resource
      using (var stream = ExecutingAssembly.GetManifestResourceStream(resourceName)) {
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        return Assembly.Load(bytes);
      }
    }
  }
}
