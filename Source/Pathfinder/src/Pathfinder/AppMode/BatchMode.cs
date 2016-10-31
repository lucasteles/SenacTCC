﻿using Pathfinder.Abstraction;
using Pathfinder.Constants;
using Pathfinder.Factories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pathfinder.AppMode
{
    public class BatchMode : IAppMode
    {
        public void Run()
        {
            var setting = Program.Settings;
            var GASettings = Program.GASettings;

            var ft = new FileTool();
            setting.IDATrackRecursion = false;
            var qtdMaps = setting.Batch_map_qtd_to_generate;
            var now = DateTime.Now;
            var folder = Path.Combine(setting.Batch_folder, $"batch_{setting.Width}x{setting.Height}_{setting.RandomSeed * 100}_{now.Year}{now.Month}{now.Day}_{now.Hour}{now.Minute}");
            var dataFile = Path.Combine(folder, "_data.csv");
            var dataFileGA = Path.Combine(folder, "_dataGA.csv");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var diags = Enum.GetValues(typeof(DiagonalMovement));
            var diagonals = new DiagonalMovement[diags.Length];
            for (int i = 0; i < diags.Length; i++)
                diagonals[i] = (DiagonalMovement)diags.GetValue(i);


            var divblock = Math.Ceiling((double)qtdMaps / (double)diagonals.Count());
            var diagIndex = 0;

            if (setting.Batch_map_origin == 0)
            {
                //generate maps
                var generator = setting.Batch_generate_pattern == 0 ?
                                    MapGeneratorFactory.GetRandomMapGeneratorImplementation() :
                                    MapGeneratorFactory.GetStandardMapGeneratorImplementation();

                for (int i = 0; i < qtdMaps; i++)
                {
                    Console.Clear();
                    Console.WriteLine("Generating maps...");
                    drawTextProgressBar(i, qtdMaps);
                    var map = generator.DefineMap(diagonal: diagonals[diagIndex]);
                    map.AllowDiagonal = diagonals[diagIndex];
                    ft.SaveFileFromMap(map, Path.Combine(folder, i.ToString() + ".txt"));

                    if ((i + 1) % divblock == 0)
                    {
                        diagIndex++;
                    }
                }

            }
            else
            {
                //if will load the map, use the configured root path
                folder = setting.Batch_folder;
            }

            var files = Directory.GetFiles(folder);
            var fileCount = files.Count();

            var finders = new int[] { 0, 1, 2, 3, 4 };
            var heuristics = new int[] { 0, 1, 2, 3 };

            var Mutation = new int[] { 0, 1, 2, 3, 4, 5 };
            var Crossover = new int[] { 0, 1, 2 };
            var Fitness = new int[] { 0 };
            var Selection = new int[] { 0 };

            var csvFile = new StringBuilder();
            var csvGAFile = new StringBuilder();

            Console.Clear();
            csvFile.Append(new TextWrapper().GetHeader());
            csvGAFile.Append(new TextGAWrapper().GetHeader());

            for (int i = 0; i < fileCount; i++)
            {

                var map = ft.ReadMapFromFile(files[i]);


                foreach (var _finder in finders)
                {

                    foreach (var _h in heuristics)
                    {

                        var h = setting.GetHeuristic(_h);
                        var finder = setting.GetFinder(h, _finder);

                        if (finder is IGeneticAlgorithm)
                        {
                            var GAFinder = ((IGeneticAlgorithm)finder);
                            for (int j = 0; j < setting.Batch_GATimesToRunPerMap; j++)
                                for (int cross = 0; cross < Crossover.Count(); cross++)
                                    for (int mut = 0; mut < Mutation.Count(); mut++)
                                        for (int fit = 0; fit < Fitness.Count(); fit++)
                                            for (int sel = 0; sel < Selection.Count(); sel++)
                                            {
                                                GAFinder.Crossover = GASettings.GetCrossover(cross);
                                                GAFinder.Mutate    = GASettings.GetMutate(mut);
                                                GAFinder.Fitness   = GASettings.GetFitness(fit);
                                                GAFinder.Selection = GASettings.GetSelection(sel);

                                                var helper =$"n:{j},cx:{GAFinder.Crossover.GetType().Name},m:{GAFinder.Mutate.GetType().Name},f:{GAFinder.Fitness.GetType().Name},s:{GAFinder.Selection.GetType().Name}";

                                                var csv = new TextWrapper();
                                                csv = RunStep(csv, i, fileCount, map, h, finder, helper);

                                                var csvGA = new TextGAWrapper()
                                                {
                                                    alg = csv.alg,
                                                    map = csv.map,
                                                    heuristic = csv.heuristic,
                                                    diagonal = csv.diagonal,
                                                    solution = csv.solution,
                                                    time = csv.time,
                                                    maxNodes = csv.maxNodes,
                                                    pathLength = csv.pathLength,

                                                    Crossover = GAFinder.Crossover.GetType().Name,
                                                    Mutation = GAFinder.Mutate.GetType().Name,
                                                    fitness = GAFinder.Fitness.GetType().Name,
                                                    Selection = GAFinder.Selection.GetType().Name,
                                                    Generations = GAFinder.Generations.ToString(),
                                                };

                                                csvGAFile.Append(csvGA.ToString());


                                            }

                        }
                        else
                        {
                            var csv = new TextWrapper();
                            csv = RunStep(csv, i, fileCount, map, h, finder);
                            csvFile.Append(csv.ToString());
                        }
                    }
                }
            }



            drawTextProgressBar(fileCount, fileCount);

            File.WriteAllText(dataFile, csvFile.ToString());
            File.WriteAllText(dataFileGA, csvGAFile.ToString());
            Console.WriteLine("\n\nComplete...");
            Console.ReadKey();
        }


        private TextWrapper RunStep(TextWrapper baseScv, int i, int fileCount, IMap map, IHeuristic h, IFinder finder, string plus="")
        {
            var csv = baseScv;
            csv.map = i.ToString();
            csv.alg = finder.Name;
            csv.heuristic = h.GetType().Name;
            csv.diagonal = map.AllowDiagonal.HasValue ? map.AllowDiagonal.Value.ToString() : finder.DiagonalMovement.ToString();
            Console.CursorLeft = 0;
            if (Console.CursorTop > 0)
            {
                Console.Write(new string(' ', 80));
                Console.CursorLeft = 0;
            }


            Console.WriteLine($"            ({i}) {csv.alg} - { csv.heuristic } - {csv.diagonal} ({plus})");
            drawTextProgressBar(i, fileCount);


            if (finder.Find(map))
            {

                csv.pathLength = finder.GetPath().OrderBy(x => x.G).Last().G.ToString();
                Console.ForegroundColor = ConsoleColor.Green;
                csv.solution = "Yes (" + finder.GetProcessedTime().ToString() + "ms )";

            }
            else
            {
                csv.solution = "No";
                csv.pathLength = "-1";
                Console.ForegroundColor = ConsoleColor.Red;
            }
            csv.time = finder.GetProcessedTime().ToString();
            csv.maxNodes = finder.GetMaxExpandedNodes().ToString();

            Console.CursorTop -= 1;
            Console.CursorLeft = 0;
            Console.WriteLine(" " + csv.solution);
            Console.ForegroundColor = ConsoleColor.White;

            return csv;
        }

        class TextWrapper : BaseTextWrapper
        {
            public string alg        { get; set; }
            public string map        { get; set; }
            public string heuristic  { get; set; }
            public string diagonal   { get; set; }
            public string solution   { get; set; }
            public string time       { get; set; }
            public string maxNodes   { get; set; }
            public string pathLength { get; set; }
        }

        class TextGAWrapper : TextWrapper
        {

            public string fitness { get; set; }
            public string Mutation { get; set; }
            public string Crossover { get; set; }
            public string Selection { get; set; }
            public string Generations { get; set; }

        }

        abstract class BaseTextWrapper
        {

            public string GetHeader()
            {
                var ret = new StringBuilder();

                var props = typeof(TextWrapper).GetProperties();

                foreach (var item in props)
                {
                    ret.Append(item.Name);
                    ret.Append(";");
                }

                return ret.ToString() + "\n";
            }

            public override string ToString()
            {
                var ret = new StringBuilder();
                var type = GetType();
                var props = type.GetProperties();

                foreach (var item in props)
                {
                    var prop = type.GetProperty(item.Name);

                    ret.Append(prop.GetValue(this, null).ToString());
                    ret.Append(";");
                }

                return ret.ToString() + "\n";
            }
        }

        private static void drawTextProgressBar(int progress, int total, int barLength = 30, int left = 0, ConsoleColor color = ConsoleColor.Green)
        {
            char loadCchar = ' ';
            Console.CursorLeft = left;
            Console.Write("[");
            Console.CursorLeft = barLength + left + 1;
            Console.Write("]");
            Console.CursorLeft = 1 + left;
            var step = ((double)barLength / total);

            //draw filled part
            int position = 1;
            for (int i = 0; i < step * progress; i++)
            {
                Console.BackgroundColor = color;
                Console.CursorLeft = left + position++;
                Console.Write(loadCchar);
            }

            //draw unfilled part
            for (int i = position; i <= barLength; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = left + position++;
                Console.Write(loadCchar);
            }

            //draw totals
            Console.CursorLeft = left + barLength + 4;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress * 100 / total + "%    ");
        }

    }
}
