// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using System.IO;
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;

namespace Oblikovati.Exporter.NX.GoldenGen
{
    /// <summary>
    /// Writes golden documents to the directory given as the first argument. Fixtures are
    /// the shared <see cref="NxSampleParts"/> so CI Job 2 opens the exact inputs the unit
    /// tests assert, with the real oblikovati-cli.
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
            yield return NxSampleParts.EmptyPart();
            yield return NxSampleParts.ParametricPart();
            yield return NxSampleParts.RectanglePart();
            yield return NxSampleParts.CirclePart();
        }
    }
}
