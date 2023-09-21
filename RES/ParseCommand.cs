using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RES
{
    internal class ParseCommand : Command
    {
        private string _outputPath;
        private bool _removeUnused;
        private bool _showHelp;

        public ParseCommand()
            : base("parse", "")
        {
            Options = new OptionSet()
            {
                "usage: parse [OPTIONS] [DIRECTORIES]",
                "OPTIONS:",
                {  "o|output=", "The output resource file path.", v => _outputPath = v },
                {  "r|remove-unused", "Remove unused resources.", v => _removeUnused = v != null },
                {  "h|help", "Show this message and exit.", v => _showHelp = v != null },
            };
        }

        public override int Invoke(IEnumerable<string> args)
        {
            try
            {
                var directoryPaths = Options.Parse(args);
                if (_showHelp)
                {
                    Options.WriteOptionDescriptions(CommandSet.Out);
                    return 0;
                }

                var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var directoryPath in directoryPaths)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var resources = ResourceParser.Parse(directoryPath);
                    foreach (var resource in resources)
                        if (!resourceNames.Contains(resource.ResourceName))
                            resourceNames.Add(resource.ResourceName);
                }

                var resourceDictionary = Resx.Read(_outputPath).ToDictionary(r => r.ResourceName, r => r, StringComparer.OrdinalIgnoreCase);

                if (_removeUnused)
                {
                    foreach(var key in  resourceDictionary.Keys.ToList())
                    {
                        if (!resourceNames.Contains(key))
                        {
                            Console.WriteLine($"Removing unused resource '{key}'.");
                            resourceDictionary.Remove(key);
                        }
                    }
                }

                foreach (var resourceName in resourceNames)
                {
                    if (!resourceDictionary.ContainsKey(resourceName))
                    {
                        Console.WriteLine($"Adding new resource '{resourceName}'.");
                        resourceDictionary.Add(resourceName, new Resource { ResourceName = resourceName, Value = "", Comment = "TODO" });
                    }
                }

                Resx.Write(_outputPath, resourceDictionary.Values);

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        
    }
}
