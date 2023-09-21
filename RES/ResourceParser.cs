using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RES
{
    internal static class ResourceParser
    {
        public static IEnumerable<Resource> Parse(string directoryPath)
        {
            Console.WriteLine($"Parsing directory '{directoryPath}'.");

            var xamlFiles = Directory.GetFiles(directoryPath, "*.xaml", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\.") && !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));
            foreach (var file in xamlFiles)
            {
                Console.WriteLine($"Parsing file '{RemovePrefix(file, directoryPath)}'.");
                foreach (var resource in ParseXaml(file, directoryPath))
                    yield return resource;
            }


            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\.") && !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));
            foreach (var file in csFiles)
            {
                Console.WriteLine($"Parsing file '{RemovePrefix(file, directoryPath)}'.");
                foreach (var resource in ParseCS(file, directoryPath))
                    yield return resource;
            }
        }

        public static IEnumerable<Resource> ParseCS(string filePath, string directoryName)
        {
            var fileContents = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContents);
            var root = syntaxTree.GetRoot();

            var invocationExpressions = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocationExpressions)
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess != null && memberAccess.Name.ToString() == "Localize" && memberAccess.Expression is IdentifierNameSyntax identifierNameSyntax)
                {
                    var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (argument != null)
                    {
                        if (argument.Expression is LiteralExpressionSyntax literalExpression &&
                            literalExpression.Kind() == SyntaxKind.StringLiteralExpression)
                        {
                            var lineSpan = syntaxTree.GetLineSpan(identifierNameSyntax.Span);
                            var lineNumber = lineSpan.StartLinePosition.Line + 1;
                            var resourceName = literalExpression.Token.ValueText;

                            yield return new() { DirectoryPath = directoryName, FilePath = RemovePrefix(filePath, directoryName), LineNumber = lineNumber, ResourceName = resourceName };
                        }
                    }
                }
            }
        }

        public static IEnumerable<Resource> ParseXaml(string filePath, string directoryName)
        {
            var text = File.ReadAllText(filePath);

            // Define a regular expression pattern to match the resource names
            // This pattern matches "{namespace:Localize ResourceName}"
            var pattern = @"\{([^}]+?):Localize\s+([^}]+?)\}";

            // Use regex to find matches in the XAML content
            var matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    var namespaceName = match.Groups[1].Value;
                    var resourceName = match.Groups[2].Value;

                    // Print the namespace and resource name
                    yield return new() { DirectoryPath = directoryName, FilePath = RemovePrefix(filePath, directoryName), ResourceName = resourceName };
                }
            }
        }

        private static string RemovePrefix(string filePath, string basePath)
        {
            return filePath.StartsWith(basePath)
                ? filePath.Substring(basePath.Length)
                : filePath;
        }
    }
}
