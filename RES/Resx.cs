using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Resources;

namespace RES
{
    internal static class Resx
    {
        public static IEnumerable<Resource> Read(string path)
        {
            Console.WriteLine($"Reading resx file '{path}'.");

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                yield break;

            var reader = new ResXResourceReader(path) { UseResXDataNodes = true };
            foreach (DictionaryEntry entry in reader)
            {
                var node = (ResXDataNode)entry.Value;
                if (node.FileRef != null || node.Name.StartsWith("$this.") || node.Name.StartsWith(">>"))
                    continue;

                var value = node.GetValue((ITypeResolutionService)null);
                if (!(value is string))
                    continue;

                yield return new()
                {
                    ResourceName = node.Name,
                    Value = (string)value,
                    Comment = node.Comment
                };
            }
        }

        public static void Write(string path, IEnumerable<Resource> resources)
        {
            Console.WriteLine($"Writing resx file '{path}'.");

            if (File.Exists(path))
                File.Delete(path);

            using (var writer = new ResXResourceWriter(path))
            {
                foreach (var resource in resources)
                {
                    var node = new ResXDataNode(resource.ResourceName, resource.Value) { Comment = resource.Comment };
                    writer.AddResource(node);
                }
            }
        }

    }
}
