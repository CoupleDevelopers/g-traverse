using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

//using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Microsoft.Build.Locator;

namespace GTraverse.ToolKit
{
    public class ReferenceFinder
    {
        [Theory]
        [InlineData("Volkan")]
        public void Find(string methodName)
        {
            MSBuildLocator.RegisterDefaults();

            string solutionPath = @"C:\Users\t-mac\Desktop\g-traverse\GTraverse.sln";
            var msWorkspace = MSBuildWorkspace.Create();
            msWorkspace.WorkspaceFailed += MsWorkspace_WorkspaceFailed;

            List<ReferencedSymbol> referencesToMethod = new List<ReferencedSymbol>();
            Console.WriteLine("Searching for method \"{0}\" reference in solution {1} ", methodName, Path.GetFileName(solutionPath));
            ISymbol methodSymbol = null;
            bool found = false;

            //You must install the MSBuild Tools or this line will throw an exception.

            var solution = msWorkspace.OpenSolutionAsync(solutionPath).Result;
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var model = document.GetSemanticModelAsync().Result;

                    var methodInvocation = document.GetSyntaxRootAsync().Result;
                    MethodDeclarationSyntax node = null;
                    try
                    {
                        var descendants = methodInvocation.DescendantNodes();
                        descendants = descendants.Where(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration));
                        node = descendants.Select(x => (MethodDeclarationSyntax)x).FirstOrDefault(x => x.Identifier.Text == methodName);
                        var a = node.Body.DescendantNodes();
                        if (node == null)
                            continue;
                    }
                    catch (Exception exception)
                    {
                        // Swallow the exception of type cast.
                        // Could be avoided by a better filtering on above linq.
                        continue;
                    }

                    methodSymbol = model.GetSymbolInfo(node).Symbol;

                    found = true;
                    break;
                }

                if (found) break;
            }

            foreach (var item in SymbolFinder.FindReferencesAsync(methodSymbol, solution).Result)
            {
                foreach (var location in item.Locations)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Project Assembly -> {0}", location.Document.Project.AssemblyName);
                    Console.ResetColor();
                }
            }

            Console.WriteLine("Finished searching. Press any key to continue....");
        }

        private void MsWorkspace_WorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}