// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using System.IO;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;

namespace Oblikovati.Exporter.NX.GoldenGen
{
    /// <summary>
    /// Writes golden documents to the directory given as the first argument. Each
    /// fixture exercises a slice of the translator so CI Job 2 can open them with the
    /// real oblikovati-cli.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("usage: GoldenGen <output-dir>");
                return 2;
            }

            string outDir = args[0];
            Directory.CreateDirectory(outDir);

            var translator = new DocumentTranslator();
            var writer = new RecipeYamlWriter();
            foreach (NxDocument fixture in Fixtures())
            {
                OblikovatiDocument doc = translator.Translate(fixture, new ExportReport());
                string path = Path.Combine(outDir, fixture.DisplayName + ".opd");
                File.WriteAllText(path, writer.Write(doc));
                Console.WriteLine("wrote " + path);
            }

            return 0;
        }

        private static IEnumerable<NxDocument> Fixtures()
        {
            yield return new NxDocument
            {
                DisplayName = "empty-part",
                Kind = NxDocumentKind.Part,
            };

            var parametric = new NxDocument
            {
                DisplayName = "parametric-part",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
            };
            parametric.Expressions.Add(new NxExpression { Name = "width", Formula = "40", Unit = "mm" });
            parametric.Expressions.Add(new NxExpression { Name = "twice", Formula = "width * 2", Unit = "mm" });
            yield return parametric;
        }
    }
}
